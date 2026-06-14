using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTalentSetting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue = 1 (GenesysSetting.Any): существующие таланты остаются видимыми в обеих
            // системах. Талант-таблицу нельзя пересоздавать (на неё ссылаются CharacterTalents с каскадом),
            // поэтому корректный сеттинг для старых строк проставится только на свежем сидировании.
            migrationBuilder.AddColumn<int>(
                name: "Setting",
                table: "TalentDefs",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Setting",
                table: "TalentDefs");
        }
    }
}
