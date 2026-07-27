using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotHeroicIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HeroicCustomName",
                table: "Characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeroicOriginMode",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroicOriginNarrative",
                table: "Characters",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "HeroicOriginPrimary",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HeroicOriginRolls",
                table: "Characters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HeroicOriginSecondary",
                table: "Characters",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HeroicCustomName",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicOriginMode",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicOriginNarrative",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicOriginPrimary",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicOriginRolls",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicOriginSecondary",
                table: "Characters");
        }
    }
}
