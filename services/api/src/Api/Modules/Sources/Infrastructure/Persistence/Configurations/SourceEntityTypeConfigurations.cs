using Api.Modules.Sources.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Api.Modules.Sources.Infrastructure.Persistence;

namespace Api.Modules.Sources.Infrastructure.Persistence.Configurations;

internal sealed class SourceEntityTypeConfiguration : IEntityTypeConfiguration<Source>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("sources", table =>
        {
            table.HasCheckConstraint("ck_sources_title_length", "char_length(title) BETWEEN 1 AND 120");
            table.HasCheckConstraint("ck_sources_origin", $"origin IN ({SnakeCaseEnumConverter<SourceOrigin>.SqlList})");
            table.HasCheckConstraint("ck_sources_category", $"category IS NULL OR category IN ({SnakeCaseEnumConverter<SourceCategory>.SqlList})");
            table.HasCheckConstraint("ck_sources_knowledge_status", $"knowledge_status IN ({SnakeCaseEnumConverter<KnowledgeStatus>.SqlList})");
        });

        builder.HasKey(source => source.Id);
        builder.HasAlternateKey(source => new { source.Id, source.CampaignId });
        builder.HasIndex(source => new { source.CampaignId, source.CreatedAt });

        builder.Property(source => source.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(source => source.CampaignId).HasColumnName("campaign_id");
        builder.Property(source => source.Title)
            .HasColumnName("title")
            .HasColumnType("varchar(120)")
            .HasConversion(title => title.Value, value => SourceTitle.Create(value).Value!);
        builder.Property(source => source.Origin).HasColumnName("origin").HasColumnType("varchar(16)")
            .HasConversion(new SnakeCaseEnumConverter<SourceOrigin>());
        builder.Property(source => source.Category).HasColumnName("category").HasColumnType("varchar(32)")
            .HasConversion(new SnakeCaseEnumConverter<SourceCategory>());
        builder.Property(source => source.KnowledgeStatus).HasColumnName("knowledge_status").HasColumnType("varchar(16)")
            .HasConversion(new SnakeCaseEnumConverter<KnowledgeStatus>());
        builder.Property(source => source.PublishedVersionId).HasColumnName("published_version_id");
        builder.Property(source => source.LastPublishedAt).HasColumnName("last_published_at");
        builder.Property(source => source.RemovedAt).HasColumnName("removed_at");
        builder.Property(source => source.CreatedAt).HasColumnName("created_at");
        builder.Property(source => source.UpdatedAt).HasColumnName("updated_at");
        builder.Ignore(source => source.IsRemoved);

        builder.HasOne<SourceVersion>()
            .WithMany()
            .HasForeignKey(source => new { source.PublishedVersionId, source.CampaignId })
            .HasPrincipalKey(version => new { version.Id, version.CampaignId })
            .HasConstraintName("fk_sources_published_version")
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SourceVersionEntityTypeConfiguration : IEntityTypeConfiguration<SourceVersion>
{
    public void Configure(EntityTypeBuilder<SourceVersion> builder)
    {
        builder.ToTable("source_versions", table =>
            table.HasCheckConstraint("ck_source_versions_content_hash", "content_hash ~ '^[0-9a-f]{64}$'"));

        builder.HasKey(version => version.Id);
        builder.HasAlternateKey(version => new { version.Id, version.CampaignId });

        builder.Property(version => version.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(version => version.CampaignId).HasColumnName("campaign_id");
        builder.Property(version => version.SourceId).HasColumnName("source_id");
        builder.Property(version => version.OriginalText).HasColumnName("original_text");
        builder.Property(version => version.ContentHash).HasColumnName("content_hash").HasColumnType("char(64)");
        builder.Property(version => version.Frontmatter).HasColumnName("frontmatter");
        builder.Property(version => version.FileName).HasColumnName("file_name");
        builder.Property(version => version.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(version => new { version.CampaignId, version.ContentHash });

        builder.HasOne<Source>()
            .WithMany()
            .HasForeignKey(version => new { version.SourceId, version.CampaignId })
            .HasPrincipalKey(source => new { source.Id, source.CampaignId })
            .HasConstraintName("fk_source_versions_source")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SourceChunkEntityTypeConfiguration : IEntityTypeConfiguration<SourceChunk>
{
    public void Configure(EntityTypeBuilder<SourceChunk> builder)
    {
        builder.ToTable("source_chunks", table =>
            table.HasCheckConstraint("ck_source_chunks_lines", "start_line >= 1 AND end_line >= start_line"));

        builder.HasKey(chunk => chunk.Id);
        builder.HasIndex(chunk => new { chunk.SourceVersionId, chunk.Ordinal }).IsUnique();

        builder.Property(chunk => chunk.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(chunk => chunk.CampaignId).HasColumnName("campaign_id");
        builder.Property(chunk => chunk.SourceVersionId).HasColumnName("source_version_id");
        builder.Property(chunk => chunk.Ordinal).HasColumnName("ordinal");
        builder.Property(chunk => chunk.HeadingPath).HasColumnName("heading_path");
        builder.Property(chunk => chunk.StartLine).HasColumnName("start_line");
        builder.Property(chunk => chunk.EndLine).HasColumnName("end_line");
        builder.Property(chunk => chunk.Text).HasColumnName("text");

        builder.HasOne<SourceVersion>()
            .WithMany()
            .HasForeignKey(chunk => new { chunk.SourceVersionId, chunk.CampaignId })
            .HasPrincipalKey(version => new { version.Id, version.CampaignId })
            .HasConstraintName("fk_source_chunks_version")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SourceIngestionJobEntityTypeConfiguration : IEntityTypeConfiguration<SourceIngestionJob>
{
    public void Configure(EntityTypeBuilder<SourceIngestionJob> builder)
    {
        builder.ToTable("source_ingestion_jobs", table =>
        {
            table.HasCheckConstraint("ck_source_ingestion_jobs_state", $"state IN ({SnakeCaseEnumConverter<IngestionJobState>.SqlList})");
            table.HasCheckConstraint("ck_source_ingestion_jobs_attempts", "attempts >= 0");
        });

        builder.HasKey(job => job.Id);
        builder.HasIndex(job => job.SourceVersionId).IsUnique();
        builder.HasIndex(job => new { job.State, job.NextAttemptAt });

        builder.Property(job => job.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(job => job.CampaignId).HasColumnName("campaign_id");
        builder.Property(job => job.SourceVersionId).HasColumnName("source_version_id");
        builder.Property(job => job.State).HasColumnName("state").HasColumnType("varchar(16)")
            .HasConversion(new SnakeCaseEnumConverter<IngestionJobState>());
        builder.Property(job => job.Attempts).HasColumnName("attempts");
        builder.Property(job => job.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(job => job.LeaseExpiresAt).HasColumnName("lease_expires_at");
        builder.Property(job => job.ErrorCode).HasColumnName("error_code").HasColumnType("varchar(64)");
        builder.Property(job => job.CreatedAt).HasColumnName("created_at");
        builder.Property(job => job.UpdatedAt).HasColumnName("updated_at").IsConcurrencyToken();

        builder.HasOne<SourceVersion>()
            .WithMany()
            .HasForeignKey(job => new { job.SourceVersionId, job.CampaignId })
            .HasPrincipalKey(version => new { version.Id, version.CampaignId })
            .HasConstraintName("fk_source_ingestion_jobs_version")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
