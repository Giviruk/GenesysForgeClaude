using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnglishContentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "TalentDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "SpellDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "SkillDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BodyEn",
                table: "RuleTableEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GroupEn",
                table: "RuleTableEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NotesEn",
                table: "RuleTableEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "QualityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "ItemDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "HeroicAbilityUpgradeDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "HeroicAbilityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "CareerRules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "CareerDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "ArchetypeDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "ArchetypeAbilityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "SpellDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "BodyEn",
                table: "RuleTableEntries");

            migrationBuilder.DropColumn(
                name: "GroupEn",
                table: "RuleTableEntries");

            migrationBuilder.DropColumn(
                name: "NotesEn",
                table: "RuleTableEntries");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "HeroicAbilityUpgradeDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "CareerRules");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "ArchetypeAbilityDefs");
        }
    }
}
