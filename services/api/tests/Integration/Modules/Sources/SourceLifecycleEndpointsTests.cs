using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Domain;
using Api.Modules.Sources.Infrastructure.Ingestion;
using Api.Modules.Sources.Infrastructure.Persistence;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Modules.Sources;

/// <summary>Update, edit, retry and removal (`06`), with the worker driven by hand so every intermediate state can be observed.</summary>
public sealed class SourceLifecycleEndpointsTests : IAsyncLifetime
{
    private SourcesApi _api = null!;
    private string _campaignId = null!;

    public async Task InitializeAsync()
    {
        _api = await SourcesApi.StartAsync(runWorker: false);
        _campaignId = await _api.CreateCampaignAsync("Ash Crown");
    }

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task AnUpdateKeepsThePublishedVersionUntilTheNewOneIsPublishedThenDeletesIt()
    {
        var bystander = await PublishedBystanderAsync();
        var sourceId = await PublishedPasteAsync("Mirela guarda el puente.");
        var firstVersion = (await SourceRowAsync(sourceId)).PublishedVersionId;

        using var response = await PutTextAsync(sourceId, "Mirela abandona el puente.");

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var pending = await ReadAsync(response);
        pending["availability"]!.GetValue<string>().Should().Be("available");
        pending["processing"]!["state"]!.GetValue<string>().Should().Be("queued");
        (await SourceRowAsync(sourceId)).PublishedVersionId.Should().Be(firstVersion, "the old version stays searchable meanwhile");

        await RunWorkerAsync();

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var version = await db.Versions.SingleAsync(version => version.SourceId == Guid.Parse(sourceId));
        version.OriginalText.Should().Be("Mirela abandona el puente.");
        (await SourceRowAsync(sourceId)).PublishedVersionId.Should().Be(version.Id);
        version.Id.Should().NotBe(firstVersion!.Value);
        (await db.Chunks.Where(chunk => chunk.SourceVersionId != bystander.VersionId).Select(chunk => chunk.SourceVersionId).Distinct().ToListAsync())
            .Should().Equal(version.Id);
        (await db.Jobs.CountAsync(job => job.SourceVersionId == version.Id)).Should().Be(1);
        await ShouldBeIntactAsync(bystander);
    }

    [Fact]
    public async Task AFailedUpdateKeepsThePublishedVersionAndARetryPublishesItWithoutDuplicates()
    {
        var sourceId = await PublishedPasteAsync("Primera versión.");
        (await PutTextAsync(sourceId, "Segunda versión.")).StatusCode.Should().Be(HttpStatusCode.Accepted);
        await FailQueuedJobsAsync();

        var failed = await GetAsync(sourceId);
        failed["availability"]!.GetValue<string>().Should().Be("available");
        failed["processing"]!["state"]!.GetValue<string>().Should().Be("failed");
        failed["processing"]!["errorCode"]!.GetValue<string>().Should().Be("source_processing_failed");

        using var retry = await _api.Client.PostAsync(SourceUrl(sourceId) + "/retry", null);

        retry.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var queued = await ReadAsync(retry);
        queued["processing"]!["state"]!.GetValue<string>().Should().Be("queued");
        queued["processing"]!["attempts"]!.GetValue<int>().Should().Be(0);

        await RunWorkerAsync();

        (await GetAsync(sourceId))["processing"].Should().BeNull();
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        (await db.Sources.CountAsync()).Should().Be(1);
        (await db.Versions.SingleAsync()).OriginalText.Should().Be("Segunda versión.");
    }

    [Fact]
    public async Task ARetryNeedsAFailedVersion()
    {
        var sourceId = await PublishedPasteAsync("Texto.");

        using var response = await _api.Client.PostAsync(SourceUrl(sourceId) + "/retry", null);

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "source_retry_unavailable");
    }

    [Fact]
    public async Task ContentEqualToThePublishedVersionChangesNothing()
    {
        var sourceId = await PublishedPasteAsync("Texto.");
        var before = await GetAsync(sourceId);

        using var response = await PutTextAsync(sourceId, "Texto.");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response))["updatedAt"]!.GetValue<string>().Should().Be(before["updatedAt"]!.GetValue<string>());
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        (await db.Versions.CountAsync()).Should().Be(1);
        (await db.Jobs.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AnUpdateIsRefusedWhileAVersionIsQueued()
    {
        var sourceId = (await _api.PasteAsync(_campaignId, "Notas", "Uno."))["id"]!.GetValue<string>();

        using var response = await PutTextAsync(sourceId, "Dos.");

        await ShouldBeProblemAsync(response, HttpStatusCode.Conflict, "source_processing");
    }

    [Fact]
    public async Task ConcurrentUpdatesQueueExactlyOneVersion()
    {
        var sourceId = await PublishedPasteAsync("Base.");

        var responses = await Task.WhenAll(Enumerable.Range(0, 6).Select(index => PutTextAsync(sourceId, $"Cambio {index}.")));

        responses.Select(response => response.StatusCode).Should()
            .ContainSingle(status => status == HttpStatusCode.Accepted)
            .And.HaveCount(6)
            .And.OnlyContain(status => status == HttpStatusCode.Accepted || status == HttpStatusCode.Conflict);
        await using var scope = _api.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Versions.CountAsync()).Should().Be(2);
        foreach (var response in responses)
        {
            response.Dispose();
        }
    }

    [Fact]
    public async Task AFileUpdatesAPastedSourceWhichKeepsItsOrigin()
    {
        var sourceId = await PublishedPasteAsync("Pegado.");

        using var response = await PutAsync(sourceId, Multipart(file: ("notas.md", "# Notas\n\nDesde fichero."u8.ToArray())));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
        (await ReadAsync(response))["origin"]!.GetValue<string>().Should().Be("paste");
        await RunWorkerAsync();
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        (await db.Versions.SingleAsync()).FileName.Should().Be("notas.md");
        (await db.Chunks.SingleAsync()).HeadingPath.Should().Equal("Notas");
    }

    [Fact]
    public async Task AnUpdateNeedsExactlyOneValidContentPart()
    {
        var sourceId = await PublishedPasteAsync("Texto.");

        using var both = await PutAsync(sourceId, Multipart(file: ("a.md", "# A"u8.ToArray()), text: "B"));
        var twoTexts = Multipart(text: "A");
        twoTexts.Add(new StringContent("B"), "text");
        using var repeatedText = await PutAsync(sourceId, twoTexts);
        var twoFiles = Multipart(file: ("a.md", "# A"u8.ToArray()));
        twoFiles.Add(new ByteArrayContent("# B"u8.ToArray()), "file", "b.md");
        using var repeatedFile = await PutAsync(sourceId, twoFiles);
        // A multipart body with no section at all is unreadable and gets the framework's 400 without `code`.
        var unrelatedField = Multipart();
        unrelatedField.Add(new StringContent("x"), "nota");
        using var neither = await PutAsync(sourceId, unrelatedField);
        using var emptyText = await PutAsync(sourceId, Multipart(text: ""));
        using var pdf = await PutAsync(sourceId, Multipart(file: ("mapa.pdf", "x"u8.ToArray())));

        await ShouldBeProblemAsync(both, HttpStatusCode.BadRequest, "source_content_part_invalid");
        await ShouldBeProblemAsync(repeatedText, HttpStatusCode.BadRequest, "source_content_part_invalid");
        await ShouldBeProblemAsync(repeatedFile, HttpStatusCode.BadRequest, "source_content_part_invalid");
        await ShouldBeProblemAsync(neither, HttpStatusCode.BadRequest, "source_content_part_invalid");
        await ShouldBeProblemAsync(emptyText, HttpStatusCode.BadRequest, "source_empty");
        await ShouldBeProblemAsync(pdf, HttpStatusCode.BadRequest, "source_file_type_unsupported");
    }

    [Fact]
    public async Task ABodyAboveTheUpdateLimitIsRejectedWith413()
    {
        // The in-memory TestServer ignores request body size limits, so this runs against Kestrel.
        await using var kestrel = await SourcesApi.StartAsync(runWorker: false, useKestrel: true);
        var campaignId = await kestrel.CreateCampaignAsync("Ash Crown");
        var sourceId = (await kestrel.PasteAsync(campaignId, "Notas", "Texto."))["id"]!.GetValue<string>();

        // Kestrel answers before reading a body whose length is over the limit; waiting for 100-continue keeps the
        // client from writing into a connection the server has already closed.
        using var request = new HttpRequestMessage(HttpMethod.Put, $"/api/campaigns/{campaignId}/sources/{sourceId}/content")
        {
            Content = Multipart(file: ("grande.txt", new byte[2 * 1024 * 1024]))
        };
        request.Headers.ExpectContinue = true;

        using var response = await kestrel.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task AnEditChangesOnlyTheFieldsItContains()
    {
        var pasted = await _api.Client.PostAsJsonAsync(
            $"/api/campaigns/{_campaignId}/sources/pastes",
            new { title = "Sesión 3", text = "Texto.", category = "session_notes", knowledgeStatus = "occurred" });
        var sourceId = (await ReadAsync(pasted))["id"]!.GetValue<string>();

        using var response = await PatchAsync(sourceId, """{ "title": " Sesión 4 ", "category": null }""");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var edited = await ReadAsync(response);
        edited["title"]!.GetValue<string>().Should().Be("Sesión 4");
        edited["category"].Should().BeNull();
        edited["knowledgeStatus"]!.GetValue<string>().Should().Be("occurred");
        edited["processing"]!["state"]!.GetValue<string>().Should().Be("queued", "an edit creates no version");
        await using var scope = _api.Services.CreateAsyncScope();
        (await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Versions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task AnEmptyEditChangesNothing()
    {
        var sourceId = await PublishedPasteAsync("Texto.");
        var before = await GetAsync(sourceId);

        using var response = await PatchAsync(sourceId, "{}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadAsync(response))["updatedAt"]!.GetValue<string>().Should().Be(before["updatedAt"]!.GetValue<string>());
    }

    [Fact]
    public async Task AnEditRejectsNullForFieldsThatCannotBeCleared()
    {
        var sourceId = await PublishedPasteAsync("Texto.");

        using var response = await PatchAsync(sourceId, """{ "title": null, "knowledgeStatus": null }""");

        var problem = await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "source_title_invalid");
        problem["errors"]!["knowledgeStatus"]![0]!.GetValue<string>().Should().Be("source_knowledge_status_invalid");
    }

    [Fact]
    public async Task RemovalWinsOverProcessingAndLeavesOnlyATombstone()
    {
        var bystander = await PublishedBystanderAsync();
        var sourceId = (await _api.PasteAsync(_campaignId, "Mirela", "Mirela guarda el puente."))["id"]!.GetValue<string>();
        ClaimedIngestionJob claimed;
        await using (var claimScope = _api.Services.CreateAsyncScope())
        {
            var db = claimScope.ServiceProvider.GetRequiredService<SourcesDbContext>();
            claimed = (await new IngestionJobQueue(db).ClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 3, CancellationToken.None))!;
        }

        using var removal = await _api.Client.DeleteAsync(SourceUrl(sourceId));

        removal.StatusCode.Should().Be(HttpStatusCode.NoContent);
        await using (var processScope = _api.Services.CreateAsyncScope())
        {
            var outcome = await processScope.ServiceProvider.GetRequiredService<ISender>().Send(new ProcessSourceVersionCommand(claimed));
            outcome.Value.Should().Be(ProcessSourceVersionOutcome.Discarded);
        }

        (await _api.ListAsync(_campaignId)).Should().BeEmpty();
        await using var scope = _api.Services.CreateAsyncScope();
        var verify = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var tombstone = await SourceRowAsync(sourceId);
        tombstone.Should().BeEquivalentTo(new { PublishedVersionId = (Guid?)null, IsRemoved = true });
        tombstone.Title.Value.Should().Be("Mirela");
        await ShouldOnlyHoldTheBystanderAsync(verify, bystander);
    }

    [Fact]
    public async Task ARemovalWaitsForAPublicationHoldingTheLockThenRemovesWhatItPublished()
    {
        var bystander = await PublishedBystanderAsync();
        var sourceId = (await _api.PasteAsync(_campaignId, "Mirela", "Mirela guarda el puente."))["id"]!.GetValue<string>();
        await using var workerScope = _api.Services.CreateAsyncScope();
        var claimed = (await new IngestionJobQueue(workerScope.ServiceProvider.GetRequiredService<SourcesDbContext>())
            .ClaimNextAsync(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(5), 3, CancellationToken.None))!;
        var work = await workerScope.ServiceProvider.GetRequiredService<Api.Modules.Sources.Application.ISourceRepository>()
            .GetProcessingWorkAsync(claimed.CampaignId, claimed.JobId, CancellationToken.None);
        work.Should().NotBeNull("the worker now holds the source row lock");

        var removal = _api.Client.DeleteAsync(SourceUrl(sourceId));
        await WaitUntilALockIsAwaitedAsync();

        removal.IsCompleted.Should().BeFalse("the removal is blocked on the source row lock");
        var outcome = await workerScope.ServiceProvider.GetRequiredService<ISender>().Send(new ProcessSourceVersionCommand(claimed));
        outcome.Value.Should().Be(ProcessSourceVersionOutcome.Published);
        using var removed = await removal;
        removed.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _api.Services.CreateAsyncScope();
        var verify = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        (await SourceRowAsync(sourceId)).IsRemoved.Should().BeTrue();
        await ShouldOnlyHoldTheBystanderAsync(verify, bystander);
    }

    [Fact]
    public async Task ARemovedSourceIsNotFoundForEveryLifecycleRoute()
    {
        var sourceId = await PublishedPasteAsync("Texto.");
        (await _api.Client.DeleteAsync(SourceUrl(sourceId))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        await ShouldBeNotFoundEverywhereAsync(_campaignId, sourceId);
    }

    [Fact]
    public async Task ASourceOfAnotherCampaignIsNotFound()
    {
        var sourceId = await PublishedPasteAsync("Texto.");
        var greyMarch = await _api.CreateCampaignAsync("Grey March");

        await ShouldBeNotFoundEverywhereAsync(greyMarch, sourceId);
        (await GetAsync(sourceId))["title"]!.GetValue<string>().Should().Be("Notas");
    }

    [Fact]
    public async Task AMalformedSourceIdIsRejected()
    {
        using var response = await _api.Client.DeleteAsync(SourceUrl("not-a-uuid"));

        await ShouldBeProblemAsync(response, HttpStatusCode.BadRequest, "source_id_invalid");
    }

    private async Task ShouldBeNotFoundEverywhereAsync(string campaignId, string sourceId)
    {
        var url = $"/api/campaigns/{campaignId}/sources/{sourceId}";
        using var put = await _api.Client.PutAsync(url + "/content", Multipart(text: "Otro."));
        using var patch = await _api.Client.PatchAsync(url, JsonBody("""{ "title": "Otro" }"""));
        using var retry = await _api.Client.PostAsync(url + "/retry", null);
        using var delete = await _api.Client.DeleteAsync(url);

        foreach (var response in new[] { put, patch, retry, delete })
        {
            await ShouldBeProblemAsync(response, HttpStatusCode.NotFound, "source_not_found");
        }
    }

    private sealed record Bystander(Guid SourceId, Guid VersionId, int Chunks);

    /// <summary>A published source in another campaign that no operation of these tests may touch.</summary>
    private async Task<Bystander> PublishedBystanderAsync()
    {
        var greyMarch = await _api.CreateCampaignAsync("Grey March");
        var sourceId = Guid.Parse((await _api.PasteAsync(greyMarch, "Reina gris", "Uno.\n\nDos."))["id"]!.GetValue<string>());
        await RunWorkerAsync();
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var versionId = (await db.Sources.SingleAsync(source => source.Id == sourceId)).PublishedVersionId!.Value;
        return new Bystander(sourceId, versionId, await db.Chunks.CountAsync(chunk => chunk.SourceVersionId == versionId));
    }

    private async Task ShouldBeIntactAsync(Bystander bystander)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var source = await db.Sources.SingleAsync(source => source.Id == bystander.SourceId);
        source.Should().BeEquivalentTo(new { PublishedVersionId = (Guid?)bystander.VersionId, IsRemoved = false });
        (await db.Chunks.CountAsync(chunk => chunk.SourceVersionId == bystander.VersionId)).Should().Be(bystander.Chunks).And.BePositive();
        (await db.Jobs.CountAsync(job => job.SourceVersionId == bystander.VersionId)).Should().Be(1);
    }

    private async Task ShouldOnlyHoldTheBystanderAsync(SourcesDbContext db, Bystander bystander)
    {
        (await db.Versions.Select(version => version.Id).ToListAsync()).Should().Equal(bystander.VersionId);
        (await db.Chunks.AllAsync(chunk => chunk.SourceVersionId == bystander.VersionId)).Should().BeTrue();
        (await db.Jobs.Select(job => job.SourceVersionId).ToListAsync()).Should().Equal(bystander.VersionId);
        await ShouldBeIntactAsync(bystander);
    }

    /// <summary>The checkpoint for a race test: PostgreSQL reports a session waiting for a row lock.</summary>
    private async Task WaitUntilALockIsAwaitedAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (await db.Database.SqlQueryRaw<int>("SELECT count(*)::int AS \"Value\" FROM pg_locks WHERE NOT granted").SingleAsync() == 0)
        {
            DateTime.UtcNow.Should().BeBefore(deadline, "the removal never reached the source lock");
            await Task.Delay(20);
        }
    }

    private async Task<string> PublishedPasteAsync(string text)
    {
        var sourceId = (await _api.PasteAsync(_campaignId, "Notas", text))["id"]!.GetValue<string>();
        await RunWorkerAsync();
        (await GetAsync(sourceId))["availability"]!.GetValue<string>().Should().Be("available");
        return sourceId;
    }

    private async Task RunWorkerAsync()
    {
        var runner = _api.Services.GetRequiredService<SourceIngestionRunner>();
        while (await runner.RunNextAsync(CancellationToken.None))
        {
        }
    }

    private async Task FailQueuedJobsAsync()
    {
        await using var scope = _api.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Database.ExecuteSqlRawAsync(
            "UPDATE source_ingestion_jobs SET state = 'failed', attempts = 1, error_code = 'source_processing_failed' WHERE state = 'queued'");
    }

    private async Task<Source> SourceRowAsync(string sourceId)
    {
        await using var scope = _api.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Sources.AsNoTracking()
            .SingleAsync(source => source.Id == Guid.Parse(sourceId));
    }

    private async Task<JsonObject> GetAsync(string sourceId) =>
        (await _api.ListAsync(_campaignId)).OfType<JsonObject>().Single(source => source["id"]!.GetValue<string>() == sourceId);

    private string SourceUrl(string sourceId) => $"/api/campaigns/{_campaignId}/sources/{sourceId}";

    private Task<HttpResponseMessage> PutTextAsync(string sourceId, string text) => PutAsync(sourceId, Multipart(text: text));

    private Task<HttpResponseMessage> PutAsync(string sourceId, HttpContent content) =>
        _api.Client.PutAsync(SourceUrl(sourceId) + "/content", content);

    private Task<HttpResponseMessage> PatchAsync(string sourceId, string json) => _api.Client.PatchAsync(SourceUrl(sourceId), JsonBody(json));

    private static StringContent JsonBody(string json) => new(json, Encoding.UTF8, "application/json");

    private static MultipartFormDataContent Multipart((string Name, byte[] Bytes)? file = null, string? text = null)
    {
        var content = new MultipartFormDataContent();
        if (file is { } part)
        {
            var bytes = new ByteArrayContent(part.Bytes);
            bytes.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(bytes, "file", part.Name);
        }

        if (text is not null)
        {
            content.Add(new StringContent(text, Encoding.UTF8), "text");
        }

        return content;
    }

    private static async Task<JsonObject> ReadAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())!;

    private static async Task<JsonObject> ShouldBeProblemAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(status, body);
        var problem = JsonNode.Parse(body)!.AsObject();
        ((string?)problem["code"]).Should().Be(code, body);
        return problem;
    }
}
