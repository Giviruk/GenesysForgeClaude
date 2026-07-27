namespace GenesysForge.Domain.Rules;

/// <summary>Канал защиты: к каким атакам применим источник (ROT-CMB-03).</summary>
public enum DefenseScope
{
    /// <summary>И к ближним, и к дальним атакам.</summary>
    General = 0,

    /// <summary>Только к ближним атакам.</summary>
    Melee = 1,

    /// <summary>Только к дальним атакам.</summary>
    Ranged = 2,
}

/// <summary>
/// Как источник влияет на защиту. Разница принципиальна: «получает Defense N» задаёт значение и
/// конкурирует с другими такими же за максимум, а «+N» прибавляется поверх лучшего значения.
/// </summary>
public enum DefenseMode
{
    /// <summary>Задаёт значение защиты (броня, укрытие, Guarded Stance, видовая Nimble).</summary>
    Provides = 0,

    /// <summary>Прибавляет к защите (Defensive, Deflection, явно суммируемые таланты и предметы).</summary>
    Increases = 1,
}

/// <summary>Один вклад в защиту с источником — нужен и для расчёта, и для объяснения итога.</summary>
/// <param name="SourceType">Категория источника: <c>Armor</c>, <c>Talent</c>, <c>Species</c>, <c>Cover</c>…</param>
/// <param name="SourceName">Отображаемое имя источника.</param>
public sealed record DefenseContribution(
    string SourceType,
    string SourceName,
    DefenseScope Scope,
    DefenseMode Mode,
    int Value);

/// <summary>Разбор одного канала защиты: что победило, что проигнорировано и где сработал предел.</summary>
/// <param name="Raw">Значение до применения предела.</param>
/// <param name="Effective">Итоговое значение: не больше <see cref="DefenseAggregator.MaxDefense"/>.</param>
/// <param name="Provider">Победивший источник-провайдер; <c>null</c>, если провайдеров нет.</param>
/// <param name="IgnoredProviders">Проигравшие провайдеры — они не складываются с победившим.</param>
/// <param name="Increases">Все применённые надбавки.</param>
public sealed record DefenseBreakdown(
    int Raw,
    int Effective,
    DefenseContribution? Provider,
    IReadOnlyList<DefenseContribution> IgnoredProviders,
    IReadOnlyList<DefenseContribution> Increases)
{
    /// <summary>Итог упёрся в предел — UI показывает и сырое значение.</summary>
    public bool Capped => Raw > Effective;
}

/// <summary>
/// Сводит вклады в защиту по правилам ROT-CMB-03: источники «получает Defense N» не складываются
/// между собой (берётся максимум), надбавки «+N» складываются друг с другом и с лучшим провайдером,
/// итог ограничен четырьмя. Вклады сверх предела не выбрасываются: после исчезновения другого
/// источника пересчёт обязан дать верное значение.
/// </summary>
public static class DefenseAggregator
{
    /// <summary>Универсальный предел защиты — одинаков для персонажей и NPC.</summary>
    public const int MaxDefense = 4;

    /// <summary>Разбор ближнего канала: применимы General и Melee.</summary>
    public static DefenseBreakdown Melee(IEnumerable<DefenseContribution> contributions) =>
        Compute(contributions, DefenseScope.Melee);

    /// <summary>Разбор дальнего канала: применимы General и Ranged.</summary>
    public static DefenseBreakdown Ranged(IEnumerable<DefenseContribution> contributions) =>
        Compute(contributions, DefenseScope.Ranged);

    private static DefenseBreakdown Compute(IEnumerable<DefenseContribution> contributions, DefenseScope channel)
    {
        var applicable = contributions
            .Where(c => c.Scope == DefenseScope.General || c.Scope == channel)
            .ToList();

        var providers = applicable
            .Where(c => c.Mode == DefenseMode.Provides && c.Value > 0)
            .OrderByDescending(c => c.Value)
            .ThenBy(c => c.SourceName, StringComparer.Ordinal)
            .ToList();
        var increases = applicable.Where(c => c.Mode == DefenseMode.Increases && c.Value != 0).ToList();

        var winner = providers.FirstOrDefault();
        var raw = Math.Max(0, (winner?.Value ?? 0) + increases.Sum(c => c.Value));

        return new DefenseBreakdown(
            raw,
            Math.Min(MaxDefense, raw),
            winner,
            [.. providers.Skip(1)],
            increases);
    }
}
