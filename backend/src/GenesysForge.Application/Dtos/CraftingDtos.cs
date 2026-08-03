using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>Строка таблицы трат символов, как её видит клиент.</summary>
public record CraftingSpendDto(
    string Code,
    string RowCode,
    CraftingKind Table,
    string NameRu,
    string NameEn,
    string Description,
    string DescriptionEn,
    int AdvantageCost,
    int ThreatCost,
    int TriumphCost,
    int DespairCost,
    bool IsNegative,
    bool Repeatable,
    bool RequiresGmConfirmation,
    bool RequiresParameter,
    /// <summary>Механика траты; <c>Descriptive</c> — приложение её не исполняет.</summary>
    CraftingSpendEffect Effect,
    bool WeaponOnly,
    int SortOrder);

/// <summary>Выбранная трата в запросе разрешения.</summary>
public record CraftingSpendInput(string Code, int Count = 1, string? Parameter = null, string PaidWith = "advantage");

/// <summary>Трата, записанная в проекте.</summary>
public record CraftingProjectSpendDto(string Code, int Count, string Parameter, string PaidWith, string TextRu, string TextEn);

/// <summary>Тело создания проекта и его предпросмотра.</summary>
/// <param name="ItemDefId">Цель: изготавливаемая запись или зачаровываемая основа.</param>
/// <param name="SkillName">Навык проверки; пусто — навык по умолчанию для вида работы.</param>
/// <param name="CostPercent">Доля расчётной стоимости компонентов, 50…200 с шагом 25.</param>
/// <param name="CostOverride">Своя цена компонентов; требует причины и отменяет долю.</param>
/// <param name="DifficultyOverride">Сложность, назначенная явно (например, за прошлый триумф).</param>
/// <param name="TimeOverride">Время, назначенное явно.</param>
/// <param name="RoughSurvival">Грубая работа Выживанием по разрешению ведущего.</param>
public record CraftingProjectInput(
    Guid ItemDefId,
    Guid? BaseCharacterItemId = null,
    CraftingKind Kind = CraftingKind.Item,
    string? SkillName = null,
    int CostPercent = 100,
    int? CostOverride = null,
    string? CostOverrideReason = null,
    int? DifficultyOverride = null,
    string? DifficultyReason = null,
    int? TimeOverride = null,
    string? TimeReason = null,
    string? Requirements = null,
    string? Intent = null,
    bool RoughSurvival = false);

/// <summary>Тело разрешения проекта: символы броска и распределение.</summary>
public record CraftingResolveInput(
    int NetSuccesses,
    int Advantages = 0,
    int Threats = 0,
    int Triumphs = 0,
    int Despairs = 0,
    IReadOnlyList<CraftingSpendInput>? Spends = null);

/// <summary>Предпросмотр проекта: числа, посчитанные сервером, до любой записи.</summary>
public record CraftingPreviewDto(
    CraftingKind Kind,
    string TargetName,
    int? TargetPrice,
    int? TargetRarity,
    string SkillName,
    int BaseDifficulty,
    int Difficulty,
    int BaseTime,
    int Time,
    /// <summary>Единица времени: <c>days</c> или <c>hours</c>.</summary>
    string TimeUnit,
    int ListedCost,
    int CostPercent,
    int? CostOverride,
    int Cost,
    bool IsWeapon,
    IReadOnlyList<CraftingSpendDto> Spends);

/// <summary>Проект целиком.</summary>
public record CraftingProjectDto(
    Guid Id,
    CraftingKind Kind,
    CraftingProjectStatus Status,
    Guid ItemDefId,
    Guid? BaseCharacterItemId,
    string TargetName,
    int? TargetPrice,
    int? TargetRarity,
    string SkillName,
    int BaseDifficulty,
    int Difficulty,
    string DifficultyReason,
    int BaseTime,
    int Time,
    string TimeUnit,
    string TimeReason,
    int ListedCost,
    int CostPercent,
    int? CostOverride,
    string CostOverrideReason,
    int Cost,
    string Requirements,
    string Intent,
    bool RoughSurvival,
    int NetSuccesses,
    int Advantages,
    int Threats,
    int Triumphs,
    int Despairs,
    Guid? CreatedCharacterItemId,
    string Outcome,
    IReadOnlyList<CraftingProjectSpendDto> Spends,
    DateTime CreatedAt,
    DateTime? ResolvedAt);
