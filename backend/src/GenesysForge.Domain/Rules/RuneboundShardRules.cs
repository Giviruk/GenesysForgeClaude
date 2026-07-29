namespace GenesysForge.Domain.Rules;

/// <summary>Как runebound shard добавляет дополнительный эффект заклинанию.</summary>
public enum ShardSpellEffectMode
{
    /// <summary>Эффект добавляется автоматически и удалить его из сборки нельзя.</summary>
    MandatoryFree = 0,

    /// <summary>Игрок может добавить эффект без повышения сложности.</summary>
    OptionalFree = 1,
}

/// <summary>
/// Бесплатный или обязательный эффект shard. Action и effect — стабильные английские коды
/// справочника магии, а не отображаемые локализованные строки.
/// </summary>
/// <param name="FreeUses">Сколько первых добавлений эффекта бесплатно.</param>
/// <param name="OverridesSkillRestriction">
/// Shard явно разрешает эффект для Runes вопреки обычной матрице: Holy у Sunburst и Doom у Fate.
/// </param>
public sealed record ShardSpellEffect(
    string Action,
    string EffectCode,
    ShardSpellEffectMode Mode,
    int FreeUses = 1,
    bool OverridesSkillRestriction = false);

/// <summary>Плоское снижение итоговой сложности для указанного действия; пустой action — любого.</summary>
public sealed record ShardDifficultyReduction(string Action, int Amount);

/// <summary>Цена самостоятельной активации shard. Исполнение остаётся за encounter runtime.</summary>
public enum ShardActivationCost
{
    Maneuver = 0,
    Action = 1,
    Passive = 2,
}

/// <summary>
/// Профиль временного оружия, создаваемого самостоятельной активацией shard. Он хранится
/// структурно и показывается игроку, но эта ветка не ведёт ход и не активирует оружие сама.
/// </summary>
public sealed record ShardActivationAttack(
    string Skill,
    int Damage,
    int Critical,
    string Range,
    IReadOnlyList<ShardActivationQuality> Qualities);

public sealed record ShardActivationQuality(string Code, int? Rating = null);

/// <summary>Точный механический паспорт одного runebound shard (ROT-MAG-11).</summary>
public sealed record RuneboundShardSpec(
    string Code,
    int AttackDamageBonus,
    int CastingStrainReduction,
    IReadOnlyList<ShardDifficultyReduction> DifficultyReductions,
    IReadOnlyList<ShardSpellEffect> SpellEffects,
    ShardActivationCost ActivationCost,
    string ActivationFrequency,
    ShardActivationAttack? ActivationAttack = null,
    bool NeedsConfiguration = false);

/// <summary>
/// Runebound shards Realms of Terrinoth. Это отдельный вид magic implement: для Runes нужен ровно
/// один shard, а обычные посохи, жезлы и фолианты его не заменяют.
/// </summary>
public static class RuneboundShardRules
{
    public const string RequiredMagicSkill = "Runes";
    public const int MinimumSkillRank = 1;

    private const string Attack = "Attack";
    private const string Augment = "Augment";
    private const string Curse = "Curse";

    private static readonly Dictionary<string, RuneboundShardSpec> ByCode =
        new(StringComparer.Ordinal)
        {
            ["arcane-bolt-rune"] = new(
                "arcane-bolt-rune", 4, 0, [],
                [
                    Optional(Attack, "Range"),
                    Mandatory(Attack, "Impact"),
                ],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Ranged", 8, 3, "Medium", Q("auto-fire"))),

            ["blasting-rune"] = new(
                "blasting-rune", 5, 0, [],
                [
                    Mandatory(Attack, "Blast"),
                    Optional(Attack, "Impact"),
                ],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Discipline", 9, 3, "Medium", Q("blast", 7), Q("knockdown"))),

            ["ice-storm-rune"] = new(
                "ice-storm-rune", 4, 0, [],
                [
                    Mandatory(Attack, "Ice"),
                    Mandatory(Attack, "Blast"),
                ],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Discipline", 7, 2, "Medium", Q("blast", 4), Q("ensnare", 3))),

            ["immolation-rune"] = new(
                "immolation-rune", 0, 0, [],
                [
                    Mandatory(Attack, "Fire"),
                    Mandatory(Attack, "Deadly"),
                ],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Discipline", 8, 3, "Short", Q("burn", 2))),

            ["lesser-rune"] = new(
                "lesser-rune", 3, 0, [], [],
                ShardActivationCost.Maneuver, "manual", NeedsConfiguration: true),

            ["lightning-strike-rune"] = new(
                "lightning-strike-rune", 5, 0, [],
                [
                    Optional(Attack, "Range"),
                    Mandatory(Attack, "Lightning"),
                ],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Discipline", 8, 3, "Long", Q("auto-fire"), Q("disorient", 3))),

            ["rune-of-collection"] = new(
                "rune-of-collection", 0, 1, [new("", 1)], [],
                ShardActivationCost.Maneuver, "manual"),

            ["rune-of-fate"] = new(
                "rune-of-fate", 0, 0, [],
                [
                    Optional(Augment, "Additional Target"),
                    Optional(Curse, "Additional Target"),
                    Mandatory(Curse, "Doom", overrides: true),
                ],
                ShardActivationCost.Action, "session"),

            ["rune-of-misery"] = new(
                "rune-of-misery", 0, 0, [new(Curse, 2)], [],
                ShardActivationCost.Action, "rounds"),

            ["soulstone-rune"] = new(
                "soulstone-rune", 0, 0, [], [],
                ShardActivationCost.Maneuver, "rounds"),

            ["stasis-rune"] = new(
                "stasis-rune", 0, 0, [],
                [Mandatory(Curse, "Paralyzed")],
                ShardActivationCost.Action, "turn"),

            ["sunburst-rune"] = new(
                "sunburst-rune", 0, 0, [],
                [Mandatory(Attack, "Holy/Unholy", overrides: true)],
                ShardActivationCost.Maneuver, "turn",
                Weapon("Ranged", 4, 1, "Medium", Q("breach", 1))),

            ["teleportation-rune"] = new(
                "teleportation-rune", 0, 0, [],
                [Optional("", "Range", freeUses: 3)],
                ShardActivationCost.Action, "instant"),

            ["terror-rune"] = new(
                "terror-rune", 0, 0, [], [],
                ShardActivationCost.Passive, "while-carried"),

            ["vision-rune"] = new(
                "vision-rune", 0, 0, [], [],
                ShardActivationCost.Action, "instant"),

            ["wanderers-stone"] = new(
                "wanderers-stone", 0, 0, [],
                [
                    Optional(Augment, "Haste"),
                    Optional(Augment, "Swift"),
                ],
                ShardActivationCost.Action, "encounter"),

            ["ynfernael-rune"] = new(
                "ynfernael-rune", 3, 0, [],
                [
                    Mandatory(Attack, "Empowered"),
                    Mandatory(Attack, "Deadly"),
                ],
                ShardActivationCost.Action, "instant"),
        };

    /// <summary>Manifest в каноническом порядке ТЗ; seed-тест сравнивает всё множество codes.</summary>
    public static IReadOnlyList<RuneboundShardSpec> All { get; } =
    [
        ByCode["arcane-bolt-rune"],
        ByCode["blasting-rune"],
        ByCode["ice-storm-rune"],
        ByCode["immolation-rune"],
        ByCode["lesser-rune"],
        ByCode["lightning-strike-rune"],
        ByCode["rune-of-collection"],
        ByCode["rune-of-fate"],
        ByCode["rune-of-misery"],
        ByCode["soulstone-rune"],
        ByCode["stasis-rune"],
        ByCode["sunburst-rune"],
        ByCode["teleportation-rune"],
        ByCode["terror-rune"],
        ByCode["vision-rune"],
        ByCode["wanderers-stone"],
        ByCode["ynfernael-rune"],
    ];

    public static bool IsShard(string? code) => For(code) is not null;

    public static RuneboundShardSpec? For(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var bare = code[(code.LastIndexOf('.') + 1)..];
        return ByCode.GetValueOrDefault(bare);
    }

    public static bool CanUseAsImplement(bool runesIsCareerSkill, int runesRanks) =>
        runesIsCareerSkill && runesRanks >= MinimumSkillRank;

    public static IReadOnlyList<ShardSpellEffect> EffectsFor(
        RuneboundShardSpec spec, string action) =>
        [.. spec.SpellEffects.Where(e =>
            string.IsNullOrEmpty(e.Action)
            || string.Equals(e.Action, action, StringComparison.Ordinal))];

    public static int FlatDifficultyReduction(RuneboundShardSpec spec, string action) =>
        spec.DifficultyReductions
            .Where(r => string.IsNullOrEmpty(r.Action)
                || string.Equals(r.Action, action, StringComparison.Ordinal))
            .Sum(r => r.Amount);

    private static ShardSpellEffect Mandatory(
        string action, string effect, bool overrides = false) =>
        new(action, effect, ShardSpellEffectMode.MandatoryFree, 1, overrides);

    private static ShardSpellEffect Optional(
        string action, string effect, int freeUses = 1, bool overrides = false) =>
        new(action, effect, ShardSpellEffectMode.OptionalFree, freeUses, overrides);

    private static ShardActivationAttack Weapon(
        string skill, int damage, int critical, string range, params ShardActivationQuality[] qualities) =>
        new(skill, damage, critical, range, qualities);

    private static ShardActivationQuality Q(string code, int? rating = null) => new(code, rating);
}
