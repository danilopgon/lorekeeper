using Api.Common.Persistence;
using Api.Modules.Sources.Application;
using Api.Modules.Sources.Application.ListSources;
using Api.Modules.Sources.Application.PasteSource;
using Api.Modules.Sources.Application.ProcessSourceVersion;
using Api.Modules.Sources.Infrastructure.Persistence;
using Api.Modules.Sources.Infrastructure.Ingestion;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Api.Modules.Sources;

public static class SourcesModule
{
    public static IServiceCollection AddSourcesModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<SourcesDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("Campaigns")
                ?? throw new InvalidOperationException("Connection string 'Campaigns' is not configured.");
            options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable(SourcesDbContext.MigrationsHistoryTable));
        });

        services.AddModuleDatabase<SourcesDbContext>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ISourceRepository, SourceRepository>();
        services.AddScoped<ISourceQueries, SourceQueries>();
        services.AddScoped<PasteSourceHandler>();
        services.AddScoped<ListSourcesHandler>();
        services.AddScoped<ISourceVersionProcessor, ProcessSourceVersionHandler>();

        services.Configure<SourceIngestionOptions>(configuration.GetSection(SourceIngestionOptions.SectionName));
        services.AddScoped<IngestionJobQueue>();
        services.AddSingleton<SourceIngestionRunner>();
        services.AddHostedService<SourceIngestionWorker>();

        return services;
    }
}
