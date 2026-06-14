using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpellParentEffect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentEffect",
                table: "SpellDefs",
                type: "character varying(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            // Справочник магии был перестроен (доступность эффектов по навыкам + привязка
            // доп. эффектов к базовому). Чистим старые встроенные записи, чтобы идемпотентный
            // сид при старте пересоздал их в новой структуре. Кастомный контент не трогаем.
            migrationBuilder.Sql("DELETE FROM \"SpellDefs\" WHERE \"OwnerUserId\" IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParentEffect",
                table: "SpellDefs");
        }
    }
}
