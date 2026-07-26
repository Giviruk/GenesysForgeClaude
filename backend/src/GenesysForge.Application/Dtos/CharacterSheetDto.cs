using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterSheetDto(
    Guid Id,
    string Name,
    GameSystem System,
    ArchetypeDto Archetype,
    CareerDto Career,
    Dictionary<string, int> Characteristics,
    int TotalXp,
    int SpentXp,
    int AvailableXp,
    bool IsCreationPhase,
    int WoundsCurrent,
    int StrainCurrent,
    int Money,
    DerivedDto Derived,
    List<CharacterSkillDto> Skills,
    List<CharacterTalentDto> Talents,
    Dictionary<int, int> TalentTierCounts,
    HeroicAbilityDto? HeroicAbility,
    int HeroicUpgradeRank,
    int HeroicUpgradePointsTotal,
    int HeroicUpgradePointsSpent,
    HeroicUpgradeStateDto HeroicUpgrades,
    List<CharacterItemDto> Items,
    string? Desire = null,
    string? Fear = null,
    string? Strength = null,
    string? Flaw = null,
    string? Background = null,
    List<CharacterCriticalInjuryDto>? CriticalInjuries = null,
    string? PortraitUrl = null,
    /// <summary>Режим стартового снаряжения, выбранный при создании (ROT-CRE-03).</summary>
    StartingEquipmentMode StartingEquipmentMode = StartingEquipmentMode.StandardMoney,
    /// <summary>
    /// Остаток бюджета стартовых покупок. Тратится раньше <see cref="Money"/> и только до
    /// завершения создания; это отдельный счёт, а не часть кошелька.
    /// </summary>
    int StartingPurchaseBudget = 0,
    /// <summary>Происхождение зафиксированных порогов ран/стрейна (ROT-CRE-02).</summary>
    ThresholdSnapshotProvenance ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.None,
    /// <summary>Данные восстановлены неоднозначно и требуют ручной проверки владельцем/GM.</summary>
    bool RulesReviewRequired = false,
    /// <summary>Выбранная видовая способность там, где вид требует выбора (ROT-SPECIES-01).</summary>
    string SpeciesAbilityChoiceCode = "",
    /// <summary>
    /// Вид требует выбора видовой способности, а он ещё не сделан. Автоматизация выбранной
    /// способности до исправления не применяется; выбирать за игрока сервер не будет.
    /// </summary>
    bool SpeciesChoiceIncomplete = false,
    /// <summary>Итоговый silhouette персонажа с учётом способности <c>Small</c>.</summary>
    int Silhouette = 1);

public record HeroicUpgradeStateDto(
    int PowerRank,
    int DurationRanks,
    int FrequencyRanks,
    bool Story,
    List<HeroicSecondaryEffectDto> SecondaryEffects);

/// <summary>Критическое ранение персонажа (U-23).</summary>
public record CharacterCriticalInjuryDto(
    Guid Id, string? RuleCode, string NameRu, string? Severity, int? RollResult, string? Notes);
