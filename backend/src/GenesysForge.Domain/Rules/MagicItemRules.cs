namespace GenesysForge.Domain.Rules;

/// <summary>
/// Каталожные магические предметы Realms of Terrinoth (ROT-MITEM-01): уникальные реликвии, а не
/// товар витрины.
///
/// <para>
/// У них нет обычной цены — книга её не задаёт. Ноль монет в этом месте не «дёшево», а «значения
/// нет»: с нулём реликвия десятой редкости покупалась в лавке бесплатно. Поэтому цена <c>null</c>,
/// покупка и продажа закрыты, а в руки игрока предмет попадает выдачей ведущего. Обычное
/// изготовление такие записи тоже не создаёт — редкое зачарование это отдельный процесс
/// (ROT-CRAFT-MAGIC-01), а не способ заказать копию именной реликвии.
/// </para>
///
/// <para>Редкость при этом остаётся: она у реликвий в таблице есть и нужна ведущему.</para>
/// </summary>
public static class MagicItemRules
{
    /// <summary>Семнадцать записей таблицы. Список закрытый: новая реликвия — правка каталога.</summary>
    private static readonly HashSet<string> Codes = new(StringComparer.Ordinal)
    {
        "bloodscript-ring",
        "cloak-of-mists",
        "dead-mans-compass",
        "deepwood-longbow",
        "elven-boots",
        "gauntlets-of-power",
        "horn-of-courage",
        "mace-of-kellos",
        "prismatic-staff",
        "serpent-dagger",
        "shadow-bracers",
        "shield-of-light",
        "soulbound-sword",
        "staff-of-light",
        "truelight-lantern",
        "warding-talisman",
        "winged-boots",
    };

    /// <summary>Сколько реликвий в таблице — по этому числу сторожит сид-тест.</summary>
    public static int Count => Codes.Count;

    /// <summary>Все коды таблицы в стабильном порядке.</summary>
    public static IReadOnlyList<string> AllCodes { get; } = [.. Codes.OrderBy(x => x, StringComparer.Ordinal)];

    /// <summary>
    /// Запись каталога — магический предмет. Код приходит и с префиксом системы
    /// (<c>rot.item.winged-boots</c>), и без него.
    /// </summary>
    public static bool IsMagicItem(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var index = code.LastIndexOf('.');
        return Codes.Contains(index < 0 ? code : code[(index + 1)..]);
    }
}
