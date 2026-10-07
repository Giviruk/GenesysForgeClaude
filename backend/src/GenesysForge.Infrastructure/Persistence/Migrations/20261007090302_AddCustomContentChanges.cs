using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomContentChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomContentChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HomebrewPackId = table.Column<Guid>(type: "uuid", nullable: true),
                    DefinitionType = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefinitionName = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ChangesJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomContentChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomContentChanges_DefinitionId_CreatedAt",
                table: "CustomContentChanges",
                columns: new[] { "DefinitionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CustomContentChanges_HomebrewPackId_CreatedAt",
                table: "CustomContentChanges",
                columns: new[] { "HomebrewPackId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomContentChanges");
        }
    }
}
