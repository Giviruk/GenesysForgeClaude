using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotSpeciesAbilityRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Backfill не нужен: типизированные правила и silhouette приезжают из каталога
            // идемпотентным сидом (SeedOrUpdateArchetypes), а не миграцией.
            //
            // Characters.SpeciesAbilityChoiceCode намеренно остаётся пустым у legacy-персонажей.
            // Выбрать за игрока Claws или Fleet of Paw нельзя — это необратимое решение, поэтому
            // старый Half-Catfolk получает признак SpeciesChoiceIncomplete на листе, а автоматизация
            // выбранной способности до исправления просто не применяется (ROT-SPECIES-01).
            migrationBuilder.AddColumn<string>(
                name: "SpeciesAbilityChoiceCode",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Silhouette",
                table: "ArchetypeDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RuleKind",
                table: "ArchetypeAbilityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RuleParameters",
                table: "ArchetypeAbilityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RuleValue",
                table: "ArchetypeAbilityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StoryPointCost",
                table: "ArchetypeAbilityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UseScope",
                table: "ArchetypeAbilityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsesPerScope",
                table: "ArchetypeAbilityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SpeciesAbilityChoiceCode",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "Silhouette",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "RuleKind",
                table: "ArchetypeAbilityDefs");

            migrationBuilder.DropColumn(
                name: "RuleParameters",
                table: "ArchetypeAbilityDefs");

            migrationBuilder.DropColumn(
                name: "RuleValue",
                table: "ArchetypeAbilityDefs");

            migrationBuilder.DropColumn(
                name: "StoryPointCost",
                table: "ArchetypeAbilityDefs");

            migrationBuilder.DropColumn(
                name: "UseScope",
                table: "ArchetypeAbilityDefs");

            migrationBuilder.DropColumn(
                name: "UsesPerScope",
                table: "ArchetypeAbilityDefs");
        }
    }
}
