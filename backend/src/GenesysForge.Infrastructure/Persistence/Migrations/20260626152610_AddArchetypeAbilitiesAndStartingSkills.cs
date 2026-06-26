using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArchetypeAbilitiesAndStartingSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ArchetypeAbilityDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArchetypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SafeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    AutomationKind = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeAbilityDefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeAbilityDefs_ArchetypeDefs_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "ArchetypeDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArchetypeStartingSkills",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArchetypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FreeRanks = table.Column<int>(type: "integer", nullable: false),
                    IsChoice = table.Column<bool>(type: "boolean", nullable: false),
                    ChoiceGroup = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ChoiceCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchetypeStartingSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArchetypeStartingSkills_ArchetypeDefs_ArchetypeId",
                        column: x => x.ArchetypeId,
                        principalTable: "ArchetypeDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeAbilityDefs_ArchetypeId",
                table: "ArchetypeAbilityDefs",
                column: "ArchetypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeStartingSkills_ArchetypeId",
                table: "ArchetypeStartingSkills",
                column: "ArchetypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ArchetypeAbilityDefs");

            migrationBuilder.DropTable(
                name: "ArchetypeStartingSkills");
        }
    }
}
