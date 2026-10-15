using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCraftingPaidCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PaidCost",
                table: "CraftingProjects",
                type: "integer",
                nullable: true);

            // Preserve payments made by PR #297's earlier audit-based implementation.
            // All unpaid legacy rows stay NULL; CASE tolerates historic non-JSON values.
            migrationBuilder.Sql("""
                UPDATE "CraftingProjects" AS p
                SET "PaidCost" = p."Cost"
                WHERE EXISTS (
                    SELECT 1 FROM "CharacterAuditEntries" AS a
                    CROSS JOIN LATERAL (
                        SELECT CASE WHEN a."DataJson" IS JSON OBJECT
                            THEN a."DataJson"::jsonb ELSE '{}'::jsonb END AS data
                    ) AS payload
                    WHERE a."CharacterId" = p."CharacterId" AND a."Action" = 28
                        AND payload.data->>'projectId' = p."Id"::text
                        AND payload.data->'chargedCost' = to_jsonb(p."Cost")
                        AND p."Cost" >= 0
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaidCost",
                table: "CraftingProjects");
        }
    }
}
