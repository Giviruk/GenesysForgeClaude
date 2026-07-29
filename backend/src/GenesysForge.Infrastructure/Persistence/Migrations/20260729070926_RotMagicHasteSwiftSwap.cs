using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// ROT-MAG-04. В каталоге были перепутаны английские коды двух эффектов Усиления: механику
    /// «второй манёвр без усталости» подписывали кодом <c>Swift</c>, а «игнорирует пересечённую
    /// местность и обездвиживание» — кодом <c>Haste</c>. Схема не меняется; чинится смысл уже
    /// сохранённых ссылок.
    ///
    /// Ведущий, настраивая фолиант или палочку, выбирал эффект по названию и описанию, а
    /// сохранялся английский код. Значит, у существующих строк коды нужно поменять местами —
    /// иначе после исправления каталога инструмент молча начнёт удешевлять не тот эффект, который
    /// ведущий выбирал. Обмен одноразовый: он живёт в миграции, а не в идемпотентном сиде,
    /// который переворачивал бы значения на каждом запуске.
    /// </summary>
    public partial class RotMagicHasteSwiftSwap : Migration
    {
        /// <summary>Промежуточная метка: без неё второй replace вернул бы обратно первый.</summary>
        private const string Marker = "<<swap>>";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder) => Swap(migrationBuilder);

        /// <summary>Обмен симметричен: откат — тот же обмен.</summary>
        protected override void Down(MigrationBuilder migrationBuilder) => Swap(migrationBuilder);

        private static void Swap(MigrationBuilder migrationBuilder) => migrationBuilder.Sql($"""
            UPDATE "CharacterItems"
            SET "ImplementChoices" = replace(
                    replace(
                        replace("ImplementChoices", 'Haste', '{Marker}'),
                        'Swift', 'Haste'),
                    '{Marker}', 'Swift')
            WHERE "ImplementChoices" LIKE '%Haste%' OR "ImplementChoices" LIKE '%Swift%';
            """);
    }
}
