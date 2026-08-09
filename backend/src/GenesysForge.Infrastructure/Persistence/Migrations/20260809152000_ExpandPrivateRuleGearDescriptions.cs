using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenesysForge.Infrastructure.Persistence.Migrations;

/// <summary>
/// Доставляет расширенные описания обычного снаряжения в уже существующую PrivateFull БД.
/// Обновляются только записи с непустым Description: в PublicSafe полное описание очищено,
/// поэтому публичная база остаётся на SafeDescription.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260809152000_ExpandPrivateRuleGearDescriptions")]
public partial class ExpandPrivateRuleGearDescriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ItemDefs"
            SET "Description" = CASE "Code"
                WHEN 'rot.item.alchemists-kit' THEN 'Переносной набор алхимика содержит основные инструменты для работы в дороге: ступку и пестик, флаконы, мерные ёмкости и тару для реагентов. При проверках Alchemy набор считается подходящим инструментом для задачи, поэтому персонаж не получает штраф за отсутствие нужного оснащения. Отдельные сложные зелья и эликсиры могут требовать специальных ингредиентов или полноценной лаборатории — это определяет ведущий.'
                WHEN 'rot.item.alchemists-lab-supplies' THEN 'Полноценная алхимическая лаборатория включает всё содержимое переносного набора и дополнительное оборудование — перегонные аппараты, тигли, стеклянную посуду и другие специализированные инструменты. При работе в лаборатории персонаж добавляет 1 синюю кость поддержки к проверкам Alchemy. Лаборатория очень тяжёлая и обычно занимает отдельное помещение; перевозить её можно, если целиком выделить под оборудование повозку с тягловым животным.'
                WHEN 'rot.item.apothecarys-kit' THEN 'Аптекарский набор содержит бинты, мази, припарки и другие средства для лечения ран и болезней. Наличие набора позволяет выполнять проверки Medicine для лечения ран и критических травм без штрафа за отсутствие подходящих медицинских инструментов.'
                WHEN 'rot.item.backpack' THEN 'Прочный дорожный рюкзак распределяет груз и позволяет нести значительно больше припасов и добычи. Пока персонаж носит рюкзак, его порог нагрузки увеличивается на 4.'
                WHEN 'rot.item.climbing-gear' THEN 'Набор для лазания включает верёвки, крючья и небольшой молоток, превращая опасный подъём по отвесной поверхности в более управляемую задачу. При использовании этого снаряжения персонаж убирает 1 чёрную кость помехи из проверок Athletics, совершаемых для лазания.'
                WHEN 'rot.item.extra-quiver' THEN 'Запасной колчан хранит дополнительные стрелы, болты или другие обычные боеприпасы для дальнобойного оружия. Если оружие получило результат «боеприпасы закончились», персонаж может потратить манёвр, чтобы пополнить его из запасного колчана. Это не восстанавливает оружие с качеством Limited Ammo, поскольку такие боеприпасы отслеживаются отдельно.'
                WHEN 'rot.item.fine-cloak' THEN 'Дорогой плащ из качественной ткани или меха подчёркивает статус владельца и помогает производить нужное впечатление. Пока персонаж носит такой плащ, он убирает 1 чёрную кость помехи из проверок Charm, Deception и Leadership. В надетом состоянии нагрузка плаща считается равной 0.'
                WHEN 'rot.item.flint-and-steel' THEN 'Огниво позволяет без магии развести обычный костёр или зажечь подходящее топливо. Если у персонажа есть время, терпение и сухой трут или растопка, он может высечь искру и разжечь огонь.'
                WHEN 'rot.item.lantern' THEN 'Железный фонарь защищает пламя металлическим каркасом и стеклом, поэтому его удобнее и безопаснее переносить, чем открытый факел. Зажжённый фонарь даёт освещение примерно до короткой дистанции и убирает 1 чёрную кость помехи, добавленную к проверке именно из-за темноты.'
                WHEN 'rot.item.rope' THEN 'Обычная прочная верёвка подходит для подъёма, страховки, связывания и множества других задач. Стандартный отрезок имеет длину примерно до средней дистанции; более длинные варианты могут приобретаться с разрешения ведущего.'
                WHEN 'rot.item.thieves-tools' THEN 'Набор отмычек и тонких инструментов позволяет пытаться открыть механические замки и защёлки без подходящего ключа, включая сложные механизмы. При проверке Skulduggery для вскрытия замка или защёлки персонаж добавляет к итоговому результату 1 Преимущество.'
                WHEN 'rot.item.waterskin-empty' THEN 'Бурдюк предназначен для переноски воды, вина и других жидкостей в дороге. Полного бурдюка достаточно, чтобы обеспечить питьём двух человек на один день. Пока бурдюк заполнен, его нагрузка увеличивается до 2.'
                WHEN 'rot.item.winter-clothing' THEN 'Тёплая одежда из плотной шерсти и меха защищает от сильного холода. Пока персонаж её носит, он убирает 2 чёрные кости помехи из проверок Survival или Resilience, если эти кости были добавлены из-за холодной погоды. В надетом состоянии нагрузка зимней одежды считается равной 1.'
                ELSE "Description"
            END
            WHERE COALESCE("Description", '') <> ''
              AND "Code" IN (
                'rot.item.alchemists-kit',
                'rot.item.alchemists-lab-supplies',
                'rot.item.apothecarys-kit',
                'rot.item.backpack',
                'rot.item.climbing-gear',
                'rot.item.extra-quiver',
                'rot.item.fine-cloak',
                'rot.item.flint-and-steel',
                'rot.item.lantern',
                'rot.item.rope',
                'rot.item.thieves-tools',
                'rot.item.waterskin-empty',
                'rot.item.winter-clothing'
              );
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE "ItemDefs"
            SET "Description" = "SafeDescription"
            WHERE COALESCE("Description", '') <> ''
              AND "Code" IN (
                'rot.item.alchemists-kit',
                'rot.item.alchemists-lab-supplies',
                'rot.item.apothecarys-kit',
                'rot.item.backpack',
                'rot.item.climbing-gear',
                'rot.item.extra-quiver',
                'rot.item.fine-cloak',
                'rot.item.flint-and-steel',
                'rot.item.lantern',
                'rot.item.rope',
                'rot.item.thieves-tools',
                'rot.item.waterskin-empty',
                'rot.item.winter-clothing'
              );
            """);
    }
}
