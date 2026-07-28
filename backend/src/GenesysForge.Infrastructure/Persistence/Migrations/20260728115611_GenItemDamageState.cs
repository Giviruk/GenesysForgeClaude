using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GenItemDamageState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ноль — ItemDamageState.Undamaged: все существующие экземпляры целы (GEN-EQP-DMG-01).
            // Улучшение получает собственное состояние: сломанное не работает, но слот носителя
            // продолжает занимать.
            migrationBuilder.AddColumn<int>(
                name: "DamageState",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DamageState",
                table: "CharacterAttachments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DamageState",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "DamageState",
                table: "CharacterAttachments");
        }
    }
}
