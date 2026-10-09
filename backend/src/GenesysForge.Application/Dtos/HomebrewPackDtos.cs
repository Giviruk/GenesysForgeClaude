using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record HomebrewPackListItemDto(
    Guid Id,
    string Name,
    string Description,
    GameSystem System,
    bool IsShared,
    bool IsEnabledByDefault,
    int EntryCount,
    DateTime UpdatedAt,
    int ExclusionCount = 0,
    IReadOnlyList<PackCampaignDto>? Campaigns = null);

public record CampaignHomebrewPackDto(Guid Id, string Name, GameSystem System,
    bool IsEnabled, bool IsMine, int EntryCount, string OwnerName, bool OwnerIsMember, DateTime? LastChangedAt,
    DateTime ConnectedAt, bool ChangedAfterConnection,
    ContentUpdatePolicy UpdatePolicy = ContentUpdatePolicy.Auto, ContentConnectionStatus Status = ContentConnectionStatus.Active,
    int ExclusionCount = 0, IReadOnlyList<CampaignPackEntryDto>? Entries = null, Guid? ProposedBy = null,
    IReadOnlyList<BaseCatalogEntryDto>? Exclusions = null);

public record HomebrewPackShareDto(string Token, string Path);

public record HomebrewPackToggleRequest(bool IsEnabled, ContentUpdatePolicy? UpdatePolicy = null);

public record HomebrewPackImportResult(Guid Id, string Name, int EntryCount, IReadOnlyList<string>? Warnings = null);

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
    List<HomebrewCareerDto>? Careers,
    IReadOnlyList<BaseContentRef>? Exclusions = null,
    List<HomebrewAttachmentDto>? Attachments = null,
    List<HomebrewMountDto>? Mounts = null);

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
    int RangedDefenseBonus,
    TalentCategory Category = TalentCategory.General);

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

public record HomebrewAttachmentDto(
    string Code,
    string Name,
    string NameRu,
    int HardPointCost,
    int? Price,
    int Rarity,
    bool IsEnchantment,
    ItemKind HostKind,
    WeaponFormTraits RequiredTraits,
    WeaponFormTraits RequiredAnyTraits,
    WeaponFormTraits ForbiddenTraits,
    List<HomebrewAttachmentEffectDto> Effects,
    string Description,
    string SafeDescription,
    string DescriptionEn,
    string Source);

public record HomebrewAttachmentEffectDto(
    AttachmentEffectKind Kind,
    string QualityCode,
    string OppositeQualityCode,
    string SkillName,
    int Value,
    int Increment,
    AttachmentEffectCondition Condition,
    string Note);

public record HomebrewMountDto(
    string Code,
    string Name,
    string NameRu,
    TransportKind TransportKind,
    MovementMode MovementMode,
    bool RequiresTraction,
    NpcKind Kind,
    int Brawn,
    int Agility,
    int Intellect,
    int Cunning,
    int Willpower,
    int Presence,
    int Soak,
    int WoundThreshold,
    int? StrainThreshold,
    int MeleeDefense,
    int RangedDefense,
    int Silhouette,
    int Capacity,
    int? Price,
    int Rarity,
    List<string> IncludedGear,
    bool RequiresRidingCheck,
    List<HomebrewMountSkillDto> Skills,
    List<HomebrewMountAbilityDto> Abilities,
    List<HomebrewMountAttackDto> Attacks,
    string Description,
    string SafeDescription,
    string DescriptionEn,
    string Source);

public record HomebrewMountSkillDto(
    string Name,
    int Ranks,
    bool IsGroupSkill);

public record HomebrewMountAbilityDto(
    string Name,
    string NameRu,
    string Description,
    string DescriptionEn);

public record HomebrewMountAttackDto(
    string Name,
    string NameRu,
    string SkillName,
    int Damage,
    int Critical,
    WeaponRange Range,
    List<string> QualityCodes);
