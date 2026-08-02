namespace GenesysForge.Domain.Rules;

/// <summary>
/// Полный профиль именного оружия (ROT-HA-02). Числа задаются таблицей книги и строятся сервером
/// из выбранного профиля: клиент присылает только enum, любые присланные им характеристики
/// игнорируются как подделка.
/// </summary>
/// <param name="Profile">Выбранный профиль.</param>
/// <param name="SkillName">Английское имя боевого навыка для броска.</param>
/// <param name="DamageKind">Как считается урон: прибавка к Мощи или готовое число.</param>
/// <param name="DamageValue">Прибавка к Мощи или сам урон — в зависимости от вида.</param>
/// <param name="Crit">Критическое значение.</param>
/// <param name="RangeBand">Дистанция.</param>
/// <param name="Encumbrance">Вес.</param>
/// <param name="HardPoints">Число слотов улучшений (HP).</param>
/// <param name="Qualities">Качества профиля: код качества и рейтинг (0 — без рейтинга).</param>
/// <param name="GroupTrait">Признак формы, который профиль задаёт автоматически.</param>
public sealed record SignatureWeaponProfileSpec(
    SignatureWeaponProfile Profile,
    string SkillName,
    DamageKind DamageKind,
    int DamageValue,
    int Crit,
    string RangeBand,
    int Encumbrance,
    int HardPoints,
    IReadOnlyList<(string Code, int Rating)> Qualities,
    WeaponFormTraits GroupTrait)
{
    /// <summary>Урон подписью: «Brawn + N» для ближнего боя, абсолютное число для дальнего.</summary>
    public string Damage => DamageText(DamageKind, DamageValue);

    /// <summary>Подпись урона по типу и значению — одна на все поверхности, включая поправки работы.</summary>
    public static string DamageText(DamageKind kind, int value) =>
        kind == Domain.DamageKind.BrawnPlus ? $"Brawn + {value}" : value.ToString();
}

/// <summary>Таблица Signature Weapons: четыре профиля с точными характеристиками.</summary>
public static class SignatureWeaponProfiles
{
    private static readonly Dictionary<SignatureWeaponProfile, SignatureWeaponProfileSpec> Table = new()
    {
        [SignatureWeaponProfile.Brawl] = new(
            SignatureWeaponProfile.Brawl, "Brawl", DamageKind.BrawnPlus, 2, 4, "Engaged", 1, 2,
            [("disorient", 3), ("superior", 0)], WeaponFormTraits.Brawl),
        [SignatureWeaponProfile.OneHanded] = new(
            SignatureWeaponProfile.OneHanded, "Melee (Light)", DamageKind.BrawnPlus, 3, 3, "Engaged", 1, 2,
            [("superior", 0)], WeaponFormTraits.OneHanded),
        [SignatureWeaponProfile.TwoHanded] = new(
            SignatureWeaponProfile.TwoHanded, "Melee (Heavy)", DamageKind.BrawnPlus, 5, 3, "Engaged", 3, 2,
            [("knockdown", 0), ("superior", 0)], WeaponFormTraits.TwoHanded),
        [SignatureWeaponProfile.Ranged] = new(
            SignatureWeaponProfile.Ranged, "Ranged", DamageKind.Fixed, 8, 3, "Long", 2, 2,
            [("superior", 0)], WeaponFormTraits.Ranged),
    };

    /// <summary>Все четыре профиля в порядке перечисления — для справочника и UI.</summary>
    public static IReadOnlyList<SignatureWeaponProfileSpec> All { get; } =
        [.. Table.Values.OrderBy(x => (int)x.Profile)];

    /// <summary>Признаки, которые задаёт сам профиль; их GM не подтверждает и не может изменить.</summary>
    public static WeaponFormTraits GroupTraits =>
        WeaponFormTraits.Brawl | WeaponFormTraits.OneHanded | WeaponFormTraits.TwoHanded | WeaponFormTraits.Ranged;

    /// <summary>
    /// Качества именного оружия с учётом качества изготовления: древняя работа добавляет
    /// Укреплённое. Один расчёт на лист и на проверку базового улучшения — иначе они разъедутся.
    /// </summary>
    public static IReadOnlyList<(string Code, int Rating)> QualitiesFor(
        SignatureWeaponProfile profile, WeaponCraftsmanship craftsmanship)
    {
        var spec = Get(profile);
        var stats = CraftsmanshipRules.For(
            ItemKind.Weapon, craftsmanship, spec.Encumbrance, 0, 0, 0, spec.HardPoints, 0, 0);
        return stats.Reinforced
            ? [.. spec.Qualities, (CraftsmanshipRules.ReinforcedQualityCode, 0)]
            : spec.Qualities;
    }

    public static SignatureWeaponProfileSpec Get(SignatureWeaponProfile profile) =>
        Table.TryGetValue(profile, out var spec)
            ? spec
            : throw new DomainRuleException("Неизвестный профиль именного оружия.", "heroic.weapon.profile_unknown");
}
