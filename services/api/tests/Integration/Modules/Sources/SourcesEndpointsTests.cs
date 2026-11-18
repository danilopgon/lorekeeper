using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Api.Modules.Sources.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Modules.Sources;

public sealed class SourcesEndpointsTests : IAsyncLifetime
{
    private SourcesApi _api = null!;

    public async Task InitializeAsync() => _api = await SourcesApi.StartAsync();

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task PastedTextIsQueuedThenBecomesAvailableWithChunksOfThePublishedVersion()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        using var response = await _api.Client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/sources/pastes",
            new { title = "  Ideas para la sesión 4 ", text = "Primera idea\n\nSegunda idea", category = "preparation", knowledgeStatus = "preparation" });

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var accepted = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        accepted["campaignId"]!.GetValue<string>().Should().Be(campaignId);
        accepted["title"]!.GetValue<string>().Should().Be("Ideas para la sesión 4");
        accepted["origin"]!.GetValue<string>().Should().Be("paste");
        accepted["category"]!.GetValue<string>().Should().Be("preparation");
        accepted["knowledgeStatus"]!.GetValue<string>().Should().Be("preparation");
        accepted["availability"]!.GetValue<string>().Should().Be("unavailable");
        accepted["lastPublishedAt"].Should().BeNull();
        accepted["processing"]!["state"]!.GetValue<string>().Should().BeOneOf("queued", "processing");

        var sourceId = accepted["id"]!.GetValue<string>();
        var available = await _api.WaitForSourceAsync(campaignId, sourceId, source => source["availability"]!.GetValue<string>() == "available");

        available["processing"].Should().BeNull();
        available["lastPublishedAt"].Should().NotBeNull();

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var source = await db.Sources.SingleAsync();
        var chunk = await db.Chunks.SingleAsync();
        chunk.SourceVersionId.Should().Be(source.PublishedVersionId!.Value);
        chunk.Should().BeEquivalentTo(new { StartLine = 1, EndLine = 3, Text = "Primera idea\n\nSegunda idea" });
        (await db.Jobs.SingleAsync()).State.ToString().Should().Be("Succeeded");
    }

    [Fact]
    public async Task ListShowsOnlyTheCampaignsOwnSourcesNewestFirst()
    {
        var ashCrown = await _api.CreateCampaignAsync("Ash Crown");
        var greyMarch = await _api.CreateCampaignAsync("Grey March");
        var first = await _api.PasteAsync(ashCrown, "Mirela", "Mirela guarda el puente.");
        var second = await _api.PasteAsync(ashCrown, "Taberna", "La taberna del cruce.");
        var other = await _api.PasteAsync(greyMarch, "Mirela", "Mirela es la reina gris.");

        var ashCrownSources = await _api.ListAsync(ashCrown);
        var greyMarchSources = await _api.ListAsync(greyMarch);

        ashCrownSources.Select(source => source!["id"]!.GetValue<string>())
            .Should().Equal(second["id"]!.GetValue<string>(), first["id"]!.GetValue<string>());
        greyMarchSources.Select(source => source!["id"]!.GetValue<string>()).Should().Equal(other["id"]!.GetValue<string>());
        greyMarchSources.Single()!["campaignId"]!.GetValue<string>().Should().Be(greyMarch);
    }

    [Fact]
    public async Task ListStartsEmpty()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        (await _api.ListAsync(campaignId)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-uuid", HttpStatusCode.BadRequest, "campaign_id_invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111", HttpStatusCode.NotFound, "campaign_not_found")]
    public async Task SourceRoutesRequireAnExistingCampaign(string campaignId, HttpStatusCode status, string code)
    {
        using var list = await _api.Client.GetAsync($"/api/campaigns/{campaignId}/sources");
        using var paste = await _api.Client.PostAsJsonAsync($"/api/campaigns/{campaignId}/sources/pastes", new { title = "Ideas", text = "Texto" });

        foreach (var response in new[] { list, paste })
        {
            response.StatusCode.Should().Be(status);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            (await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]!.GetValue<string>().Should().Be(code);
        }
    }

    [Fact]
    public async Task InvalidPastesAreRejectedWithFieldErrorsAndNothingIsStored()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        using var response = await _api.Client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/sources/pastes",
            new { title = " ", text = new string('a', 1_048_577), category = "rumours" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
        problem["code"]!.GetValue<string>().Should().Be("source_title_invalid");
        problem["errors"]!["title"]![0]!.GetValue<string>().Should().Be("source_title_invalid");
        problem["errors"]!["text"]![0]!.GetValue<string>().Should().Be("source_too_large");
        problem["errors"]!["category"]![0]!.GetValue<string>().Should().Be("source_category_invalid");
        (await _api.ListAsync(campaignId)).Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyTextIsRejected()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        using var response = await _api.Client.PostAsJsonAsync($"/api/campaigns/{campaignId}/sources/pastes", new { title = "Ideas", text = "  \n " });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonObject>())!["code"]!.GetValue<string>().Should().Be("source_empty");
    }
}
