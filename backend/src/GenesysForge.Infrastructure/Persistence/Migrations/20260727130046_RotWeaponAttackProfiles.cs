using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotWeaponAttackProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsThrown",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "WeaponAttackProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    SkillName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DamageKind = table.Column<int>(type: "integer", nullable: false),
                    DamageValue = table.Column<int>(type: "integer", nullable: false),
                    Crit = table.Column<int>(type: "integer", nullable: false),
                    Range = table.Column<int>(type: "integer", nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CannotAttackEngaged = table.Column<bool>(type: "boolean", nullable: false),
                    FixedDifficulty = table.Column<int>(type: "integer", nullable: true),
                    Qualities = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeaponAttackProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeaponAttackProfiles_ItemDefs_ItemDefId",
                        column: x => x.ItemDefId,
                        principalTable: "ItemDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeaponAttackProfiles_ItemDefId_Code",
                table: "WeaponAttackProfiles",
                columns: new[] { "ItemDefId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WeaponAttackProfiles");

            migrationBuilder.DropColumn(
                name: "IsThrown",
                table: "CharacterItems");
        }
    }
}
