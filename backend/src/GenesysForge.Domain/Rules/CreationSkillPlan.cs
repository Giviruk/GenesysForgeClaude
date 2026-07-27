namespace GenesysForge.Domain.Rules;

/// <summary>Одна бесплатная прибавка рангов при создании персонажа.</summary>
/// <param name="Ranks">Сколько бесплатных рангов добавляет источник.</param>
/// <param name="SourceLabel">Человекочитаемое имя источника — для текста ошибки и UI.</param>
public sealed record CreationSkillGrant(int Ranks, string SourceLabel);

/// <summary>Итоговое состояние одного навыка в плане создания.</summary>
public sealed record CreationSkillPlanEntry(
    Guid SkillDefId,
    string SkillName,
    int TotalRanks,
    bool IsCareer,
    IReadOnlyList<CreationSkillGrant> Grants);

/// <summary>Нарушение предела рангов при создании.</summary>
public sealed record CreationSkillPlanViolation(
    string SkillName,
    int TotalRanks,
    IReadOnlyList<CreationSkillGrant> Grants)
{
    public string Describe() =>
        $"«{SkillName}» получает ранг {TotalRanks} при создании "
        + $"(предел — {GenesysRules.MaxSkillRankAtCreation}). Источники: "
        + string.Join(", ", Grants.Select(g => $"{g.SourceLabel} +{g.Ranks}"))
        + ".";
}

/// <summary>
/// План бесплатных стартовых рангов: собирается целиком до первой записи в БД, затем проверяется.
/// Предел ранга при создании применяется к сумме всех бесплатных источников (вид, карьера, выбор
/// игрока и любой другой); превышение — ошибка, обрезать ранг до предела нельзя.
/// </summary>
public sealed class CreationSkillPlan
{
    private readonly Dictionary<Guid, Builder> _entries = [];

    private sealed class Builder
    {
        public required string SkillName { get; init; }
        public int TotalRanks { get; set; }
        public bool IsCareer { get; set; }
        public List<CreationSkillGrant> Grants { get; } = [];
    }

    /// <summary>Отмечает навык как карьерный без выдачи рангов.</summary>
    public void MarkCareer(Guid skillDefId, string skillName)
    {
        Entry(skillDefId, skillName).IsCareer = true;
    }

    /// <summary>Добавляет бесплатные ранги от одного источника. Нулевые/отрицательные игнорируются.</summary>
    public void AddFreeRanks(Guid skillDefId, string skillName, int ranks, string sourceLabel)
    {
        if (ranks <= 0) return;
        var entry = Entry(skillDefId, skillName);
        entry.TotalRanks += ranks;
        entry.Grants.Add(new CreationSkillGrant(ranks, sourceLabel));
    }

    /// <summary>Итоговые ранги навыка в плане (0, если навык не затронут).</summary>
    public int RanksOf(Guid skillDefId) => _entries.TryGetValue(skillDefId, out var e) ? e.TotalRanks : 0;

    /// <summary>Все навыки плана в порядке первого появления.</summary>
    public IReadOnlyList<CreationSkillPlanEntry> Entries =>
        _entries.Select(kv => new CreationSkillPlanEntry(
            kv.Key, kv.Value.SkillName, kv.Value.TotalRanks, kv.Value.IsCareer, kv.Value.Grants)).ToList();

    /// <summary>
    /// Все навыки, чей итоговый бесплатный ранг превышает предел создания. Пустой список — план валиден.
    /// </summary>
    public IReadOnlyList<CreationSkillPlanViolation> Validate() =>
        _entries.Values
            .Where(e => e.TotalRanks > GenesysRules.MaxSkillRankAtCreation)
            .OrderBy(e => e.SkillName, StringComparer.Ordinal)
            .Select(e => new CreationSkillPlanViolation(e.SkillName, e.TotalRanks, e.Grants))
            .ToList();

    private Builder Entry(Guid skillDefId, string skillName)
    {
        if (!_entries.TryGetValue(skillDefId, out var entry))
            _entries[skillDefId] = entry = new Builder { SkillName = skillName };
        return entry;
    }
}
