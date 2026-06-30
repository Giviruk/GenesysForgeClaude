using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomArchetypeCareerOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "CareerDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerUserId",
                table: "ArchetypeDefs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerDefs_OwnerUserId",
                table: "CareerDefs",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArchetypeDefs_OwnerUserId",
                table: "ArchetypeDefs",
                column: "OwnerUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CareerDefs_OwnerUserId",
                table: "CareerDefs");

            migrationBuilder.DropIndex(
                name: "IX_ArchetypeDefs_OwnerUserId",
                table: "ArchetypeDefs");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "CareerDefs");

            migrationBuilder.DropColumn(
                name: "OwnerUserId",
                table: "ArchetypeDefs");
        }
    }
}
