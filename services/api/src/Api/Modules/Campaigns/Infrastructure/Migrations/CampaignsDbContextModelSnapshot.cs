using Api.Modules.Campaigns.Domain;
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

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.ToTable("campaigns", table =>
            {
                table.HasCheckConstraint("ck_campaigns_name_trimmed", "name = btrim(name, E' \\t\\n\\r\\f\\013')");
                table.HasCheckConstraint("ck_campaigns_name_length", "char_length(name) BETWEEN 1 AND 120");
            });

            entity.HasKey(campaign => campaign.Id);

            entity.Property(campaign => campaign.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .ValueGeneratedNever();

            entity.Property(campaign => campaign.Name)
                .HasColumnName("name")
                .HasColumnType("varchar(120)")
                .HasConversion(new ValueConverter<CampaignName, string>(
                    name => name.Value,
                    value => CampaignName.Create(value).Value!))
                .IsRequired();

            entity.Property(campaign => campaign.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property(campaign => campaign.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();

            entity.Property<string>("NameNormalized")
                .HasColumnName("name_normalized")
                .HasColumnType("varchar(120)")
                .HasComputedColumnSql("lower(btrim(name, E' \\t\\n\\r\\f\\013'))", stored: true);

            entity.HasIndex("NameNormalized")
                .IsUnique()
                .HasDatabaseName("ux_campaigns_name_normalized");
        });
    }
}
