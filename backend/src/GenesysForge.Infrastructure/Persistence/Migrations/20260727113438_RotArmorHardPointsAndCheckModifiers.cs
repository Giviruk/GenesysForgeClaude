using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotArmorHardPointsAndCheckModifiers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HardPoints",
                table: "ItemDefs",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ItemCheckModifiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Characteristic = table.Column<int>(type: "integer", nullable: true),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    RequiresWorn = table.Column<bool>(type: "boolean", nullable: false),
                    Condition = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemCheckModifiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemCheckModifiers_ItemDefs_ItemDefId",
                        column: x => x.ItemDefId,
                        principalTable: "ItemDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItemCheckModifiers_ItemDefId",
                table: "ItemCheckModifiers",
                column: "ItemDefId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItemCheckModifiers");

            migrationBuilder.DropColumn(
                name: "HardPoints",
                table: "ItemDefs");
        }
    }
}
