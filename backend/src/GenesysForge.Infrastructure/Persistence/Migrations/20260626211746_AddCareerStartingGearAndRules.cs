using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerStartingGearAndRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StartingMoneyDice",
                table: "CareerDefs",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "StartingMoneyFixed",
                table: "CareerDefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CareerRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerRules_CareerDefs_CareerId",
                        column: x => x.CareerId,
                        principalTable: "CareerDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CareerStartingGears",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CareerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ItemNameFallback = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    IsChoice = table.Column<bool>(type: "boolean", nullable: false),
                    ChoiceGroup = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChoiceOption = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerStartingGears", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerStartingGears_CareerDefs_CareerId",
                        column: x => x.CareerId,
                        principalTable: "CareerDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerRules_CareerId",
                table: "CareerRules",
                column: "CareerId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerStartingGears_CareerId",
                table: "CareerStartingGears",
                column: "CareerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerRules");

            migrationBuilder.DropTable(
                name: "CareerStartingGears");

            migrationBuilder.DropColumn(
                name: "StartingMoneyDice",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "StartingMoneyFixed",
                table: "CareerDefs");
        }
    }
}
