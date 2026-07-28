using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Этап расчёта характеристик предмета (ROT-WPN-02). Порядок фиксирован и одинаков для всех
/// поверхностей: база каталога → качество изготовления → улучшения → состояние повреждения →
/// ситуативные эффекты. Этап хранится вместе с каждой поправкой, чтобы разбор оставался читаемым,
/// когда появятся следующие стадии.
/// </summary>
public enum ItemStatStage
{
    /// <summary>Числа таблицы книги.</summary>
    Base = 0,

    /// <summary>Качество изготовления экземпляра (ROT-WPN-02).</summary>
    Craftsmanship = 1,

    /// <summary>Установленные улучшения (ROT-EQP-ATT-01…03).</summary>
    Attachments = 2,

    /// <summary>Состояние повреждения предмета (GEN-EQP-DMG-01).</summary>
    DamageState = 3,

    /// <summary>Ситуативные эффекты боя и обстановки.</summary>
    Situational = 4,

    /// <summary>
    /// Материал магического инструмента (ROT-MAG-MAT-01). Номер больше остальных только потому,
    /// что этап появился позже: по порядку расчёта он идёт рядом с качеством изготовления —
    /// это тоже неизменяемое свойство экземпляра.
    /// </summary>
    Material = 5,
}

/// <summary>
/// Одна поправка к числовой характеристике предмета: что изменилось, с чего на что и от чего.
/// Клиент не пересчитывает характеристики сам — он показывает разбор.
/// </summary>
/// <param name="Field">Стабильный код характеристики: <c>encumbrance</c>, <c>soak</c>, <c>price</c>…</param>
/// <param name="Base">Значение до поправки.</param>
/// <param name="Effective">Значение после поправки, уже с учётом полов и потолков.</param>
public sealed record ItemStatAdjustment(
    string Field, int Base, int Effective, ItemStatStage Stage, string Source);

/// <summary>Помеха или снятие помехи, которые добавляет качество изготовления (Iron, Elven).</summary>
public sealed record CraftsmanshipCheckModifier(CheckModifierKind Kind, string SkillName, int Value);

/// <summary>
/// Характеристики экземпляра после применения качества изготовления. Все поля — итоговые:
/// расчёт полов и потолков живёт здесь, а не размазан по вызывающим.
/// </summary>
public sealed record EffectiveItemStats(
    WeaponCraftsmanship Craftsmanship,
    int Encumbrance,
    int SoakBonus,
    int MeleeDefense,
    int RangedDefense,
    int? HardPoints,
    int Price,
    int Rarity,
    bool Reinforced,
    IReadOnlyList<CraftsmanshipCheckModifier> CheckModifiers,
    IReadOnlyList<ItemStatAdjustment> Adjustments);

/// <summary>
/// Качество изготовления (ROT-WPN-02): один immutable тип на экземпляр оружия или брони.
/// Типы не складываются — новый заменяет предыдущий целиком, поэтому все поправки считаются
/// от чисел каталога, а не от уже изменённых.
/// </summary>
public static class CraftsmanshipRules
{
    /// <summary>Код качества «укреплённое» в справочнике: его выдаёт Ancient.</summary>
    public const string ReinforcedQualityCode = "reinforced";

    /// <summary>Пределы редкости: ниже нуля и выше десяти значений не бывает.</summary>
    public const int MinRarity = 0;
    public const int MaxRarity = 10;

    /// <summary>Урон, уменьшенный качеством изготовления, не опускается ниже единицы.</summary>
    public const int MinReducedDamage = 1;

    /// <summary>Крит не опускается ниже единицы.</summary>
    public const int MinCrit = 1;

    /// <summary>Навыки, которым железная броня добавляет помеху, пока надета.</summary>
    public static readonly string[] IronPenaltySkills = ["Athletics", "Coordination", "Riding", "Stealth"];

    private const string StealthSkill = "Stealth";

    /// <summary>Типы, которые вообще можно выбрать для предмета. Порядок — для справочника и UI.</summary>
    public static IReadOnlyList<WeaponCraftsmanship> All { get; } =
        [WeaponCraftsmanship.Steel, WeaponCraftsmanship.Iron, WeaponCraftsmanship.Dwarven,
         WeaponCraftsmanship.Elven, WeaponCraftsmanship.Ancient];

    /// <summary>Качество изготовления бывает только у оружия и брони: мешок не бывает эльфийским.</summary>
    public static bool AppliesTo(ItemKind kind) => kind is ItemKind.Weapon or ItemKind.Armor;

    /// <summary>
    /// Записи каталога, у которых тип задан самой записью: уникальные магические и именные вещи
    /// (ROT-MITEM-01). Ключ — стабильный код без префикса системы. Таблица пуста, пока таких
    /// записей в каталоге нет; выводить тип разбором названия запрещено — «Elven Blade» может
    /// оказаться названием, а не работой.
    /// </summary>
    private static readonly Dictionary<string, WeaponCraftsmanship> FixedByCode =
        new(StringComparer.Ordinal);

    /// <summary>
    /// Тип, заданный каталогом для этого кода, или <c>null</c>, если выбор свободный.
    /// Код приходит вместе с префиксом системы (<c>rot.item.axe</c>) — сравнивается хвост.
    /// </summary>
    public static WeaponCraftsmanship? FixedFor(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var bare = code[(code.LastIndexOf('.') + 1)..];
        return FixedByCode.TryGetValue(bare, out var craftsmanship) ? craftsmanship : null;
    }

    /// <summary>
    /// Проверяет, что тип вообще применим к предмету. Неизвестное значение и качество у снаряжения
    /// отклоняются машинным кодом, а не молча превращаются в обычную работу.
    /// </summary>
    public static void EnsureApplicable(ItemKind kind, WeaponCraftsmanship craftsmanship)
    {
        if (!Enum.IsDefined(craftsmanship))
            throw new DomainRuleException(
                "Неизвестное качество изготовления.", "item.craftsmanship.unknown");
        if (craftsmanship != WeaponCraftsmanship.Steel && !AppliesTo(kind))
            throw new DomainRuleException(
                "Качество изготовления бывает только у оружия и брони.",
                "item.craftsmanship.not_applicable");
    }

    /// <summary>Цена экземпляра: железо — половина с округлением вниз, эльфы и гномы вдвое, Ancient — в двадцать раз.</summary>
    public static int Price(int basePrice, WeaponCraftsmanship craftsmanship) => craftsmanship switch
    {
        WeaponCraftsmanship.Iron => (int)Math.Floor(basePrice / 2.0),
        WeaponCraftsmanship.Dwarven or WeaponCraftsmanship.Elven => basePrice * 2,
        WeaponCraftsmanship.Ancient => basePrice * 20,
        _ => basePrice,
    };

    /// <summary>
    /// Редкость экземпляра. Ancient задаёт ровно десять — это не сдвиг, а фиксированное значение;
    /// остальные сдвиги обрезаются диапазоном 0…10.
    /// </summary>
    public static int Rarity(int baseRarity, WeaponCraftsmanship craftsmanship) => craftsmanship switch
    {
        WeaponCraftsmanship.Ancient => MaxRarity,
        WeaponCraftsmanship.Iron => Math.Clamp(baseRarity - 1, MinRarity, MaxRarity),
        WeaponCraftsmanship.Dwarven => Math.Clamp(baseRarity + 2, MinRarity, MaxRarity),
        WeaponCraftsmanship.Elven => Math.Clamp(baseRarity + 3, MinRarity, MaxRarity),
        _ => Math.Clamp(baseRarity, MinRarity, MaxRarity),
    };

    /// <summary>Сдвиг урона оружия: гномья и древняя работа бьют сильнее, эльфийская — слабее.</summary>
    public static int DamageDelta(WeaponCraftsmanship craftsmanship) => craftsmanship switch
    {
        WeaponCraftsmanship.Dwarven or WeaponCraftsmanship.Ancient => 1,
        WeaponCraftsmanship.Elven => -1,
        _ => 0,
    };

    /// <summary>
    /// Итоговый урон профиля с учётом качества изготовления. Пол считается по итогу — тому числу,
    /// которое персонаж действительно наносит, а не по печатной прибавке к Мощи: «Мощь+2»
    /// эльфийской работы у сильного героя остаётся полноценным уроном.
    /// </summary>
    public static int Damage(int baseDamage, WeaponCraftsmanship craftsmanship)
    {
        var delta = DamageDelta(craftsmanship);
        var value = baseDamage + delta;
        return delta < 0 ? Math.Max(MinReducedDamage, value) : value;
    }

    /// <summary>Крит профиля: железо тупее на единицу, эльфийская и древняя работа острее, но не ниже единицы.</summary>
    public static int Crit(int baseCrit, WeaponCraftsmanship craftsmanship) => craftsmanship switch
    {
        WeaponCraftsmanship.Iron => baseCrit + 1,
        WeaponCraftsmanship.Elven or WeaponCraftsmanship.Ancient => Math.Max(MinCrit, baseCrit - 1),
        _ => baseCrit,
    };

    /// <summary>Экземпляр укреплён: броня не поддаётся Pierce/Breach, а сам предмет — Sunder.</summary>
    public static bool IsReinforced(WeaponCraftsmanship craftsmanship) =>
        craftsmanship == WeaponCraftsmanship.Ancient;

    /// <summary>
    /// Полный расчёт экземпляра. Оружие и броня меняются по-разному, поэтому вид предмета —
    /// обязательный вход: железная броня тяжелеет, а железное оружие только тупится.
    /// </summary>
    public static EffectiveItemStats For(
        ItemKind kind, WeaponCraftsmanship craftsmanship, int encumbrance, int soakBonus,
        int meleeDefense, int rangedDefense, int? hardPoints, int price, int rarity)
    {
        var adjustments = new List<ItemStatAdjustment>();
        var source = craftsmanship.ToString();

        void Track(string field, int before, int after)
        {
            if (before != after) adjustments.Add(
                new ItemStatAdjustment(field, before, after, ItemStatStage.Craftsmanship, source));
        }

        var applies = AppliesTo(kind) && craftsmanship != WeaponCraftsmanship.Steel;
        if (!applies)
            return new EffectiveItemStats(
                craftsmanship, encumbrance, soakBonus, meleeDefense, rangedDefense, hardPoints,
                price, Math.Clamp(rarity, MinRarity, MaxRarity), false, [], []);

        var isArmor = kind == ItemKind.Armor;
        var encDelta = craftsmanship switch
        {
            WeaponCraftsmanship.Iron => isArmor ? 2 : 0,
            WeaponCraftsmanship.Dwarven => 1,
            WeaponCraftsmanship.Elven => isArmor ? -2 : 0,
            _ => 0,
        };
        var effectiveEnc = Math.Max(0, encumbrance + encDelta);

        var effectiveSoak = soakBonus;
        var effectiveMelee = meleeDefense;
        var effectiveRanged = rangedDefense;
        if (isArmor && craftsmanship == WeaponCraftsmanship.Ancient)
        {
            effectiveSoak += 1;
            effectiveMelee += 1;
            effectiveRanged += 1;
        }

        // Слоты улучшений: у записи без книжного значения их нет вовсе, и выдумывать ноль нельзя.
        var hpDelta = craftsmanship switch
        {
            WeaponCraftsmanship.Dwarven => isArmor ? 1 : 0,
            WeaponCraftsmanship.Ancient => -1,
            _ => 0,
        };
        var effectiveHp = hardPoints is { } hp ? Math.Max(0, hp + hpDelta) : (int?)null;

        var effectivePrice = Price(price, craftsmanship);
        var effectiveRarity = Rarity(rarity, craftsmanship);

        Track("encumbrance", encumbrance, effectiveEnc);
        Track("soak", soakBonus, effectiveSoak);
        Track("meleeDefense", meleeDefense, effectiveMelee);
        Track("rangedDefense", rangedDefense, effectiveRanged);
        if (hardPoints is { } baseHp && effectiveHp is { } newHp) Track("hardPoints", baseHp, newHp);
        Track("price", price, effectivePrice);
        Track("rarity", Math.Clamp(rarity, MinRarity, MaxRarity), effectiveRarity);

        return new EffectiveItemStats(
            craftsmanship, effectiveEnc, effectiveSoak, effectiveMelee, effectiveRanged, effectiveHp,
            effectivePrice, effectiveRarity, IsReinforced(craftsmanship),
            CheckModifiers(kind, craftsmanship), adjustments);
    }

    /// <inheritdoc cref="For(ItemKind, WeaponCraftsmanship, int, int, int, int, int?, int, int)"/>
    public static EffectiveItemStats For(ItemDef def, WeaponCraftsmanship craftsmanship) =>
        For(def.Kind, craftsmanship, def.Encumbrance, def.SoakBonus, def.MeleeDefense,
            def.RangedDefense, def.HardPoints, def.Price, def.Rarity);

    /// <summary>
    /// Помехи от качества изготовления: железная броня мешает двигаться, эльфийская — наоборот,
    /// снимает одну помеху со Скрытности. Обе действуют только пока броня надета и активна.
    /// </summary>
    public static IReadOnlyList<CraftsmanshipCheckModifier> CheckModifiers(
        ItemKind kind, WeaponCraftsmanship craftsmanship)
    {
        if (kind != ItemKind.Armor) return [];
        return craftsmanship switch
        {
            WeaponCraftsmanship.Iron =>
                [.. IronPenaltySkills.Select(s => new CraftsmanshipCheckModifier(CheckModifierKind.AddSetback, s, 1))],
            WeaponCraftsmanship.Elven =>
                [new CraftsmanshipCheckModifier(CheckModifierKind.RemoveSetback, StealthSkill, 1)],
            _ => [],
        };
    }
}
