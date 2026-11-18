using Api.Common.Persistence;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace Integration.Modules.Sources;

internal sealed class SourcesApi : IAsyncDisposable
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .WithDatabase("lorekeeper_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public HttpClient Client { get; private set; } = null!;

    public IServiceProvider Services => _factory.Services;

    public static async Task<SourcesApi> StartAsync(bool runWorker = true, Action<IServiceCollection>? configureServices = null)
    {
        var api = new SourcesApi();
        await api._postgres.StartAsync();
        api._factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Campaigns", api._postgres.GetConnectionString());
            builder.UseSetting("Sources:Ingestion:PollInterval", "00:00:00.050");
            builder.UseSetting("Sources:Ingestion:RetryBaseDelay", "00:00:00");
            builder.ConfigureTestServices(services =>
            {
                if (!runWorker)
                {
                    ApiFactory.RemoveIngestionWorker(services);
                }

                configureServices?.Invoke(services);
            });
        });

        await api._factory.Services.MigrateModuleDatabasesAsync();
        api.Client = api._factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        return api;
    }

    public async Task<string> CreateCampaignAsync(string name)
    {
        using var response = await Client.PostAsJsonAsync("/api/campaigns", new { name });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!["id"]!.GetValue<string>();
    }

    public async Task<JsonObject> PasteAsync(string campaignId, string title, string text)
    {
        using var response = await Client.PostAsJsonAsync($"/api/campaigns/{campaignId}/sources/pastes", new { title, text });
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);
        return (await response.Content.ReadFromJsonAsync<JsonObject>())!;
    }

    public async Task<JsonArray> ListAsync(string campaignId) =>
        (await Client.GetFromJsonAsync<JsonArray>($"/api/campaigns/{campaignId}/sources"))!;

    public async Task<JsonObject> WaitForSourceAsync(string campaignId, string sourceId, Func<JsonObject, bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var source = (await ListAsync(campaignId)).OfType<JsonObject>().Single(item => item["id"]!.GetValue<string>() == sourceId);
            if (condition(source))
            {
                return source;
            }

            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException($"Source never reached the expected state; last seen: {source.ToJsonString()}");
            }

            await Task.Delay(50);
        }
    }

    public async ValueTask DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _postgres.DisposeAsync();
    }
}
