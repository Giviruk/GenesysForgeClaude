using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotMountItem01Mounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MountDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameRu = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Brawn = table.Column<int>(type: "integer", nullable: false),
                    Agility = table.Column<int>(type: "integer", nullable: false),
                    Intellect = table.Column<int>(type: "integer", nullable: false),
                    Cunning = table.Column<int>(type: "integer", nullable: false),
                    Willpower = table.Column<int>(type: "integer", nullable: false),
                    Presence = table.Column<int>(type: "integer", nullable: false),
                    Soak = table.Column<int>(type: "integer", nullable: false),
                    WoundThreshold = table.Column<int>(type: "integer", nullable: false),
                    StrainThreshold = table.Column<int>(type: "integer", nullable: true),
                    MeleeDefense = table.Column<int>(type: "integer", nullable: false),
                    RangedDefense = table.Column<int>(type: "integer", nullable: false),
                    Silhouette = table.Column<int>(type: "integer", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: true),
                    Rarity = table.Column<int>(type: "integer", nullable: false),
                    IncludedGear = table.Column<List<string>>(type: "text[]", nullable: false),
                    RequiresRidingCheck = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SafeDescription = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: true),
                    Retired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountDefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterMounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    MountDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: false),
                    WoundsCurrent = table.Column<int>(type: "integer", nullable: false),
                    CarriedLoad = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterMounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterMounts_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterMounts_MountDefs_MountDefId",
                        column: x => x.MountDefId,
                        principalTable: "MountDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MountAbility",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MountDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountAbility", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MountAbility_MountDefs_MountDefId",
                        column: x => x.MountDefId,
                        principalTable: "MountDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MountAttack",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MountDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    SkillName = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Damage = table.Column<int>(type: "integer", nullable: false),
                    Critical = table.Column<int>(type: "integer", nullable: false),
                    Range = table.Column<int>(type: "integer", nullable: false),
                    QualityCodes = table.Column<List<string>>(type: "text[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountAttack", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MountAttack_MountDefs_MountDefId",
                        column: x => x.MountDefId,
                        principalTable: "MountDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MountSkill",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MountDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Ranks = table.Column<int>(type: "integer", nullable: false),
                    IsGroupSkill = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MountSkill", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MountSkill_MountDefs_MountDefId",
                        column: x => x.MountDefId,
                        principalTable: "MountDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMounts_CharacterId",
                table: "CharacterMounts",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CharacterMounts_MountDefId",
                table: "CharacterMounts",
                column: "MountDefId");

            migrationBuilder.CreateIndex(
                name: "IX_MountAbility_MountDefId",
                table: "MountAbility",
                column: "MountDefId");

            migrationBuilder.CreateIndex(
                name: "IX_MountAttack_MountDefId",
                table: "MountAttack",
                column: "MountDefId");

            migrationBuilder.CreateIndex(
                name: "IX_MountDefs_System_Code",
                table: "MountDefs",
                columns: new[] { "System", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_MountSkill_MountDefId",
                table: "MountSkill",
                column: "MountDefId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterMounts");

            migrationBuilder.DropTable(
                name: "MountAbility");

            migrationBuilder.DropTable(
                name: "MountAttack");

            migrationBuilder.DropTable(
                name: "MountSkill");

            migrationBuilder.DropTable(
                name: "MountDefs");
        }
    }
}
