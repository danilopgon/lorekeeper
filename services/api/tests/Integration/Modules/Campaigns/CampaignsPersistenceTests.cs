using Api.Modules.Campaigns.Domain;
using Api.Modules.Campaigns.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Integration.Modules.Campaigns;

public sealed class CampaignsPersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("lorekeeper_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    [Fact]
    public async Task CampaignsStartEmpty()
    {
        await using var context = CreateContext();

        var campaigns = await context.Campaigns.ToListAsync();

        campaigns.Should().BeEmpty();
    }

    [Fact]
    public async Task CampaignInsertPersistsTrimmedNameAndUtcTimestamps()
    {
        await using var context = CreateContext();
        var name = CampaignName.Create("  Ash Crown  ").Value!;
        var now = new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.FromHours(2));

        context.Campaigns.Add(Campaign.Create(name, now));
        await context.SaveChangesAsync();

        var stored = await context.Campaigns.SingleAsync();
        stored.Name.Value.Should().Be("Ash Crown");
        stored.CreatedAt.Should().Be(now.ToUniversalTime());
        stored.UpdatedAt.Should().Be(stored.CreatedAt);
    }

    [Fact]
    public async Task DatabaseCheckConstraintsRejectUntrimmedOrInvalidNames()
    {
        await using var context = CreateContext();

        var act = () => context.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO campaigns (id, name, created_at, updated_at)
            VALUES ({0}, {1}, now(), now())
            """,
            Guid.NewGuid(),
            " Ash Crown ");

        var exception = await act.Should().ThrowAsync<DbUpdateException>();
        CampaignPersistenceErrors.From(exception.Which).Should().Be(CampaignPersistenceError.InvalidName);
    }

    [Fact]
    public async Task DuplicateCampaignNamesAreRejectedCaseInsensitively()
    {
        await using var context = CreateContext();
        context.Campaigns.Add(Campaign.Create(CampaignName.Create("Ash Crown").Value!, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();

        context.Campaigns.Add(Campaign.Create(CampaignName.Create("ash crown").Value!, DateTimeOffset.UtcNow));
        var exception = await context.Invoking(static c => c.SaveChangesAsync()).Should().ThrowAsync<DbUpdateException>();

        CampaignPersistenceErrors.From(exception.Which).Should().Be(CampaignPersistenceError.NameConflict);
        (await context.Campaigns.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task ConcurrentDuplicateCampaignNamesAreProtectedByTheDatabase()
    {
        var first = InsertCampaign("Ash Crown");
        var second = InsertCampaign("ASH CROWN");

        var outcomes = await Task.WhenAll(Capture(first), Capture(second));

        outcomes.Should().ContainSingle(static outcome => outcome == null);
        outcomes.OfType<DbUpdateException>()
            .Should().ContainSingle(exception => CampaignPersistenceErrors.From(exception) == CampaignPersistenceError.NameConflict);
        await using var context = CreateContext();
        (await context.Campaigns.CountAsync()).Should().Be(1);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Campaigns", _postgres.GetConnectionString()));
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private CampaignsDbContext CreateContext() => _factory.Services.GetRequiredService<CampaignsDbContext>();

    private async Task InsertCampaign(string name)
    {
        await using var context = CreateContext();
        context.Campaigns.Add(Campaign.Create(CampaignName.Create(name).Value!, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
    }

    private static async Task<Exception?> Capture(Task task)
    {
        try
        {
            await task;
            return null;
        }
        catch (Exception exception)
        {
            return exception;
        }
    }
}
