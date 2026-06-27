using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record NpcSkillDto(string Name, int Ranks);
public record NpcAbilityDto(string Name, string Description);
public record NpcAttackQualityDto(string QualityCode, string NameRu, int? Rating);
public record NpcAttackDto(
    string Name,
    string SkillName,
    string Damage,
    string Critical,
    string RangeBand,
    string Notes,
    IReadOnlyList<NpcAttackQualityDto> Qualities);

/// <summary>Карточка NPC в списке библиотеки.</summary>
public record NpcListItemDto(
    Guid Id,
    string Name,
    GameSystem System,
    NpcKind Kind,
    NpcRole Role,
    int Soak,
    int WoundThreshold,
    int? StrainThreshold,
    NpcVisibility Visibility,
    Guid? CampaignId,
    bool IsMine,
    IReadOnlyList<NpcSkillDto> Skills,
    IReadOnlyList<string> Tags,
    DateTime CreatedAt);

/// <summary>Полная карточка NPC.</summary>
public record NpcDetailDto(
    Guid Id,
    string Name,
    GameSystem System,
    NpcKind Kind,
    NpcRole Role,
    string Description,
    string Source,
    int Brawn,
    int Agility,
    int Intellect,
    int Cunning,
    int Willpower,
    int Presence,
    int WoundThreshold,
    int? StrainThreshold,
    int Soak,
    int MeleeDefense,
    int RangedDefense,
    int Silhouette,
    string Tactics,
    NpcVisibility Visibility,
    Guid? CampaignId,
    bool IsMine,
    IReadOnlyList<NpcSkillDto> Skills,
    IReadOnlyList<NpcAbilityDto> Abilities,
    IReadOnlyList<NpcAttackDto> Attacks,
    IReadOnlyList<string> Talents,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Warnings,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>Тело создания/редактирования NPC.</summary>
public record NpcInput(
    string Name,
    GameSystem System,
    NpcKind Kind,
    NpcRole Role,
    string? Description,
    string? Source,
    int Brawn,
    int Agility,
    int Intellect,
    int Cunning,
    int Willpower,
    int Presence,
    int WoundThreshold,
    int? StrainThreshold,
    int Soak,
    int MeleeDefense,
    int RangedDefense,
    int Silhouette,
    string? Tactics,
    NpcVisibility Visibility,
    Guid? CampaignId,
    List<NpcSkillDto>? Skills,
    List<NpcAbilityDto>? Abilities,
    List<NpcAttackDto>? Attacks,
    List<string>? Talents,
    List<string>? Equipment,
    List<string>? Tags);

/// <summary>Параметры быстрого детерминированного черновика.</summary>
public record QuickDraftRequest(
    GameSystem System,
    NpcKind Kind,
    NpcRole Role,
    NpcPowerLevel PowerLevel,
    CharacteristicType? PrimaryCharacteristic,
    NpcCombatStyle CombatStyle,
    string? Name);
