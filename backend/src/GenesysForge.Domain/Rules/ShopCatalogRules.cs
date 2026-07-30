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

    // Скакуны в этом списке больше не нужны: они не записи снаряжения, а собственный тип контента
    // со статблоком (ROT-MOUNT-ITEM-01), и витрина берёт их из каталога скакунов. Здесь остаётся
    // транспортное снаряжение — то, что действительно лежит в инвентаре.
    private static readonly HashSet<string> TransportCodes =
    [
        "wagon",
        "barding",
        "saddlebags",
    ];

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
