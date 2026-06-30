using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record HomebrewPackListItemDto(
    Guid Id,
    string Name,
    string Description,
    GameSystem System,
    bool IsShared,
    bool IsEnabledByDefault,
    int EntryCount,
    DateTime UpdatedAt);

public record HomebrewPackShareDto(string Token, string Path);

public record HomebrewPackToggleRequest(bool IsEnabled);

public record HomebrewPackImportResult(Guid Id, string Name, int EntryCount);

public record HomebrewPackExportDto(
    string Format,
    string Name,
    string? Description,
    GameSystem System,
    List<HomebrewSkillDto>? Skills,
    List<HomebrewTalentDto>? Talents,
    List<HomebrewItemDto>? Items,
    List<HomebrewHeroicAbilityDto>? HeroicAbilities,
    List<HomebrewArchetypeDto>? Archetypes,
    List<HomebrewCareerDto>? Careers);

public record HomebrewSkillDto(
    string? Code,
    string Name,
    string? NameRu,
    CharacteristicType Characteristic,
    SkillKind Kind,
    string? Description,
    string? SafeDescription,
    string? Source);

public record HomebrewTalentDto(
    string? Code,
    string Name,
    string? NameRu,
    int Tier,
    bool IsRanked,
    string? Activation,
    string? Description,
    string? SafeDescription,
    string? Source,
    int WoundBonus,
    int StrainBonus,
    int SoakBonus,
    int MeleeDefenseBonus,
    int RangedDefenseBonus);

public record HomebrewItemDto(
    string? Code,
    string Name,
    string? NameRu,
    ItemKind Kind,
    int Encumbrance,
    int SoakBonus,
    int MeleeDefense,
    int RangedDefense,
    int EncumbranceThresholdBonus,
    string? Description,
    string? SafeDescription,
    string? Source,
    int Price,
    int Rarity,
    string? SkillName,
    string? Damage,
    string? Crit,
    string? RangeBand,
    string? Properties);

public record HomebrewHeroicAbilityDto(
    string? Code,
    string Name,
    string? NameRu,
    string? Description,
    string? SafeDescription,
    string? Source,
    string? Requirement,
    string? ActivationCost,
    string? Activation,
    string? Duration,
    string? Frequency,
    string? Notes);

public record HomebrewArchetypeDto(
    string? Code,
    string Name,
    string? NameRu,
    int Brawn,
    int Agility,
    int Intellect,
    int Cunning,
    int Willpower,
    int Presence,
    int WoundBase,
    int StrainBase,
    int StartingXp,
    string? Description,
    string? SafeDescription,
    string? Source,
    List<HomebrewArchetypeAbilityDto>? Abilities);

public record HomebrewArchetypeAbilityDto(
    string? Code,
    string NameRu,
    string? NameEn,
    string? SafeDescription);

public record HomebrewCareerDto(
    string? Code,
    string Name,
    string? NameRu,
    string? Description,
    string? SafeDescription,
    string? Source,
    List<string>? CareerSkillNames,
    int StartingMoneyFixed,
    string? StartingMoneyDice);
