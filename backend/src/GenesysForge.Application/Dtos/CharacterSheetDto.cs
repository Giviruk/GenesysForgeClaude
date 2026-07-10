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
    List<CharacterItemDto> Items,
    string? Desire = null,
    string? Fear = null,
    string? Strength = null,
    string? Flaw = null,
    string? Background = null,
    List<CharacterCriticalInjuryDto>? CriticalInjuries = null,
    string? PortraitUrl = null);

/// <summary>Критическое ранение персонажа (U-23).</summary>
public record CharacterCriticalInjuryDto(
    Guid Id, string? RuleCode, string NameRu, string? Severity, int? RollResult, string? Notes);
