using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGameParticipantRangePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "RangeAngle",
                table: "GameParticipants",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RangeZone",
                table: "GameParticipants",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RangeAngle",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "RangeZone",
                table: "GameParticipants");
        }
    }
}
