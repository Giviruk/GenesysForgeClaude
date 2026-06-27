using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNpcAttacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NpcAttacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NpcId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    SkillName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Damage = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Critical = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RangeBand = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Notes = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcAttacks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcAttacks_Npcs_NpcId",
                        column: x => x.NpcId,
                        principalTable: "Npcs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NpcAttackQualities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NpcAttackId = table.Column<Guid>(type: "uuid", nullable: false),
                    QualityDefId = table.Column<Guid>(type: "uuid", nullable: true),
                    QualityCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NpcAttackQualities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NpcAttackQualities_NpcAttacks_NpcAttackId",
                        column: x => x.NpcAttackId,
                        principalTable: "NpcAttacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_NpcAttackQualities_QualityDefs_QualityDefId",
                        column: x => x.QualityDefId,
                        principalTable: "QualityDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NpcAttackQualities_NpcAttackId",
                table: "NpcAttackQualities",
                column: "NpcAttackId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcAttackQualities_QualityDefId",
                table: "NpcAttackQualities",
                column: "QualityDefId");

            migrationBuilder.CreateIndex(
                name: "IX_NpcAttacks_NpcId",
                table: "NpcAttacks",
                column: "NpcId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NpcAttackQualities");

            migrationBuilder.DropTable(
                name: "NpcAttacks");
        }
    }
}
