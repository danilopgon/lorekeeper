using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.Sources.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "source_chunks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ordinal = table.Column<int>(type: "integer", nullable: false),
                    heading_path = table.Column<string[]>(type: "text[]", nullable: false),
                    start_line = table.Column<int>(type: "integer", nullable: false),
                    end_line = table.Column<int>(type: "integer", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_chunks", x => x.id);
                    table.CheckConstraint("ck_source_chunks_lines", "start_line >= 1 AND end_line >= start_line");
                });

            migrationBuilder.CreateTable(
                name: "source_ingestion_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    state = table.Column<string>(type: "varchar(16)", nullable: false),
                    attempts = table.Column<int>(type: "integer", nullable: false),
                    next_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lease_expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error_code = table.Column<string>(type: "varchar(64)", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_ingestion_jobs", x => x.id);
                    table.CheckConstraint("ck_source_ingestion_jobs_attempts", "attempts >= 0");
                    table.CheckConstraint("ck_source_ingestion_jobs_state", "state IN ('queued', 'processing', 'succeeded', 'failed')");
                });

            migrationBuilder.CreateTable(
                name: "source_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_text = table.Column<string>(type: "text", nullable: false),
                    content_hash = table.Column<string>(type: "char(64)", nullable: false),
                    frontmatter = table.Column<string>(type: "text", nullable: true),
                    file_name = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_source_versions", x => x.id);
                    table.UniqueConstraint("AK_source_versions_id_campaign_id", x => new { x.id, x.campaign_id });
                    table.CheckConstraint("ck_source_versions_content_hash", "content_hash ~ '^[0-9a-f]{64}$'");
                });

            migrationBuilder.CreateTable(
                name: "sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "varchar(120)", nullable: false),
                    origin = table.Column<string>(type: "varchar(16)", nullable: false),
                    category = table.Column<string>(type: "varchar(32)", nullable: true),
                    knowledge_status = table.Column<string>(type: "varchar(16)", nullable: false),
                    published_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    removed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sources", x => x.id);
                    table.UniqueConstraint("AK_sources_id_campaign_id", x => new { x.id, x.campaign_id });
                    table.CheckConstraint("ck_sources_category", "category IS NULL OR category IN ('session_notes', 'preparation', 'characters_places', 'general_reference')");
                    table.CheckConstraint("ck_sources_knowledge_status", "knowledge_status IN ('unknown', 'preparation', 'occurred')");
                    table.CheckConstraint("ck_sources_origin", "origin IN ('file', 'paste')");
                    table.CheckConstraint("ck_sources_title_length", "char_length(title) BETWEEN 1 AND 120");
                    table.ForeignKey(
                        name: "fk_sources_published_version",
                        columns: x => new { x.published_version_id, x.campaign_id },
                        principalTable: "source_versions",
                        principalColumns: new[] { "id", "campaign_id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_source_chunks_source_version_id_campaign_id",
                table: "source_chunks",
                columns: new[] { "source_version_id", "campaign_id" });

            migrationBuilder.CreateIndex(
                name: "IX_source_chunks_source_version_id_ordinal",
                table: "source_chunks",
                columns: new[] { "source_version_id", "ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_ingestion_jobs_source_version_id",
                table: "source_ingestion_jobs",
                column: "source_version_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_source_ingestion_jobs_source_version_id_campaign_id",
                table: "source_ingestion_jobs",
                columns: new[] { "source_version_id", "campaign_id" });

            migrationBuilder.CreateIndex(
                name: "IX_source_ingestion_jobs_state_next_attempt_at",
                table: "source_ingestion_jobs",
                columns: new[] { "state", "next_attempt_at" });

            migrationBuilder.CreateIndex(
                name: "IX_source_versions_campaign_id_content_hash",
                table: "source_versions",
                columns: new[] { "campaign_id", "content_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_source_versions_source_id_campaign_id",
                table: "source_versions",
                columns: new[] { "source_id", "campaign_id" });

            migrationBuilder.CreateIndex(
                name: "IX_sources_campaign_id_created_at",
                table: "sources",
                columns: new[] { "campaign_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_sources_published_version_id_campaign_id",
                table: "sources",
                columns: new[] { "published_version_id", "campaign_id" });

            migrationBuilder.AddForeignKey(
                name: "fk_sources_campaign",
                table: "sources",
                column: "campaign_id",
                principalTable: "campaigns",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_source_chunks_version",
                table: "source_chunks",
                columns: new[] { "source_version_id", "campaign_id" },
                principalTable: "source_versions",
                principalColumns: new[] { "id", "campaign_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_source_ingestion_jobs_version",
                table: "source_ingestion_jobs",
                columns: new[] { "source_version_id", "campaign_id" },
                principalTable: "source_versions",
                principalColumns: new[] { "id", "campaign_id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_source_versions_source",
                table: "source_versions",
                columns: new[] { "source_id", "campaign_id" },
                principalTable: "sources",
                principalColumns: new[] { "id", "campaign_id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sources_campaign",
                table: "sources");

            migrationBuilder.DropForeignKey(
                name: "fk_sources_published_version",
                table: "sources");

            migrationBuilder.DropTable(
                name: "source_chunks");

            migrationBuilder.DropTable(
                name: "source_ingestion_jobs");

            migrationBuilder.DropTable(
                name: "source_versions");

            migrationBuilder.DropTable(
                name: "sources");
        }
    }
}
