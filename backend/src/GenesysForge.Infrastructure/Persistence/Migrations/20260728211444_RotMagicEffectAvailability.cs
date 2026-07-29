using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotMagicEffectAvailability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedSkills",
                table: "SpellDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "DifficultyIncrease",
                table: "SpellDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Exclusions",
                table: "SpellDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsOptional",
                table: "SpellDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Resolution",
                table: "SpellDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedSkills",
                table: "SpellDefs");

            migrationBuilder.DropColumn(
                name: "DifficultyIncrease",
                table: "SpellDefs");

            migrationBuilder.DropColumn(
                name: "Exclusions",
                table: "SpellDefs");

            migrationBuilder.DropColumn(
                name: "IsOptional",
                table: "SpellDefs");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "SpellDefs");
        }
    }
}
