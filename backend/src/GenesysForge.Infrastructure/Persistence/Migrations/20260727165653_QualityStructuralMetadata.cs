using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QualityStructuralMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdvantageCost",
                table: "QualityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CanActivateOnMiss",
                table: "QualityDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "EffectKind",
                table: "QualityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Repeatability",
                table: "QualityDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresHit",
                table: "QualityDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TriumphMayPay",
                table: "QualityDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdvantageCost",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "CanActivateOnMiss",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "EffectKind",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "Repeatability",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "RequiresHit",
                table: "QualityDefs");

            migrationBuilder.DropColumn(
                name: "TriumphMayPay",
                table: "QualityDefs");
        }
    }
}
