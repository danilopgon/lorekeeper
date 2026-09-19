using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Modules.Campaigns.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Integration.Modules.Campaigns;

public sealed class CampaignEndpointsTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("lorekeeper_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [Fact]
    public async Task ListCampaignsStartsEmptyAndHasNoImplicitActiveCampaign()
    {
        using var response = await _client.GetAsync("/api/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("[]");
        content.Should().NotContain("activeCampaign", "Block 01 never infers an active campaign");
    }

    [Fact]
    public async Task CreateCampaignReturnsDtoLocationAndUtcTimestamps()
    {
        using var response = await _client.PostAsJsonAsync("/api/campaigns", new { name = "  Ash Crown  " });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.OriginalString.Should().StartWith("/api/campaigns/");

        var campaign = await ReadJsonObject(response);
        var id = campaign["id"]!.GetValue<string>();
        id.Should().MatchRegex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$");
        campaign["name"]!.GetValue<string>().Should().Be("Ash Crown");
        campaign["createdAt"]!.GetValue<DateTimeOffset>().Offset.Should().Be(TimeSpan.Zero);
        campaign["updatedAt"]!.GetValue<DateTimeOffset>().Should().Be(campaign["createdAt"]!.GetValue<DateTimeOffset>());
        campaign.ContainsKey("activeCampaignId").Should().BeFalse();
    }

    [Fact]
    public async Task ListCampaignsReturnsCreatedCampaignDtos()
    {
        await CreateCampaign("Ash Crown");
        await CreateCampaign("Bright Coast");

        using var response = await _client.GetAsync("/api/campaigns");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaigns = await ReadJsonArray(response);
        campaigns.Select(node => node!["name"]!.GetValue<string>()).Should().Equal("Ash Crown", "Bright Coast");
    }

    [Fact]
    public async Task GetCampaignReturnsCreatedCampaign()
    {
        var created = await CreateCampaign("Ash Crown");
        var id = created["id"]!.GetValue<string>();

        using var response = await _client.GetAsync($"/api/campaigns/{id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var campaign = await ReadJsonObject(response);
        campaign["id"]!.GetValue<string>().Should().Be(id);
        campaign["name"]!.GetValue<string>().Should().Be("Ash Crown");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateCampaignRejectsInvalidName(string? name)
    {
        using var response = await _client.PostAsJsonAsync("/api/campaigns", new { name });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadJsonObject(response);
        problem["code"]!.GetValue<string>().Should().Be("campaign_name_invalid");
        problem["errors"]!["name"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Contain("campaign_name_invalid");
    }

    [Fact]
    public async Task CreateCampaignRejectsDuplicateNameCaseInsensitively()
    {
        await CreateCampaign("Ash Crown");

        using var response = await _client.PostAsJsonAsync("/api/campaigns", new { name = "ash crown" });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await ReadJsonObject(response);
        problem["code"]!.GetValue<string>().Should().Be("campaign_name_conflict");
        problem["detail"]!.GetValue<string>().Should().NotContain("ux_campaigns_name_normalized");
        problem["errors"]!["name"]!.AsArray().Select(node => node!.GetValue<string>()).Should().Contain("campaign_name_conflict");
    }

    [Fact]
    public async Task GetCampaignRejectsMalformedId()
    {
        using var response = await _client.GetAsync("/api/campaigns/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await ReadJsonObject(response);
        problem["code"]!.GetValue<string>().Should().Be("campaign_id_invalid");
    }

    [Fact]
    public async Task GetCampaignReturnsNotFoundForUnknownId()
    {
        using var response = await _client.GetAsync($"/api/campaigns/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await ReadJsonObject(response);
        problem["code"]!.GetValue<string>().Should().Be("campaign_not_found");
    }

    [Theory]
    [InlineData("/api/campaigns/00000000-0000-0000-0000-000000000000/chat")]
    [InlineData("/api/campaigns/00000000-0000-0000-0000-000000000000/sources")]
    [InlineData("/api/campaigns/00000000-0000-0000-0000-000000000000/ingestion")]
    [InlineData("/api/sources")]
    [InlineData("/api/chat")]
    [InlineData("/api/retrieval")]
    [InlineData("/api/citations")]
    [InlineData("/api/accounts")]
    [InlineData("/api/teams")]
    public async Task OutOfScopeEndpointsAreNotIntroduced(string path)
    {
        using var response = await _client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCampaignEndpointIsNotIntroduced()
    {
        using var response = await _client.DeleteAsync($"/api/campaigns/{Guid.NewGuid():D}");

        response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseSetting("ConnectionStrings:Campaigns", _postgres.GetConnectionString()));
        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CampaignsDbContext>();
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<JsonObject> CreateCampaign(string name)
    {
        using var response = await _client.PostAsJsonAsync("/api/campaigns", new { name });
        response.EnsureSuccessStatusCode();
        return await ReadJsonObject(response);
    }

    private static async Task<JsonObject> ReadJsonObject(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!.AsObject();
    }

    private static async Task<JsonArray> ReadJsonArray(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonNode.Parse(content)!.AsArray();
    }
}
