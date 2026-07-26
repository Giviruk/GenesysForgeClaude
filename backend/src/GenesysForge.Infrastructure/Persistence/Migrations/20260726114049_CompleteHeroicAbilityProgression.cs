using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CompleteHeroicAbilityProgression : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "HeroicDurationRanks",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "HeroicFrequencyRanks",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HeroicStoryUpgrade",
                table: "Characters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // До этой миграции система ошибочно выдавала одно стартовое ability point.
            // Убираем Power-ранги, которые не покрываются XP сверх стартового XP вида.
            migrationBuilder.Sql(
                """
                UPDATE "Characters" AS c
                SET "HeroicUpgradeRank" = CASE
                    WHEN GREATEST(c."TotalXp" - a."StartingXp", 0) / 50 >= 3
                        THEN c."HeroicUpgradeRank"
                    WHEN GREATEST(c."TotalXp" - a."StartingXp", 0) / 50 >= 1
                        THEN LEAST(c."HeroicUpgradeRank", 1)
                    ELSE 0
                END
                FROM "ArchetypeDefs" AS a
                WHERE c."ArchetypeId" = a."Id";
                """);

            migrationBuilder.CreateTable(
                name: "HeroicSecondaryEffectDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NameRu = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SafeDescription = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    DescriptionEn = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HeroicSecondaryEffectDefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CharacterHeroicSecondaryEffects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    HeroicSecondaryEffectDefId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterHeroicSecondaryEffects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterHeroicSecondaryEffects_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CharacterHeroicSecondaryEffects_HeroicSecondaryEffectDefs_H~",
                        column: x => x.HeroicSecondaryEffectDefId,
                        principalTable: "HeroicSecondaryEffectDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHeroicSecondaryEffects_CharacterId_HeroicSecondary~",
                table: "CharacterHeroicSecondaryEffects",
                columns: new[] { "CharacterId", "HeroicSecondaryEffectDefId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CharacterHeroicSecondaryEffects_HeroicSecondaryEffectDefId",
                table: "CharacterHeroicSecondaryEffects",
                column: "HeroicSecondaryEffectDefId");

            migrationBuilder.CreateIndex(
                name: "IX_HeroicSecondaryEffectDefs_Code",
                table: "HeroicSecondaryEffectDefs",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterHeroicSecondaryEffects");

            migrationBuilder.DropTable(
                name: "HeroicSecondaryEffectDefs");

            migrationBuilder.DropColumn(
                name: "HeroicDurationRanks",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicFrequencyRanks",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "HeroicStoryUpgrade",
                table: "Characters");
        }
    }
}
