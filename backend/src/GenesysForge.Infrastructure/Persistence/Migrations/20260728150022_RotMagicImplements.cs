using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RotMagicImplements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ноль — ImplementMaterial.Oak: дуб ничего не меняет, поэтому существующие экземпляры
            // остаются с числами каталога (ROT-MAG-MAT-01). Выбор эффектов пуст, а значит фолиант
            // и палочка старой базы считаются ненастроенными — бесплатный эффект начнёт работать
            // только после явного решения ведущего (ROT-MAG-IMP-01).
            migrationBuilder.AddColumn<string>(
                name: "ImplementChoices",
                table: "CharacterItems",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "ImplementConfigured",
                table: "CharacterItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ImplementMaterial",
                table: "CharacterItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImplementChoices",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "ImplementConfigured",
                table: "CharacterItems");

            migrationBuilder.DropColumn(
                name: "ImplementMaterial",
                table: "CharacterItems");
        }
    }
}
