using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotStartingEquipmentModes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill намеренно отсутствует.
            //
            // Characters: у существующих персонажей деньги уже выданы по старому правилу
            // (Money = формула карьеры). Начислять им бюджет 500 задним числом значило бы
            // выдумать средства, а пересчитывать Money — отобрать уже потраченное. Поэтому
            // StartingPurchaseBudget = 0 и StartingEquipmentMode = StandardMoney (значения по
            // умолчанию): новое правило действует только для новых персонажей.
            //
            // CharacterItems.Provenance: отличить исторически выданный комплект от купленного
            // предмета нельзя (ROT-CRE-04 прямо запрещает переписывать старый инвентарь), поэтому
            // все существующие строки остаются Purchased. Как следствие, автоматическая раскладка
            // Adventuring Pack на Traveling Gear их не затрагивает — она требует доказанного
            // provenance CareerPackage.
            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "TalentDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "SkillDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "QualityDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "ItemDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "HeroicSecondaryEffectDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "HeroicAbilityDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "StartingEquipmentMode",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StartingPurchaseBudget",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Provenance",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "CareerDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Retired",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "HeroicSecondaryEffectDefs");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "StartingEquipmentMode",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "StartingPurchaseBudget",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Provenance",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "Retired",
                table: "CareerDefs");
        }
    }
}
