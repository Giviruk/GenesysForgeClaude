using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotMag11RuneboundShards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Rarity",
                table: "ItemDefs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<int>(
                name: "Price",
                table: "ItemDefs",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "Purchasable",
                table: "ItemDefs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "Sellable",
                table: "ItemDefs",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ShardActivationChoice",
                table: "CharacterItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ShardConfigured",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ShardEffectAction",
                table: "CharacterItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShardEffectChoice",
                table: "CharacterItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE "ItemDefs"
                SET "Price" = NULL,
                    "Rarity" = NULL,
                    "Purchasable" = FALSE,
                    "Sellable" = FALSE
                WHERE "Code" IN (
                    'rot.item.arcane-bolt-rune', 'rot.item.blasting-rune', 'rot.item.ice-storm-rune', 'rot.item.immolation-rune',
                    'rot.item.lesser-rune', 'rot.item.lightning-strike-rune', 'rot.item.rune-of-collection', 'rot.item.rune-of-fate',
                    'rot.item.rune-of-misery', 'rot.item.soulstone-rune', 'rot.item.stasis-rune', 'rot.item.sunburst-rune',
                    'rot.item.teleportation-rune', 'rot.item.terror-rune', 'rot.item.vision-rune', 'rot.item.wanderers-stone',
                    'rot.item.ynfernael-rune'
                );

                INSERT INTO "CharacterItems" (
                    "Id", "CharacterId", "Craftsmanship", "DamageState", "ImplementChoices",
                    "ImplementConfigured", "ImplementMaterial", "IsThrown", "ItemDefId", "Provenance",
                    "Quantity", "State", "ShardActivationChoice", "ShardConfigured",
                    "ShardEffectAction", "ShardEffectChoice"
                )
                SELECT
                    gen_random_uuid(), ci."CharacterId", ci."Craftsmanship", ci."DamageState",
                    ci."ImplementChoices", ci."ImplementConfigured", ci."ImplementMaterial",
                    ci."IsThrown", ci."ItemDefId", ci."Provenance", 1, ci."State", '', FALSE, '', ''
                FROM "CharacterItems" ci
                JOIN "ItemDefs" d ON d."Id" = ci."ItemDefId"
                CROSS JOIN LATERAL generate_series(2, ci."Quantity") AS copy_number
                WHERE d."Code" IN (
                    'rot.item.arcane-bolt-rune', 'rot.item.blasting-rune', 'rot.item.ice-storm-rune', 'rot.item.immolation-rune',
                    'rot.item.lesser-rune', 'rot.item.lightning-strike-rune', 'rot.item.rune-of-collection', 'rot.item.rune-of-fate',
                    'rot.item.rune-of-misery', 'rot.item.soulstone-rune', 'rot.item.stasis-rune', 'rot.item.sunburst-rune',
                    'rot.item.teleportation-rune', 'rot.item.terror-rune', 'rot.item.vision-rune', 'rot.item.wanderers-stone',
                    'rot.item.ynfernael-rune'
                )
                AND ci."Quantity" > 1;

                UPDATE "CharacterItems" ci
                SET "Quantity" = 1
                FROM "ItemDefs" d
                WHERE d."Id" = ci."ItemDefId"
                  AND d."Code" IN (
                    'rot.item.arcane-bolt-rune', 'rot.item.blasting-rune', 'rot.item.ice-storm-rune', 'rot.item.immolation-rune',
                    'rot.item.lesser-rune', 'rot.item.lightning-strike-rune', 'rot.item.rune-of-collection', 'rot.item.rune-of-fate',
                    'rot.item.rune-of-misery', 'rot.item.soulstone-rune', 'rot.item.stasis-rune', 'rot.item.sunburst-rune',
                    'rot.item.teleportation-rune', 'rot.item.terror-rune', 'rot.item.vision-rune', 'rot.item.wanderers-stone',
                    'rot.item.ynfernael-rune'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "ItemDefs"
                SET "Price" = COALESCE("Price", 0),
                    "Rarity" = COALESCE("Rarity", 1);
                """);

            migrationBuilder.DropColumn(
                name: "Purchasable",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Sellable",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "ShardActivationChoice",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "ShardConfigured",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "ShardEffectAction",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "ShardEffectChoice",
                table: "CharacterItems");

            migrationBuilder.AlterColumn<int>(
                name: "Rarity",
                table: "ItemDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Price",
                table: "ItemDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
