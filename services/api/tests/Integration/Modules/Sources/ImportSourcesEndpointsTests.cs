using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Api.Modules.Sources.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Modules.Sources;

public sealed class ImportSourcesEndpointsTests : IAsyncLifetime
{
    private static readonly byte[] ByteOrderMark = [0xEF, 0xBB, 0xBF];

    private SourcesApi _api = null!;

    public async Task InitializeAsync() => _api = await SourcesApi.StartAsync();

    public async Task DisposeAsync() => await _api.DisposeAsync();

    [Fact]
    public async Task ABodyAboveTheImportLimitIsRejectedWith413()
    {
        // The in-memory TestServer ignores request body size limits, so this runs against Kestrel.
        await using var kestrel = await SourcesApi.StartAsync(runWorker: false, useKestrel: true);
        var campaignId = await kestrel.CreateCampaignAsync("Ash Crown");
        var oversized = Enumerable.Range(0, 2).Select(index => new UploadFile($"parte-{index}.txt", new byte[27 * 1024 * 1024])).ToList();

        using var response = await ImportAsync(kestrel.Client, campaignId, oversized, Metadata(oversized.Count));

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ABodyJustBelowTheImportLimitIsAccepted()
    {
        await using var kestrel = await SourcesApi.StartAsync(runWorker: false, useKestrel: true);
        var campaignId = await kestrel.CreateCampaignAsync("Ash Crown");
        var files = Enumerable.Range(0, 50).Select(index => new UploadFile($"parte-{index}.txt", Encoding.UTF8.GetBytes(new string('a', 1_048_576)))).ToList();

        using var response = await ImportAsync(kestrel.Client, campaignId, files, Metadata(files.Count));

        response.StatusCode.Should().Be(HttpStatusCode.Accepted, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EachFileIsAcceptedOrRejectedOnItsOwnAndAcceptedOnesBecomeAvailable()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        UploadFile[] files =
        [
            Utf8("sesion-3.md", "---\ntags: [sesion]\n---\n# Sesión 3\n\n## Taberna\n\nMirela entra."),
            Utf8("notas.TXT", "Primera nota.\n\nSegunda nota."),
            Utf8("mapa.pdf", "%PDF"),
            new("roto.md", [0xC3, 0x28]),
            new("vacio.txt", []),
            Utf8("sin-titulo.md", "# Algo"),
            Utf8("solo-frontmatter.md", "---\ntitle: x\n---\n\n"),
            new("enorme.txt", Encoding.UTF8.GetBytes(new string('a', 1_048_577)))
        ];
        var metadata = Metadata(files.Length);
        metadata[0] = new { title = "Sesión 3", category = "session_notes", knowledgeStatus = "occurred" };
        metadata[5] = new { title = " ", category = (string?)null, knowledgeStatus = "unknown" };

        using var response = await ImportAsync(campaignId, files, metadata);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var items = (await response.Content.ReadFromJsonAsync<JsonObject>())!["items"]!.AsArray();
        items.Select(item => (item!["index"]!.GetValue<int>(), item["fileName"]!.GetValue<string>(), item["outcome"]!.GetValue<string>(), item["errorCode"]?.GetValue<string>()))
            .Should().Equal(
                (0, "sesion-3.md", "accepted", null),
                (1, "notas.TXT", "accepted", null),
                (2, "mapa.pdf", "rejected", "source_file_type_unsupported"),
                (3, "roto.md", "rejected", "source_encoding_invalid"),
                (4, "vacio.txt", "rejected", "source_empty"),
                (5, "sin-titulo.md", "rejected", "source_title_invalid"),
                (6, "solo-frontmatter.md", "rejected", "source_empty"),
                (7, "enorme.txt", "rejected", "source_too_large"));
        items[0]!["source"]!["origin"]!.GetValue<string>().Should().Be("file");
        items[2]!["source"].Should().BeNull();

        var markdownId = items[0]!["source"]!["id"]!.GetValue<string>();
        await _api.WaitForSourceAsync(campaignId, markdownId, source => source["availability"]!.GetValue<string>() == "available");
        await _api.WaitForSourceAsync(campaignId, items[1]!["source"]!["id"]!.GetValue<string>(), source => source["availability"]!.GetValue<string>() == "available");
        (await _api.ListAsync(campaignId)).Should().HaveCount(2);

        await using var scope = _api.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SourcesDbContext>();
        var version = await db.Versions.SingleAsync(version => version.FileName == "sesion-3.md");
        version.Frontmatter.Should().Be("tags: [sesion]");
        var chunks = await db.Chunks.Where(chunk => chunk.SourceVersionId == version.Id).OrderBy(chunk => chunk.Ordinal).ToListAsync();
        chunks.Select(chunk => (chunk.StartLine, chunk.EndLine, string.Join(" > ", chunk.HeadingPath)))
            .Should().Equal((4, 4, "Sesión 3"), (6, 8, "Sesión 3 > Taberna"));
    }

    [Fact]
    public async Task TheSizeLimitAllowsExactlyOneMebibyteOfTextPlusAByteOrderMark()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var atLimit = Encoding.UTF8.GetBytes(new string('a', 1_048_576));
        UploadFile[] files = [new("con-bom.txt", [.. ByteOrderMark, .. atLimit]), new("sin-bom.txt", [.. atLimit, (byte)'a'])];

        using var response = await ImportAsync(campaignId, files, Metadata(files.Length));

        var items = (await response.Content.ReadFromJsonAsync<JsonObject>())!["items"]!.AsArray();
        items[0]!["outcome"]!.GetValue<string>().Should().Be("accepted");
        items[1]!["errorCode"]!.GetValue<string>().Should().Be("source_too_large");
    }

    [Fact]
    public async Task DuplicateContentIsImportedAgainAsANewSource()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        UploadFile[] files = [Utf8("a.md", "# Igual"), Utf8("b.md", "# Igual")];

        using var response = await ImportAsync(campaignId, files, Metadata(files.Length));

        var items = (await response.Content.ReadFromJsonAsync<JsonObject>())!["items"]!.AsArray();
        items.Select(item => item!["outcome"]!.GetValue<string>()).Should().Equal("accepted", "accepted");
        (await _api.ListAsync(campaignId)).Should().HaveCount(2);
    }

    [Fact]
    public async Task RequestShapeFailuresRejectTheWholeBatch()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");
        var one = new[] { Utf8("a.md", "# A") };
        var tooMany = Enumerable.Range(0, 51).Select(index => Utf8($"{index}.md", $"# {index}")).ToArray();

        (await CodeOf(await ImportAsync(campaignId, [], Metadata(0)))).Should().Be("source_batch_empty");
        (await CodeOf(await ImportAsync(campaignId, tooMany, Metadata(51)))).Should().Be("source_batch_too_large");
        (await CodeOf(await ImportAsync(campaignId, one, metadata: null))).Should().Be("source_metadata_invalid");
        (await CodeOf(await ImportAsync(campaignId, one, Metadata(2)))).Should().Be("source_metadata_invalid");
        (await CodeOf(await ImportAsync(campaignId, one, rawMetadata: "{\"title\":\"A\"}"))).Should().Be("source_metadata_invalid");
        (await CodeOf(await ImportAsync(campaignId, one, rawMetadata: "[{\"title\":5}]"))).Should().Be("source_metadata_invalid");
        (await _api.ListAsync(campaignId)).Should().BeEmpty();
    }

    [Theory]
    [InlineData("not-a-uuid", HttpStatusCode.BadRequest, "campaign_id_invalid")]
    [InlineData("11111111-1111-1111-1111-111111111111", HttpStatusCode.NotFound, "campaign_not_found")]
    public async Task ImportsAndContentMatchesRequireAnExistingCampaign(string campaignId, HttpStatusCode status, string code)
    {
        using var import = await ImportAsync(campaignId, [Utf8("a.md", "# A")], Metadata(1));
        using var matches = await _api.Client.PostAsJsonAsync($"/api/campaigns/{campaignId}/sources/content-matches", new { hashes = new[] { Hash("x") } });

        foreach (var response in new[] { import, matches })
        {
            response.StatusCode.Should().Be(status);
            (await CodeOf(response)).Should().Be(code);
        }
    }

    [Fact]
    public async Task ContentMatchesOnlyReportNonRemovedSourcesOfTheSameCampaign()
    {
        var ashCrown = await _api.CreateCampaignAsync("Ash Crown");
        var greyMarch = await _api.CreateCampaignAsync("Grey March");
        var kept = await _api.PasteAsync(ashCrown, "Mirela", "Mirela guarda el puente.");
        var removed = await _api.PasteAsync(ashCrown, "Taberna", "La taberna del cruce.");
        await _api.PasteAsync(greyMarch, "Reina", "Solo en Grey March.");
        await using (var scope = _api.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE sources SET removed_at = now() WHERE id = {Guid.Parse(removed["id"]!.GetValue<string>())}");
        }

        using var response = await _api.Client.PostAsJsonAsync(
            $"/api/campaigns/{ashCrown}/sources/content-matches",
            new { hashes = new[] { Hash("Mirela guarda el puente."), Hash("La taberna del cruce."), Hash("Solo en Grey March."), Hash("nada") } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var matches = (await response.Content.ReadFromJsonAsync<JsonObject>())!["matches"]!.AsArray();
        matches.Select(match => (match!["hash"]!.GetValue<string>(), match["sourceId"]!.GetValue<string>(), match["title"]!.GetValue<string>()))
            .Should().Equal((Hash("Mirela guarda el puente."), kept["id"]!.GetValue<string>(), "Mirela"));
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[\"ABC\"]")]
    [InlineData("[\"9F86D081884C7D659A2FEAA0C55AD015A3BF4F1B2B0B822CD15D6C15B0F00A08\"]")]
    [InlineData("null")]
    public async Task MalformedHashListsAreRejected(string hashes)
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        using var response = await _api.Client.PostAsync(
            $"/api/campaigns/{campaignId}/sources/content-matches",
            new StringContent($"{{\"hashes\":{hashes}}}", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await CodeOf(response)).Should().Be("source_hashes_invalid");
    }

    [Fact]
    public async Task MoreThanFiftyHashesAreRejected()
    {
        var campaignId = await _api.CreateCampaignAsync("Ash Crown");

        using var response = await _api.Client.PostAsJsonAsync(
            $"/api/campaigns/{campaignId}/sources/content-matches",
            new { hashes = Enumerable.Range(0, 51).Select(index => Hash(index.ToString())).ToArray() });

        (await CodeOf(response)).Should().Be("source_hashes_invalid");
    }

    private sealed record UploadFile(string Name, byte[] Content);

    private static UploadFile Utf8(string name, string text) => new(name, Encoding.UTF8.GetBytes(text));

    private static object[] Metadata(int count) =>
        Enumerable.Range(0, count).Select(index => (object)new { title = $"Fuente {index}", category = (string?)null, knowledgeStatus = "unknown" }).ToArray();

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));

    private Task<HttpResponseMessage> ImportAsync(string campaignId, IEnumerable<UploadFile> files, object[]? metadata = null, string? rawMetadata = null) =>
        ImportAsync(_api.Client, campaignId, files, metadata, rawMetadata);

    private static async Task<HttpResponseMessage> ImportAsync(HttpClient client, string campaignId, IEnumerable<UploadFile> files, object[]? metadata = null, string? rawMetadata = null)
    {
        using var form = new MultipartFormDataContent();
        foreach (var file in files)
        {
            var part = new ByteArrayContent(file.Content);
            part.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            form.Add(part, "files", file.Name);
        }

        var metadataJson = rawMetadata ?? (metadata is null ? null : JsonSerializer.Serialize(metadata));
        if (metadataJson is not null)
        {
            form.Add(new StringContent(metadataJson, Encoding.UTF8, "application/json"), "metadata");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, $"/api/campaigns/{campaignId}/sources/imports") { Content = form };
        request.Headers.ExpectContinue = true;
        return await client.SendAsync(request);
    }

    private static async Task<string?> CodeOf(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonObject>())?["code"]?.GetValue<string>();
}
