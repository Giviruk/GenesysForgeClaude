using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryShopFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Crit",
                table: "ItemDefs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Damage",
                table: "ItemDefs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Properties",
                table: "ItemDefs",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RangeBand",
                table: "ItemDefs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SkillName",
                table: "ItemDefs",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Money",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 500);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Crit",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Damage",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Properties",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "RangeBand",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "SkillName",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "Money",
                table: "Characters");
        }
    }
}
