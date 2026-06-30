using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddHomebrewPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "TalentDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "SkillDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "ItemDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "HeroicAbilityDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "CareerDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "HomebrewPackId",
                table: "ArchetypeDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HomebrewPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    ShareTokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsShared = table.Column<bool>(type: "boolean", nullable: false),
                    IsEnabledByDefault = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomebrewPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HomebrewPackCampaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomebrewPackCampaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomebrewPackCampaigns_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomebrewPackCampaigns_HomebrewPacks_HomebrewPackId",
                        column: x => x.HomebrewPackId,
                        principalTable: "HomebrewPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HomebrewPackCharacters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HomebrewPackCharacters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HomebrewPackCharacters_Characters_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Characters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HomebrewPackCharacters_HomebrewPacks_HomebrewPackId",
                        column: x => x.HomebrewPackId,
                        principalTable: "HomebrewPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TalentDefs_HomebrewPackId",
                table: "TalentDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillDefs_HomebrewPackId",
                table: "SkillDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemDefs_HomebrewPackId",
                table: "ItemDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_HeroicAbilityDefs_HomebrewPackId",
                table: "HeroicAbilityDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerDefs_HomebrewPackId",
                table: "CareerDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeDefs_HomebrewPackId",
                table: "ArchetypeDefs",
                column: "HomebrewPackId");

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackCampaigns_CampaignId",
                table: "HomebrewPackCampaigns",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackCampaigns_HomebrewPackId_CampaignId",
                table: "HomebrewPackCampaigns",
                columns: new[] { "HomebrewPackId", "CampaignId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackCharacters_CharacterId",
                table: "HomebrewPackCharacters",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPackCharacters_HomebrewPackId_CharacterId",
                table: "HomebrewPackCharacters",
                columns: new[] { "HomebrewPackId", "CharacterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPacks_OwnerUserId",
                table: "HomebrewPacks",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_HomebrewPacks_ShareTokenHash",
                table: "HomebrewPacks",
                column: "ShareTokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HomebrewPackCampaigns");

            migrationBuilder.DropTable(
                name: "HomebrewPackCharacters");

            migrationBuilder.DropTable(
                name: "HomebrewPacks");

            migrationBuilder.DropIndex(
                name: "IX_TalentDefs_HomebrewPackId",
                table: "TalentDefs");

            migrationBuilder.DropIndex(
                name: "IX_SkillDefs_HomebrewPackId",
                table: "SkillDefs");

            migrationBuilder.DropIndex(
                name: "IX_ItemDefs_HomebrewPackId",
                table: "ItemDefs");

            migrationBuilder.DropIndex(
                name: "IX_HeroicAbilityDefs_HomebrewPackId",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropIndex(
                name: "IX_CareerDefs_HomebrewPackId",
                table: "CareerDefs");

            migrationBuilder.DropIndex(
                name: "IX_ArchetypeDefs_HomebrewPackId",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "TalentDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "SkillDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "ItemDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "HeroicAbilityDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "HomebrewPackId",
                table: "ArchetypeDefs");
        }
    }
}
