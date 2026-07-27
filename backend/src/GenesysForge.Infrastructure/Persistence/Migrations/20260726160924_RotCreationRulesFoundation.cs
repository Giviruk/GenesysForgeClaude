using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotCreationRulesFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "CareerSkillNames",
                table: "TalentDefs",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<int>(
                name: "CreationStrainThreshold",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreationWoundThreshold",
                table: "Characters",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RulesReviewRequired",
                table: "Characters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ThresholdSnapshotProvenance",
                table: "Characters",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "GrantsCareerSkill",
                table: "ArchetypeStartingSkills",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // ROT-CRE-02 backfill. Для уже завершённых персонажей характеристика на момент
            // completion восстанавливается точно: после создания её меняет только Dedication,
            // а его выдачи хранятся пофамильно в CharacterTalents.GrantedCharacteristics.
            // Поэтому brawnAtCompletion = текущая Brawn − число выдач Dedication в Brawn.
            // Это шаг 2 стратегии (LegacyAuditReconstructed): угадывание и запись нуля запрещены.
            // Персонажи в фазе создания порогов не получают — они считаются динамически.
            migrationBuilder.Sql("""
                UPDATE "Characters" c
                SET "CreationWoundThreshold" = GREATEST(1, a."WoundBase" + c."Brawn" - (
                        SELECT count(*)
                        FROM "CharacterTalents" ct
                        CROSS JOIN LATERAL unnest(string_to_array(ct."GrantedCharacteristics", ',')) AS g
                        WHERE ct."CharacterId" = c."Id" AND btrim(g) = 'Brawn')),
                    "CreationStrainThreshold" = GREATEST(1, a."StrainBase" + c."Willpower" - (
                        SELECT count(*)
                        FROM "CharacterTalents" ct
                        CROSS JOIN LATERAL unnest(string_to_array(ct."GrantedCharacteristics", ',')) AS g
                        WHERE ct."CharacterId" = c."Id" AND btrim(g) = 'Willpower')),
                    "ThresholdSnapshotProvenance" = 2
                FROM "ArchetypeDefs" a
                WHERE a."Id" = c."ArchetypeId"
                  AND c."IsCreationPhase" = false
                  AND c."CreationWoundThreshold" IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareerSkillNames",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "CreationStrainThreshold",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "CreationWoundThreshold",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "RulesReviewRequired",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "ThresholdSnapshotProvenance",
                table: "Characters");

            migrationBuilder.DropColumn(
                name: "GrantsCareerSkill",
                table: "ArchetypeStartingSkills");
        }
    }
}
