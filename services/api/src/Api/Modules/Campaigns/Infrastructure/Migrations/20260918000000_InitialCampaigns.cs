using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Modules.Campaigns.Infrastructure.Migrations;

public partial class InitialCampaigns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "campaigns",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "varchar(120)", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                name_normalized = table.Column<string>(type: "varchar(120)", nullable: true, computedColumnSql: "lower(btrim(name, E' \\t\\n\\r\\f\\013'))", stored: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_campaigns", x => x.id);
                table.CheckConstraint("ck_campaigns_name_length", "char_length(name) BETWEEN 1 AND 120");
                table.CheckConstraint("ck_campaigns_name_trimmed", "name = btrim(name, E' \\t\\n\\r\\f\\013')");
            });

        migrationBuilder.CreateIndex(
            name: "ux_campaigns_name_normalized",
            table: "campaigns",
            column: "name_normalized",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "campaigns");
    }
}
