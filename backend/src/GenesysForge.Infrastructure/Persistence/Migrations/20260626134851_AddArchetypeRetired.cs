using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddArchetypeRetired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Retired",
                table: "ArchetypeDefs",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Retired",
                table: "ArchetypeDefs");
        }
    }
}
