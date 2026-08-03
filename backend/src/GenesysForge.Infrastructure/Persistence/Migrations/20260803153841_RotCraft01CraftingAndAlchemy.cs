using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotCraft01CraftingAndAlchemy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CraftNote",
                table: "CharacterItems",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CraftedEncumbrance",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CraftedFragile",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CraftedHardPoints",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CraftedQualities",
                table: "CharacterItems",
                type: "character varying(400)",
                maxLength: 400,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CraftingProjectId",
                table: "CharacterItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CraftingProjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ItemDefId = table.Column<Guid>(type: "uuid", nullable: false),
                    BaseCharacterItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    TargetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TargetPrice = table.Column<int>(type: "integer", nullable: true),
                    TargetRarity = table.Column<int>(type: "integer", nullable: true),
                    SkillName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    BaseDifficulty = table.Column<int>(type: "integer", nullable: false),
                    DifficultyReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Time = table.Column<int>(type: "integer", nullable: false),
                    BaseTime = table.Column<int>(type: "integer", nullable: false),
                    TimeReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ListedCost = table.Column<int>(type: "integer", nullable: false),
                    CostPercent = table.Column<int>(type: "integer", nullable: false),
                    CostOverride = table.Column<int>(type: "integer", nullable: true),
                    CostOverrideReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    Requirements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Intent = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RoughSurvival = table.Column<bool>(type: "boolean", nullable: false),
                    NetSuccesses = table.Column<int>(type: "integer", nullable: false),
                    Advantages = table.Column<int>(type: "integer", nullable: false),
                    Threats = table.Column<int>(type: "integer", nullable: false),
                    Triumphs = table.Column<int>(type: "integer", nullable: false),
                    Despairs = table.Column<int>(type: "integer", nullable: false),
                    CreatedCharacterItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    Outcome = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingProjects", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraftingProjects_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftingProjects_ItemDefs_ItemDefId",
                        column: x => x.ItemDefId,
                        principalTable: "ItemDefs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CraftingSpendDefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RowCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Table = table.Column<int>(type: "integer", nullable: false),
                    NameRu = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SafeDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DescriptionEn = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Retired = table.Column<bool>(type: "boolean", nullable: false),
                    AdvantageCost = table.Column<int>(type: "integer", nullable: false),
                    ThreatCost = table.Column<int>(type: "integer", nullable: false),
                    TriumphCost = table.Column<int>(type: "integer", nullable: false),
                    DespairCost = table.Column<int>(type: "integer", nullable: false),
                    IsNegative = table.Column<bool>(type: "boolean", nullable: false),
                    Repeatable = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresGmConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresParameter = table.Column<bool>(type: "boolean", nullable: false),
                    Effect = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<int>(type: "integer", nullable: false),
                    Quality = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    WeaponOnly = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingSpendDefs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CraftingProjectSpends",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CraftingProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SpendCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    Parameter = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    PaidWith = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TextRu = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    TextEn = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftingProjectSpends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraftingProjectSpends_CraftingProjects_CraftingProjectId",
                        column: x => x.CraftingProjectId,
                        principalTable: "CraftingProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CraftingProjects_CharacterId",
                table: "CraftingProjects",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingProjects_CharacterId_Status",
                table: "CraftingProjects",
                columns: new[] { "CharacterId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CraftingProjects_ItemDefId",
                table: "CraftingProjects",
                column: "ItemDefId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingProjectSpends_CraftingProjectId",
                table: "CraftingProjectSpends",
                column: "CraftingProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftingSpendDefs_Code",
                table: "CraftingSpendDefs",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CraftingSpendDefs_Table_SortOrder",
                table: "CraftingSpendDefs",
                columns: new[] { "Table", "SortOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CraftingProjectSpends");

            migrationBuilder.DropTable(
                name: "CraftingSpendDefs");

            migrationBuilder.DropTable(
                name: "CraftingProjects");

            migrationBuilder.DropColumn(
                name: "CraftNote",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "CraftedEncumbrance",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "CraftedFragile",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "CraftedHardPoints",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "CraftedQualities",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "CraftingProjectId",
                table: "CharacterItems");
        }
    }
}
