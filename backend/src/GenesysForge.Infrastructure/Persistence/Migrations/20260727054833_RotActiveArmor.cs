using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotActiveArmor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ActiveArmorCharacterItemId",
                table: "Characters",
                type: "uuid",
                nullable: true);

            // Бэкфилл ROT-CMB-02: у персонажа с уже надетой бронёй выбор делается детерминированно —
            // максимальное поглощение, затем максимальная применимая защита, затем стабильный id.
            // Предметы и их состояния не меняются: выбирается только одна из уже надетых броней.
            migrationBuilder.Sql("""
                UPDATE "Characters" AS c
                SET "ActiveArmorCharacterItemId" = (
                    SELECT ci."Id"
                    FROM "CharacterItems" AS ci
                    JOIN "ItemDefs" AS d ON d."Id" = ci."ItemDefId"
                    WHERE ci."CharacterId" = c."Id"
                      AND ci."State" = 0
                      AND d."Kind" = 1
                      AND ci."Quantity" >= 1
                    ORDER BY d."SoakBonus" DESC,
                             GREATEST(d."MeleeDefense", d."RangedDefense") DESC,
                             ci."Id"
                    LIMIT 1)
                WHERE c."ActiveArmorCharacterItemId" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveArmorCharacterItemId",
                table: "Characters");
        }
    }
}
