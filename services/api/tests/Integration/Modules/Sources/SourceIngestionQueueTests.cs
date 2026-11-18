using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Domain;
using Api.Modules.Sources.Infrastructure.Persistence;
using Api.Modules.Sources.Infrastructure.Ingestion;
using Api.SharedKernel;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Integration.Modules.Sources;

public sealed class SourceIngestionQueueTests : IAsyncLifetime
{
    private readonly ScriptedProcessor _processor = new();
    private SourcesApi _api = null!;

    public async Task InitializeAsync() =>
        _api = await SourcesApi.StartAsync(runWorker: false, services =>
        {
            services.AddScoped<IRequestHandler<ProcessSourceVersionCommand, Result<ProcessSourceVersionOutcome>>>(provider =>
            {
                _processor.Real = new ProcessSourceVersionHandler(
                    provider.GetRequiredService<ISourceRepository>(),
                    provider.GetRequiredService<TimeProvider>());
                return _processor;
            });
        });

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task APasteCommitsItsVersionAndQueuedJobTogether()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var accepted = await _api.PasteAsync(campaignId, "Ideas", "Texto");

        accepted["availability"]!.GetValue<string>().Should().Be("unavailable");
        accepted["lastPublishedAt"].Should().BeNull();
        accepted["processing"]!["state"]!.GetValue<string>().Should().Be("queued");
        accepted["processing"]!["attempts"]!.GetValue<int>().Should().Be(0);
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var version = await db.Versions.SingleAsync();
        var job = await db.Jobs.SingleAsync();
        job.SourceVersionId.Should().Be(version.Id);
        job.CampaignId.Should().Be(version.CampaignId);
        job.State.Should().Be(IngestionJobState.Queued);
    }

    [Fact]
    public async Task AFailedSaveLeavesNeitherSourceVersionNorJob()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var source = Source.Create(Guid.NewGuid(), SourceMetadata.Parse("Huérfana", null, null).Value, SourceOrigin.Paste, DateTimeOffset.UtcNow);
        var version = SourceVersion.Create(source, SourceText.Create("texto").Value, null, DateTimeOffset.UtcNow);
        db.AddRange(source, version, SourceIngestionJob.Create(version, DateTimeOffset.UtcNow));

        var act = () => db.SaveChangesAsync();

        await act.Should().ThrowAsync<DbUpdateException>("the campaign does not exist");
        await using var verify = _api.Services.CreateAsyncScope();
        var fresh = verify.ServiceProvider.GetRequiredService<SourcesDbContext>();
        (await fresh.Sources.CountAsync()).Should().Be(0);
        (await fresh.Versions.CountAsync()).Should().Be(0);
        (await fresh.Jobs.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AVersionCannotBelongToASourceOfAnotherCampaign()
    {
        var ashCrown = Guid.Parse(await _api.CreateCampaignAsync("Ash Crown"));
        var greyMarch = Guid.Parse(await _api.CreateCampaignAsync("Grey March"));
        var pasted = await _api.PasteAsync(ashCrown.ToString("D"), "Mirela", "texto");
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();

        var act = () => db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO source_versions (id, campaign_id, source_id, original_text, content_hash, created_at)
            VALUES ({Guid.NewGuid()}, {greyMarch}, {Guid.Parse(pasted["id"]!.GetValue<string>())}, 'x', {new string('a', 64)}, now())
            """);

        (await act.Should().ThrowAsync<PostgresException>()).Which.SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
    }

    [Fact]
    public async Task ClaimingSkipsJobsLockedByAnotherWorker()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        await _api.PasteAsync(campaignId, "Uno", "uno");
        await _api.PasteAsync(campaignId, "Dos", "dos");
        await using var first = _api.Services.CreateAsyncScope();
        await using var second = _api.Services.CreateAsyncScope();
        var firstDb = first.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var secondDb = second.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var now = DateTimeOffset.UtcNow;

        await using var transaction = await firstDb.Database.BeginTransactionAsync();
        var claimedFirst = await new IngestionJobQueue(firstDb).ClaimNextAsync(now, TimeSpan.FromMinutes(5), maxAttempts: 3, CancellationToken.None);
        var claimedSecond = await new IngestionJobQueue(secondDb).ClaimNextAsync(now, TimeSpan.FromMinutes(5), maxAttempts: 3, CancellationToken.None);
        var claimedThird = await new IngestionJobQueue(secondDb).ClaimNextAsync(now, TimeSpan.FromMinutes(5), maxAttempts: 3, CancellationToken.None);

        claimedFirst.Should().NotBeNull();
        claimedSecond.Should().NotBeNull();
        claimedSecond!.JobId.Should().NotBe(claimedFirst!.JobId);
        claimedThird.Should().BeNull();
    }

    [Fact]
    public async Task AnExpiredLeaseIsReclaimedAndProcessed()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var sourceId = (await _api.PasteAsync(campaignId, "Ideas", "Texto"))["id"]!.GetValue<string>();
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
            var claimed = await new IngestionJobQueue(db).ClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), maxAttempts: 3, CancellationToken.None);
            claimed!.Attempts.Should().Be(1);
            (await new IngestionJobQueue(db).ClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), maxAttempts: 3, CancellationToken.None))
                .Should().BeNull("the lease is still valid");
            await db.Database.ExecuteSqlRawAsync("UPDATE source_ingestion_jobs SET lease_expires_at = now() - interval '1 second'");
        }

        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeTrue();

        var source = await _api.WaitForSourceAsync(campaignId, sourceId, _ => true);
        source["availability"]!.GetValue<string>().Should().Be("available");
        (await JobAsync()).Attempts.Should().Be(2);
    }

    [Fact]
    public async Task TransientFailuresRetryUpToTheLimitThenFailAsUnavailable()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var sourceId = (await _api.PasteAsync(campaignId, "Ideas", "Texto"))["id"]!.GetValue<string>();
        _processor.Failure = () => new TimeoutException("provider timed out");

        for (var attempt = 1; attempt <= 3; attempt++)
        {
            (await Runner().RunNextAsync(CancellationToken.None)).Should().BeTrue();
            var job = await JobAsync();
            job.Attempts.Should().Be(attempt);
            job.State.Should().Be(attempt < 3 ? IngestionJobState.Queued : IngestionJobState.Failed);
        }

        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeFalse("a failed job is never claimed again");
        var source = await _api.WaitForSourceAsync(campaignId, sourceId, _ => true);
        source["availability"]!.GetValue<string>().Should().Be("unavailable");
        source["processing"]!["state"]!.GetValue<string>().Should().Be("failed");
        source["processing"]!["errorCode"]!.GetValue<string>().Should().Be("source_processing_unavailable");
        source["processing"]!["attempts"]!.GetValue<int>().Should().Be(3);
    }

    [Fact]
    public async Task AnUnexpectedFailureIsNotRetried()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var sourceId = (await _api.PasteAsync(campaignId, "Ideas", "Texto"))["id"]!.GetValue<string>();
        _processor.Failure = () => new InvalidOperationException("bug");

        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeTrue();
        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeFalse();

        var job = await JobAsync();
        job.Should().BeEquivalentTo(new { State = IngestionJobState.Failed, Attempts = 1, ErrorCode = "source_processing_failed" });
        var source = await _api.WaitForSourceAsync(campaignId, sourceId, _ => true);
        source["processing"]!["errorCode"]!.GetValue<string>().Should().Be("source_processing_failed");
    }

    [Fact]
    public async Task ReprocessingAVersionReplacesItsChunksInsteadOfDuplicatingThem()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        await _api.PasteAsync(campaignId, "Ideas", "Uno\n\nDos");
        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeTrue();
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
            await db.Database.ExecuteSqlRawAsync("UPDATE source_ingestion_jobs SET state = 'queued', next_attempt_at = now() - interval '1 second'");
        }

        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeTrue();

        await using var verify = _api.Services.CreateAsyncScope();
        var chunks = await verify.ServiceProvider.GetRequiredService<SourcesDbContext>().Chunks.ToListAsync();
        chunks.Should().ContainSingle().Which.Text.Should().Be("Uno\n\nDos");
    }

    [Fact]
    public async Task AnExpiredLeaseOnTheLastAttemptFailsInsteadOfBeingClaimedAgain()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var sourceId = (await _api.PasteAsync(campaignId, "Ideas", "Texto"))["id"]!.GetValue<string>();
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Database.ExecuteSqlRawAsync(
                "UPDATE source_ingestion_jobs SET state = 'processing', attempts = 3, lease_expires_at = now() - interval '1 second'");
        }

        (await Runner().RunNextAsync(CancellationToken.None)).Should().BeFalse("an exhausted job is never claimed again");

        (await JobAsync()).Should().BeEquivalentTo(new { State = IngestionJobState.Failed, Attempts = 3, ErrorCode = "source_processing_unavailable" });
        var source = await _api.WaitForSourceAsync(campaignId, sourceId, _ => true);
        source["processing"]!["state"]!.GetValue<string>().Should().Be("failed");
    }

    private SourceIngestionRunner Runner() => _api.Services.GetRequiredService<SourceIngestionRunner>();

    private async Task<SourceIngestionJob> JobAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Jobs.AsNoTracking().SingleAsync();
    }

    private sealed class ScriptedProcessor : IRequestHandler<ProcessSourceVersionCommand, Result<ProcessSourceVersionOutcome>>
    {
        public ProcessSourceVersionHandler? Real { get; set; }

        public Func<Exception>? Failure { get; set; }

        public Task<Result<ProcessSourceVersionOutcome>> Handle(ProcessSourceVersionCommand command, CancellationToken cancellationToken) =>
            Failure is { } failure ? throw failure() : Real!.Handle(command, cancellationToken);
    }
}
