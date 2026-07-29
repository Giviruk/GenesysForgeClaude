namespace GenesysForge.Domain.Rules;

/// <summary>
/// Один допустимый источник рейтинга: навык и его ранги у персонажа (ROT-MAG-10).
/// </summary>
/// <param name="Skill">Английское имя навыка — стабильный код.</param>
/// <param name="Ranks">Ранги персонажа: ноль — это ноль, а не «хотя бы один».</param>
/// <param name="Reason">
/// Почему источник доступен: базовое правило системы или talent-исключение. Клиенту он нужен,
/// чтобы объяснить выбор, а не показывать два навыка без причины.
/// </param>
public sealed record KnowledgeRatingOption(string Skill, int Ranks, KnowledgeRatingReason Reason);

/// <summary>Основание, по которому навык годится как источник рейтинга.</summary>
public enum KnowledgeRatingReason
{
    /// <summary>Навык Знания, названный правилами системы: в RoT — Предания, в Core — Знание.</summary>
    Default = 0,

    /// <summary>Талант «Тёмное прозрение» разрешает считать по Запретному.</summary>
    DarkInsight = 1,
}

/// <summary>
/// Откуда берётся числовой рейтинг эффектов заклинания (ROT-MAG-10).
///
/// В RoT «ранги Знания» в описании эффекта означают ровно <c>Knowledge (Lore)</c>: ни общий
/// <c>Knowledge</c>, ни наибольший из знаниевых навыков не подходят — иначе персонаж с высокой
/// Географией внезапно жёг бы сильнее. Талант «Тёмное прозрение» — единственное исключение: он
/// разрешает считать те же рейтинги по <c>Knowledge (Forbidden)</c>. Выбор между ними делает
/// игрок при сборке заклинания, поэтому правило возвращает список источников, а не одно число.
/// </summary>
public static class KnowledgeRatingRules
{
    /// <summary>Навык рейтинга в Realms of Terrinoth.</summary>
    public const string LoreSkill = "Knowledge (Lore)";

    /// <summary>Альтернатива, которую открывает «Тёмное прозрение».</summary>
    public const string ForbiddenSkill = "Knowledge (Forbidden)";

    /// <summary>В Genesys Core знаниевый навык один и не делится на области.</summary>
    public const string CoreKnowledgeSkill = "Knowledge";

    /// <summary>Талант-исключение; сравнивается по стабильному английскому имени.</summary>
    public const string DarkInsightTalent = "Dark Insight";

    /// <summary>Навык, названный правилами системы по умолчанию.</summary>
    public static string DefaultSkill(GameSystem system) =>
        system == GameSystem.RealmsOfTerrinoth ? LoreSkill : CoreKnowledgeSkill;

    /// <summary>Персонаж владеет талантом-исключением.</summary>
    public static bool HasDarkInsight(IEnumerable<string> talentNames) =>
        talentNames.Contains(DarkInsightTalent, StringComparer.Ordinal);

    /// <summary>
    /// Источники рейтинга, доступные персонажу: первый — тот, что называют правила системы.
    /// Второй появляется только вместе с талантом и только в Realms of Terrinoth, где вообще
    /// существует Запретное знание.
    /// </summary>
    /// <param name="ranks">Ранги навыков персонажа по английскому имени.</param>
    public static IReadOnlyList<KnowledgeRatingOption> Options(
        GameSystem system, IReadOnlyDictionary<string, int> ranks, bool hasDarkInsight)
    {
        var options = new List<KnowledgeRatingOption>
        {
            new(DefaultSkill(system), Rank(ranks, DefaultSkill(system)), KnowledgeRatingReason.Default),
        };
        if (hasDarkInsight && system == GameSystem.RealmsOfTerrinoth)
            options.Add(new(ForbiddenSkill, Rank(ranks, ForbiddenSkill), KnowledgeRatingReason.DarkInsight));
        return options;
    }

    /// <summary>
    /// Выбранный источник. Пустой или чужой выбор — это выбор по умолчанию, а не ошибка: список
    /// источников зависит от таланта, и клиент, отставший на один талант, не должен ломаться.
    /// </summary>
    public static KnowledgeRatingOption Resolve(
        IReadOnlyList<KnowledgeRatingOption> options, string? chosenSkill) =>
        options.FirstOrDefault(o => string.Equals(o.Skill, chosenSkill, StringComparison.Ordinal))
        ?? options[0];

    private static int Rank(IReadOnlyDictionary<string, int> ranks, string skill) =>
        ranks.TryGetValue(skill, out var value) ? Math.Max(0, value) : 0;
}
