using Api.Modules.Campaigns.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Api.Modules.Campaigns.Infrastructure;

public sealed class CampaignEntityTypeConfiguration : IEntityTypeConfiguration<Campaign>
{
    private const string TrimCharactersSql = "E' \\t\\n\\r\\f\\013'";

    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns", table =>
        {
            table.HasCheckConstraint("ck_campaigns_name_trimmed", $"name = btrim(name, {TrimCharactersSql})");
            table.HasCheckConstraint("ck_campaigns_name_length", "char_length(name) BETWEEN 1 AND 120");
        });

        builder.HasKey(campaign => campaign.Id);

        builder.Property(campaign => campaign.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(campaign => campaign.Name)
            .HasColumnName("name")
            .HasColumnType("varchar(120)")
            .HasConversion(
                name => name.Value,
                value => CampaignName.Create(value).Value!)
            .IsRequired();

        builder.Property(campaign => campaign.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(campaign => campaign.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property<string>("NameNormalized")
            .HasColumnName("name_normalized")
            .HasColumnType("varchar(120)")
            .HasComputedColumnSql($"lower(btrim(name, {TrimCharactersSql}))", stored: true);

        builder.HasIndex("NameNormalized")
            .IsUnique()
            .HasDatabaseName("ux_campaigns_name_normalized");
    }
}
