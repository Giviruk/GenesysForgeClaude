using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCampaignChronicle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CampaignChronicleChapters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignChronicleChapters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignChronicleChapters_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignChronicleChapters_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CampaignChronicleChapters_Users_UpdatedByUserId",
                        column: x => x.UpdatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CampaignChronicleRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ChapterId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    EditedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignChronicleRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignChronicleRevisions_CampaignChronicleChapters_Chapte~",
                        column: x => x.ChapterId,
                        principalTable: "CampaignChronicleChapters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignChronicleRevisions_Users_EditedByUserId",
                        column: x => x.EditedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignChronicleChapters_CampaignId_SortOrder",
                table: "CampaignChronicleChapters",
                columns: new[] { "CampaignId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CampaignChronicleChapters_CreatedByUserId",
                table: "CampaignChronicleChapters",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignChronicleChapters_UpdatedByUserId",
                table: "CampaignChronicleChapters",
                column: "UpdatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignChronicleRevisions_ChapterId_Version",
                table: "CampaignChronicleRevisions",
                columns: new[] { "ChapterId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CampaignChronicleRevisions_EditedByUserId",
                table: "CampaignChronicleRevisions",
                column: "EditedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CampaignChronicleRevisions");

            migrationBuilder.DropTable(
                name: "CampaignChronicleChapters");
        }
    }
}
