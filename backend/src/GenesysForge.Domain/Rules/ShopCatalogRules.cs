using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Стабильная классификация общей витрины. Правила опираются на code и структурные поля, а не на
/// локализованное имя, поэтому переключение языка не меняет категорию товара.
/// </summary>
public static class ShopCatalogRules
{
    private static readonly HashSet<string> ServiceCodes =
    [
        "ale-flagon",
        "lodging-common-room-1-night",
        "lodging-private-room-1-night",
        "meal-tavern",
        "porter-per-day",
        "torchbearer-per-day",
        "travel-riverboat-1-day",
        "travel-wagon-1-day",
        "wine-bottle",
    ];

    // Ни скакуны, ни повозка в этом списке больше не нужны: это не записи снаряжения, а собственный
    // тип контента со статблоком (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01), и витрина берёт их из
    // каталога транспорта. Здесь остаётся транспортное снаряжение — то, что действительно лежит в
    // инвентаре и устанавливается на транспорт.
    // Эти же коды — единственное снаряжение, которое ставится на конкретный транспорт, а не
    // складывается в него грузом (ROT-TRANSPORT-01). Список кодов, а не поле каталога: записей
    // всего две, и обе книжные.
    private static readonly HashSet<string> TransportCodes =
    [
        "barding",
        "saddlebags",
    ];

    /// <summary>Позицию можно установить на транспорт: попона и седельные сумки.</summary>
    public static bool IsMountGear(string? code) => TransportCodes.Contains(BareCode(code));

    /// <summary>
    /// Попона — единственное установленное снаряжение с ограничением по профилю (ROT-MOUNT-NPC-01):
    /// по умолчанию она рассчитана на боевого скакуна.
    /// </summary>
    public static bool IsBarding(string? code) =>
        string.Equals(BareCode(code), "barding", StringComparison.Ordinal);

    private static readonly HashSet<string> ConsumableCodes =
    [
        "trail-rations-1-day",
        "torches-3",
        "herbs-of-healing",
        "protective-tonic",
        "invisibility-potion",
        "power-potion",
        "speed-potion",
        "bottled-courage",
        "smokebomb-vial",
        "acid-flask",
        "stamina-elixir",
        "health-elixir",
        "immunity-elixir",
        "regeneration-elixir",
        "poison",
    ];

    public static bool IsService(string? code) => ServiceCodes.Contains(BareCode(code));

    public static ShopItemCategory Category(ItemDef item)
    {
        var code = BareCode(item.Code);
        if (ServiceCodes.Contains(code)) return ShopItemCategory.Service;
        if (TransportCodes.Contains(code)) return ShopItemCategory.Transport;
        if (ConsumableCodes.Contains(code)) return ShopItemCategory.Consumable;
        if (ImplementRules.IsImplement(item.Code) || RuneboundShardRules.IsShard(item.Code))
            return ShopItemCategory.MagicImplement;
        // Реликвия проверяется до вида: среди семнадцати есть и оружие, и снаряжение, но лежат
        // они в своей полке, а не вперемешку с покупным (ROT-MITEM-01).
        if (MagicItemRules.IsMagicItem(item.Code)) return ShopItemCategory.MagicItem;
        if (item.Kind == ItemKind.Armor) return ShopItemCategory.Armor;
        if (item.Kind != ItemKind.Weapon) return ShopItemCategory.Gear;

        var skill = item.AttackProfiles.FirstOrDefault(p => p.IsDefault)?.SkillName ?? item.SkillName;
        if (skill.Contains("Ranged", StringComparison.OrdinalIgnoreCase)
            || skill.Contains("Gunnery", StringComparison.OrdinalIgnoreCase))
            return ShopItemCategory.WeaponRanged;
        if (skill.Contains("Heavy", StringComparison.OrdinalIgnoreCase))
            return ShopItemCategory.WeaponHeavy;
        return ShopItemCategory.WeaponLight;
    }

    private static string BareCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "";
        var index = code.LastIndexOf('.');
        return index < 0 ? code : code[(index + 1)..];
    }
}
