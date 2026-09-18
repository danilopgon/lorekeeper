using Api.Modules.Campaigns.Domain;
using Microsoft.EntityFrameworkCore;

namespace Api.Modules.Campaigns.Infrastructure;

public sealed class CampaignsDbContext(DbContextOptions<CampaignsDbContext> options) : DbContext(options)
{
    public DbSet<Campaign> Campaigns => Set<Campaign>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CampaignEntityTypeConfiguration());
    }
}
