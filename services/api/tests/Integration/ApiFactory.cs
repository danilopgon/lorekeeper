using Api.Modules.Sources.Infrastructure.Ingestion;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Integration;

internal static class ApiFactory
{
    /// <summary>A factory for tests without a database: the ingestion worker never polls the operator's local database.</summary>
    public static WebApplicationFactory<Program> WithoutIngestionWorker(this WebApplicationFactory<Program> factory) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(RemoveIngestionWorker));

    public static void RemoveIngestionWorker(IServiceCollection services) =>
        services.Remove(services.Single(descriptor => descriptor.ImplementationType == typeof(SourceIngestionWorker)));
}
