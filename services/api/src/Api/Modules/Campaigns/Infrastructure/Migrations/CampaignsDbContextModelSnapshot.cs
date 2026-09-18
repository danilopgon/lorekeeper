using System;
using Api.Modules.Campaigns.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Api.Modules.Campaigns.Infrastructure.Migrations;

[DbContext(typeof(CampaignsDbContext))]
public partial class CampaignsDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "10.0.0");
        new CampaignEntityTypeConfiguration().Configure(modelBuilder.Entity<Api.Modules.Campaigns.Domain.Campaign>());
    }
}
