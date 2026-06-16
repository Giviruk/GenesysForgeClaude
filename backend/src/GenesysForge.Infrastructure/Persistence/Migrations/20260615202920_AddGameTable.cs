using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GameSessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    PlayerStoryPoints = table.Column<int>(type: "integer", nullable: false),
                    GmStoryPoints = table.Column<int>(type: "integer", nullable: false),
                    CurrentRound = table.Column<int>(type: "integer", nullable: false),
                    CurrentTurnIndex = table.Column<int>(type: "integer", nullable: false),
                    PublicNotes = table.Column<string>(type: "text", nullable: false),
                    GmNotes = table.Column<string>(type: "text", nullable: false),
                    AllowPlayerEdits = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GameParticipants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CharacterId = table.Column<Guid>(type: "uuid", nullable: true),
                    NpcId = table.Column<Guid>(type: "uuid", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParticipantType = table.Column<int>(type: "integer", nullable: false),
                    InitiativeSlotType = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    WoundsCurrent = table.Column<int>(type: "integer", nullable: false),
                    WoundsThreshold = table.Column<int>(type: "integer", nullable: false),
                    StrainCurrent = table.Column<int>(type: "integer", nullable: false),
                    StrainThreshold = table.Column<int>(type: "integer", nullable: true),
                    Soak = table.Column<int>(type: "integer", nullable: false),
                    MeleeDefense = table.Column<int>(type: "integer", nullable: false),
                    RangedDefense = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsDefeated = table.Column<bool>(type: "boolean", nullable: false),
                    IsHiddenFromPlayers = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameParticipants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameParticipants_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InitiativeSlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SlotType = table.Column<int>(type: "integer", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    AssignedParticipantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InitiativeSlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InitiativeSlots_GameSessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "GameSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GameParticipants_SessionId",
                table: "GameParticipants",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_GameSessions_CampaignId",
                table: "GameSessions",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_InitiativeSlots_SessionId",
                table: "InitiativeSlots",
                column: "SessionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameParticipants");

            migrationBuilder.DropTable(
                name: "InitiativeSlots");

            migrationBuilder.DropTable(
                name: "GameSessions");
        }
    }
}
