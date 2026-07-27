using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotTalentPolicyAndChoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "ExcludesTalentCodes",
                table: "TalentDefs",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiresTalentCode",
                table: "TalentDefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "NeedsChoice",
                table: "CharacterTalents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CharacterTalentChoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterTalentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RankIndex = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CharacterTalentChoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CharacterTalentChoices_CharacterTalents_CharacterTalentId",
                        column: x => x.CharacterTalentId,
                        principalTable: "CharacterTalents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CharacterTalentChoices_CharacterTalentId",
                table: "CharacterTalentChoices",
                column: "CharacterTalentId");

            // ROT-TAL-03 backfill. Существующие выборы Dedication лежат в legacy-CSV
            // CharacterTalents.GrantedCharacteristics; переносим их в общий формат по порядку
            // покупки, сохраняя legacy-поле как временный алиас. Значение уже стабильное
            // (имя CharacteristicType), поэтому угадывать ничего не нужно.
            migrationBuilder.Sql("""
                INSERT INTO "CharacterTalentChoices" ("Id", "CharacterTalentId", "RankIndex", "Kind", "Value", "DisplayName")
                SELECT gen_random_uuid(), ct."Id", g.ord - 1, 1, btrim(g.value), btrim(g.value)
                FROM "CharacterTalents" ct
                CROSS JOIN LATERAL unnest(string_to_array(ct."GrantedCharacteristics", ',')) WITH ORDINALITY AS g(value, ord)
                WHERE coalesce(ct."GrantedCharacteristics", '') <> ''
                  AND btrim(g.value) <> '';
                """);

            // Таланты, требующие выбора, но не имеющие его ни в новом, ни в legacy-формате,
            // помечаются NeedsChoice. Выбирать за игрока нельзя, повторно платить XP — тоже.
            migrationBuilder.Sql("""
                UPDATE "CharacterTalents" ct
                SET "NeedsChoice" = true
                FROM "TalentDefs" td
                WHERE td."Id" = ct."TalentDefId"
                  AND td."Code" ~ '\.talent\.(povyshenie|kvalifikatsiya|schastlivoe-popadanie|heroic-recovery|geroicheskaya-volya|odarennost|master|signature-spell|zhivotnoe-sputnik)$'
                  AND NOT EXISTS (SELECT 1 FROM "CharacterTalentChoices" c WHERE c."CharacterTalentId" = ct."Id");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CharacterTalentChoices");

            migrationBuilder.DropColumn(
                name: "ExcludesTalentCodes",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "RequiresTalentCode",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "NeedsChoice",
                table: "CharacterTalents");
        }
    }
}
