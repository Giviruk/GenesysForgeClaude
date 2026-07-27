using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotItemCraftsmanship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Значение по умолчанию — 2 (WeaponCraftsmanship.Steel), а не ноль: нулём в этом enum
            // записана гномья работа (ROT-HA-02 положил её первой), и legacy-строки нельзя молча
            // сделать гномьими. Все уже существующие предметы — обычной работы (ROT-WPN-02).
            migrationBuilder.AddColumn<int>(
                name: "Craftsmanship",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Craftsmanship",
                table: "CharacterItems");
        }
    }
}
