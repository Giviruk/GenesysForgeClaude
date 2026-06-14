using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "TalentDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "TalentDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "TalentDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "TalentDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "SkillDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "SkillDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "SkillDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "SkillDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SkillDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ItemDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "ItemDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "ItemDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ItemDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "HeroicAbilityDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "HeroicAbilityDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "HeroicAbilityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "HeroicAbilityDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "CareerDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "CareerDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "CareerDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "CareerDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "ArchetypeDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameRu",
                table: "ArchetypeDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SafeDescription",
                table: "ArchetypeDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "ArchetypeDefs",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Code",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "NameRu",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "SafeDescription",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "ArchetypeDefs");
        }
    }
}
