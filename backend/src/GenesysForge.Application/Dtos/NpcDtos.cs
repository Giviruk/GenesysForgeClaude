using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record NpcSkillDto(string Name, int Ranks);
public record NpcAbilityDto(string Name, string Description);

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
    NpcVisibility Visibility,
    Guid? CampaignId,
    bool IsMine,
    IReadOnlyList<NpcSkillDto> Skills,
    IReadOnlyList<NpcAbilityDto> Abilities,
    IReadOnlyList<string> Talents,
    IReadOnlyList<string> Equipment,
    IReadOnlyList<string> Tags,
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
    NpcVisibility Visibility,
    Guid? CampaignId,
    List<NpcSkillDto>? Skills,
    List<NpcAbilityDto>? Abilities,
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
