using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeNpcStrain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE "Npcs" SET "StrainThreshold" = NULL
                WHERE "Kind" <> 2 AND "StrainThreshold" IS NOT NULL;

                UPDATE "GameParticipants" SET "StrainThreshold" = NULL, "StrainCurrent" = 0
                WHERE ("ParticipantType" = 2 OR "NpcId" IN (SELECT "Id" FROM "Npcs" WHERE "Kind" <> 2))
                  AND ("StrainThreshold" IS NOT NULL OR "StrainCurrent" <> 0);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data-only normalization: rollback must not recreate invalid thresholds.
        }
    }
}
