using Api.Modules.Campaigns.Infrastructure;
using Api.Modules.Sources.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration;

internal static class TestDatabase
{
    public static async Task MigrateAsync(IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<CampaignsDbContext>().Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<SourcesDbContext>().Database.MigrateAsync();
    }
}
