using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotHeroicParameters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CharacterHeroicConfigurations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParagonSkillDefId = table.Column<Guid>(type: "uuid", nullable: true),
                    ParagonSkillName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SixthSenseSubject = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHeroicConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHeroicConfigurations_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterHeroicConfigurations_SkillDefs_ParagonSkillDefId",
                        column: x => x.ParagonSkillDefId,
                        principalTable: "SkillDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CharacterSignatureWeapons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Profile = table.Column<int>(type: "integer", nullable: false),
                    Craftsmanship = table.Column<int>(type: "integer", nullable: false),
                    NarrativeForm = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    FormTraits = table.Column<int>(type: "integer", nullable: false),
                    IsLost = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterSignatureWeapons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterSignatureWeapons_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHeroicConfigurations_CharacterId",
                table: "CharacterHeroicConfigurations",
                column: "CharacterId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHeroicConfigurations_ParagonSkillDefId",
                table: "CharacterHeroicConfigurations",
                column: "ParagonSkillDefId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterSignatureWeapons_CharacterId",
                table: "CharacterSignatureWeapons",
                column: "CharacterId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterHeroicConfigurations");

            migrationBuilder.DropTable(
                name: "CharacterSignatureWeapons");
        }
    }
}
