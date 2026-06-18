using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContentPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContentPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    System = table.Column<int>(type: "integer", nullable: false),
                    IsPublicToCampaign = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContentPackEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AllowedState = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    SafeSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PageRef = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    GmNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PlayerNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContentPackEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContentPackEntries_ContentPacks_ContentPackId",
                        column: x => x.ContentPackId,
                        principalTable: "ContentPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContentPackEntries_ContentPackId",
                table: "ContentPackEntries",
                column: "ContentPackId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPacks_CampaignId",
                table: "ContentPacks",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_ContentPacks_OwnerUserId",
                table: "ContentPacks",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContentPackEntries");

            migrationBuilder.DropTable(
                name: "ContentPacks");
        }
    }
}
