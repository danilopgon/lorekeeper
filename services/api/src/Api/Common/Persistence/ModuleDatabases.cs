using Microsoft.EntityFrameworkCore;

namespace Api.Common.Persistence;

public sealed record ModuleDatabase(Type ContextType);

/// <summary>Each module registers its DbContext here; the <c>migrate</c> command applies them in registration order.</summary>
public static class ModuleDatabases
{
    public const string MigrateCommand = "migrate";

    public static IServiceCollection AddModuleDatabase<TContext>(this IServiceCollection services)
        where TContext : DbContext =>
        services.AddSingleton(new ModuleDatabase(typeof(TContext)));

    public static async Task MigrateModuleDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        foreach (var database in scope.ServiceProvider.GetServices<ModuleDatabase>())
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(database.ContextType);
            await context.Database.MigrateAsync(cancellationToken);
        }
    }
}
