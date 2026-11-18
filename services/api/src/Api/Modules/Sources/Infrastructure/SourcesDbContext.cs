using Api.Modules.Sources.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Sources.Infrastructure;

public sealed class SourcesDbContext(DbContextOptions<SourcesDbContext> options) : DbContext(options)
{
    public const string MigrationsHistoryTable = "__EFMigrationsHistory_Sources";

    public DbSet<Source> Sources => Set<Source>();
    public DbSet<SourceVersion> Versions => Set<SourceVersion>();
    public DbSet<SourceChunk> Chunks => Set<SourceChunk>();
    public DbSet<SourceIngestionJob> Jobs => Set<SourceIngestionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SourcesDbContext).Assembly,
            type => type.Namespace == typeof(SourcesDbContext).Namespace);
    }
}
