using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotTalentLifecycleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StoryPointCost",
                table: "TalentDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StrainCost",
                table: "TalentDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Trigger",
                table: "TalentDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "UseScope",
                table: "TalentDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsesPerScope",
                table: "TalentDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoryPointCost",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "StrainCost",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "Trigger",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "UseScope",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "UsesPerScope",
                table: "TalentDefs");
        }
    }
}
