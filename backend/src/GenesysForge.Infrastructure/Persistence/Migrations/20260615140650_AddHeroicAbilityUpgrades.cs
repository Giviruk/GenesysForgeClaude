using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHeroicAbilityUpgrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Activation",
                table: "HeroicAbilityDefs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ActivationCost",
                table: "HeroicAbilityDefs",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Duration",
                table: "HeroicAbilityDefs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Frequency",
                table: "HeroicAbilityDefs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "HeroicAbilityDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Requirement",
                table: "HeroicAbilityDefs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "HeroicUpgradeRank",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "HeroicAbilityUpgradeDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HeroicAbilityDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroicAbilityUpgradeDefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HeroicAbilityUpgradeDefs_HeroicAbilityDefs_HeroicAbilityDef~",
                        column: x => x.HeroicAbilityDefId,
                        principalTable: "HeroicAbilityDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HeroicAbilityUpgradeDefs_HeroicAbilityDefId",
                table: "HeroicAbilityUpgradeDefs",
                column: "HeroicAbilityDefId");

            // Старый набор героических способностей заменяется каталогом heroics.catalog.json (теперь с улучшениями).
            // Удаляем встроенные записи (OwnerUserId IS NULL), чтобы идемпотентный сид добавил новые;
            // кастомные способности пользователей не трогаем. У персонажей выбор обнулится (FK ON DELETE SET NULL).
            migrationBuilder.Sql("DELETE FROM \"HeroicAbilityDefs\" WHERE \"OwnerUserId\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HeroicAbilityUpgradeDefs");

            migrationBuilder.DropColumn(
                name: "Activation",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "ActivationCost",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Duration",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "Requirement",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "HeroicUpgradeRank",
                table: "Characters");
        }
    }
}
