namespace GenesysForge.Domain.Rules;

/// <summary>
/// Структурная доступность магических действий и дополнительных эффектов (ROT-MAG-01).
///
/// Матрица «действие × направление» — правило, а не оформление справочника: по ней сид собирает
/// записи, сборщик решает, что можно выбрать, а справочник объясняет, чего направление не умеет.
/// Поэтому таблица живёт здесь одна, а не разъезжается по сиду и двум клиентам.
///
/// Действия Expanded Player's Guide помечены отдельно: они не входят в RoT-матрицу и не меняют
/// доступность остальных восьми.
/// </summary>
public static class MagicMatrix
{
    public const string Arcana = "Arcana";
    public const string Divine = "Divine";
    public const string Primal = "Primal";
    public const string Runes = "Runes";
    public const string Verse = "Verse";

    /// <summary>Пять направлений Realms of Terrinoth в порядке таблицы.</summary>
    public static IReadOnlyList<string> AllSkills { get; } = [Arcana, Divine, Primal, Runes, Verse];

    /// <summary>Направления Genesys Core: Рун и Песни в базовой книге нет.</summary>
    public static IReadOnlyList<string> CoreSkills { get; } = [Arcana, Divine, Primal];

    /// <summary>Базовая матрица RoT: действие → направления, которым оно доступно.</summary>
    private static readonly Dictionary<string, string[]> ActionSkills = new(StringComparer.Ordinal)
    {
        ["Attack"] = [Arcana, Divine, Primal, Runes],
        ["Augment"] = [Divine, Primal, Runes, Verse],
        ["Barrier"] = [Arcana, Divine, Runes],
        ["Conjure"] = [Arcana, Primal],
        ["Curse"] = [Arcana, Divine, Runes, Verse],
        ["Dispel"] = [Arcana, Verse],
        ["Heal"] = [Divine, Primal, Verse],
        ["Utility"] = [Arcana, Divine, Primal, Runes, Verse],
        // Expanded Player's Guide: в RoT-матрицу не входят, но доступность у них тоже структурная.
        ["Mask"] = [Arcana],
        ["Predict"] = [Arcana, Divine],
        ["Transform"] = [Primal],
    };

    /// <summary>Действия Expanded Player's Guide: контент опциональный и помечается в интерфейсе.</summary>
    private static readonly HashSet<string> OptionalActions =
        new(["Mask", "Predict", "Transform"], StringComparer.Ordinal);

    /// <summary>
    /// Девять дополнительных эффектов, которые книга оставляет одному направлению. Таблица явная:
    /// «Только Вера» из описания разбирать текстом нельзя, а на этом же признаке держится скидка
    /// священного символа (ROT-MAG-IMP-01).
    /// </summary>
    private static readonly Dictionary<(string Action, string Effect), string> RestrictedEffects = new()
    {
        [("Attack", "Manipulative")] = Arcana,
        [("Attack", "Non-Lethal")] = Primal,
        [("Attack", "Holy/Unholy")] = Divine,
        [("Barrier", "Reflective")] = Arcana,
        [("Barrier", "Sanctuary")] = Divine,
        [("Augment", "Divine Health")] = Divine,
        [("Augment", "Primal Fury")] = Primal,
        [("Curse", "Despair")] = Divine,
        [("Curse", "Doom")] = Arcana,
    };

    /// <summary>
    /// Эффекты, которые разрешено добавлять к одному заклинанию несколько раз (ROT-MAG-02). Ключ —
    /// пара «действие + эффект», а не одно имя: одноимённый эффект другого действия повторяемость
    /// по наследству не получает.
    /// </summary>
    private static readonly HashSet<(string Action, string Effect)> RepeatableEffects =
    [
        ("Attack", "Range"), ("Augment", "Range"), ("Barrier", "Range"), ("Conjure", "Range"),
        ("Curse", "Range"), ("Dispel", "Range"), ("Heal", "Range"),
        // EPG: увеличения размера и силуэта отмечены книгой отдельно.
        ("Mask", "Range"), ("Mask", "Size"), ("Transform", "Silhouette Increase"),
    ];

    /// <summary>
    /// Эффекты, несочетаемые внутри одного заклинания: ключ — пара «действие + эффект», значение —
    /// коды, вместе с которыми его выбрать нельзя. Проклятья «Отчаяние» и «Паралич» бьют по одной
    /// цели и с «Дополнительной целью» не складываются. Ограничение — поле записи, а не оговорка
    /// в конце описания, и запрет симметричен: с какой стороны его ни выбирай, вторая недоступна.
    /// </summary>
    private static readonly Dictionary<(string Action, string Effect), string[]> Exclusions = new()
    {
        [("Curse", "Despair")] = ["Additional Target"],
        [("Curse", "Paralyzed")] = ["Additional Target"],
        [("Curse", "Additional Target")] = ["Despair", "Paralyzed"],
    };

    /// <summary>
    /// Как применяется запись справочника: ключ — пара «действие + эффект», у самого действия
    /// эффект пустой. Всё, чего нет в таблице, применяется при успехе проверки — это и есть
    /// обычный случай, а перечислены отклонения от него: свойства с активацией, траты преимуществ
    /// после броска, очко сюжета и параметры заклинания, которые до броска меняют дальность,
    /// число целей или размер.
    /// </summary>
    private static readonly Dictionary<(string Action, string Effect), SpellResolutionKind> Resolutions = new()
    {
        [("Utility", "")] = SpellResolutionKind.Narrative,
        [("Predict", "")] = SpellResolutionKind.Narrative,

        // Attack
        [("Attack", "Close Combat")] = SpellResolutionKind.Parameter,
        [("Attack", "Range")] = SpellResolutionKind.Parameter,
        [("Attack", "Blast")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Ice")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Lightning")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Fire")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Deadly")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Impact")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Destructive")] = SpellResolutionKind.ActivatedQuality,
        [("Attack", "Non-Lethal")] = SpellResolutionKind.PassiveQuality,
        [("Attack", "Manipulative")] = SpellResolutionKind.AdvantageSpend,

        // Barrier, Heal, Conjure, Curse, Dispel, Augment — параметры заклинания
        [("Barrier", "Range")] = SpellResolutionKind.Parameter,
        [("Barrier", "Additional Target")] = SpellResolutionKind.Parameter,
        [("Heal", "Range")] = SpellResolutionKind.Parameter,
        [("Heal", "Additional Target")] = SpellResolutionKind.Parameter,
        [("Heal", "Revive Incapacitated")] = SpellResolutionKind.Parameter,
        [("Conjure", "Range")] = SpellResolutionKind.Parameter,
        [("Conjure", "Additional Summon")] = SpellResolutionKind.Parameter,
        [("Conjure", "Medium Summon")] = SpellResolutionKind.Parameter,
        [("Conjure", "Grand Summon")] = SpellResolutionKind.Parameter,
        [("Curse", "Range")] = SpellResolutionKind.Parameter,
        [("Curse", "Additional Target")] = SpellResolutionKind.Parameter,
        [("Dispel", "Range")] = SpellResolutionKind.Parameter,
        [("Dispel", "Additional Target")] = SpellResolutionKind.Parameter,
        [("Augment", "Range")] = SpellResolutionKind.Parameter,
        [("Augment", "Additional Target")] = SpellResolutionKind.Parameter,

        // EPG
        [("Mask", "Range")] = SpellResolutionKind.Parameter,
        [("Mask", "Size")] = SpellResolutionKind.Parameter,
        [("Mask", "Additional Illusion")] = SpellResolutionKind.Parameter,
        [("Predict", "Additional Questions")] = SpellResolutionKind.Parameter,
        [("Predict", "Empowered")] = SpellResolutionKind.Parameter,
        [("Predict", "Scry")] = SpellResolutionKind.Narrative,
        [("Predict", "Cheat Death")] = SpellResolutionKind.StoryPoint,
        [("Transform", "Silhouette Increase")] = SpellResolutionKind.Parameter,
        [("Transform", "Curse of the Wild")] = SpellResolutionKind.Parameter,
    };

    /// <summary>Восемь RoT-действий в порядке таблицы.</summary>
    public static IReadOnlyList<string> NativeActions { get; } =
        [.. ActionSkills.Keys.Where(a => !OptionalActions.Contains(a))];

    /// <summary>Все действия, включая опциональные (EPG).</summary>
    public static IReadOnlyList<string> AllActions { get; } = [.. ActionSkills.Keys];

    /// <summary>Действие взято из Expanded Player's Guide и в RoT-матрицу не входит.</summary>
    public static bool IsOptionalAction(string action) => OptionalActions.Contains(action);

    /// <summary>
    /// Направления, которым доступно действие. Неизвестное действие — пустой список, а не
    /// исключение: справочник пополняется людьми, и падать всем сидом из-за одной записи нельзя.
    /// </summary>
    public static IReadOnlyList<string> SkillsForAction(string action) =>
        ActionSkills.TryGetValue(action, out var skills) ? skills : [];

    /// <summary>
    /// Направления, которым доступен дополнительный эффект: доступность родительского действия,
    /// суженная явным ограничением эффекта.
    /// </summary>
    public static IReadOnlyList<string> SkillsForEffect(string action, string effect)
    {
        var parent = SkillsForAction(action);
        return RestrictedEffects.TryGetValue((action, effect), out var only)
            ? [.. parent.Where(s => string.Equals(s, only, StringComparison.OrdinalIgnoreCase))]
            : parent;
    }

    /// <summary>Направление, которому эффект доступен исключительно; пусто — доступен нескольким.</summary>
    public static string RestrictedSkill(string action, string effect) =>
        RestrictedEffects.TryGetValue((action, effect), out var only) ? only : "";

    /// <summary>Действие доступно направлению.</summary>
    public static bool IsActionAvailable(string skill, string action) =>
        SkillsForAction(action).Contains(skill, StringComparer.OrdinalIgnoreCase);

    /// <summary>Дополнительный эффект доступен направлению вместе со своим действием.</summary>
    public static bool IsEffectAvailable(string skill, string action, string effect) =>
        SkillsForEffect(action, effect).Contains(skill, StringComparer.OrdinalIgnoreCase);

    /// <summary>Эффект можно добавить к одному заклинанию несколько раз.</summary>
    public static bool IsRepeatable(string action, string effect) =>
        RepeatableEffects.Contains((action, effect));

    /// <summary>Коды эффектов, вместе с которыми этот выбрать нельзя.</summary>
    public static IReadOnlyList<string> ConflictsFor(string action, string effect) =>
        Exclusions.TryGetValue((action, effect), out var codes) ? codes : [];

    /// <summary>
    /// Как применяется запись: у действия <paramref name="effect"/> пустой. Неописанный случай —
    /// применение при успехе проверки.
    /// </summary>
    public static SpellResolutionKind ResolutionFor(string action, string effect = "") =>
        Resolutions.TryGetValue((action, effect), out var kind) ? kind : SpellResolutionKind.OnSuccess;
}
