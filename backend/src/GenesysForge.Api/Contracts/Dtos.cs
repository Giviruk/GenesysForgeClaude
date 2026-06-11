using GenesysForge.Domain;

namespace GenesysForge.Api.Contracts;

// ---------- Auth ----------
public record RegisterRequest(string Email, string Password, string DisplayName);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Token, Guid UserId, string Email, string DisplayName);

// ---------- Reference ----------
public record SkillDefDto(Guid Id, string Name, CharacteristicType Characteristic, SkillKind Kind, bool IsCustom);
public record TalentDefDto(Guid Id, string Name, int Tier, bool IsRanked, string Activation, string Description,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus, bool IsCustom);
public record ItemDefDto(Guid Id, string Name, ItemKind Kind, int Encumbrance, int SoakBonus, int MeleeDefense,
    int RangedDefense, int EncumbranceThresholdBonus, string Description, int Price, int Rarity, bool IsCustom);
public record HeroicAbilityDto(Guid Id, string Name, string Description, bool IsCustom);
public record ArchetypeDto(Guid Id, string Name, int Brawn, int Agility, int Intellect, int Cunning, int Willpower,
    int Presence, int WoundBase, int StrainBase, int StartingXp, string Description);
public record CareerDto(Guid Id, string Name, string Description, List<string> CareerSkillNames);
public record ReferenceResponse(
    List<ArchetypeDto> Archetypes,
    List<CareerDto> Careers,
    List<SkillDefDto> Skills,
    List<TalentDefDto> Talents,
    List<ItemDefDto> Items,
    List<HeroicAbilityDto> HeroicAbilities);

// ---------- Custom content ----------
public record CreateCustomSkillRequest(GameSystem System, string Name, CharacteristicType Characteristic, SkillKind Kind);
public record CreateCustomTalentRequest(GameSystem System, string Name, int Tier, bool IsRanked, string Activation,
    string Description, int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus);
public record CreateCustomItemRequest(GameSystem System, string Name, ItemKind Kind, int Encumbrance, int SoakBonus,
    int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus, string Description, int Price, int Rarity);
public record CreateCustomHeroicAbilityRequest(string Name, string Description);

// ---------- Characters ----------
public record CreateCharacterRequest(string Name, GameSystem System, Guid ArchetypeId, Guid CareerId,
    List<string>? FreeCareerSkillNames);
public record CharacterListItemDto(Guid Id, string Name, GameSystem System, string Archetype, string Career,
    bool IsCreationPhase, DateTime CreatedAt);
public record UpdateCharacterRequest(string? Name, int? TotalXp, int? WoundsCurrent, int? StrainCurrent);
public record BuyTalentRequest(Guid TalentDefId);
public record SetHeroicAbilityRequest(Guid? HeroicAbilityId);
public record AddItemRequest(Guid ItemDefId, int Quantity, ItemState State);
public record UpdateItemRequest(ItemState? State, int? Quantity);

public record DicePoolDto(int Ability, int Proficiency);
public record CharacterSkillDto(Guid SkillDefId, string Name, SkillKind Kind, CharacteristicType Characteristic,
    int Ranks, bool IsCareer, DicePoolDto Pool, int NextRankCost);
public record CharacterTalentDto(Guid TalentDefId, string Name, int Tier, bool IsRanked, int Ranks,
    string Activation, string Description);
public record CharacterItemDto(Guid Id, Guid ItemDefId, string Name, ItemKind Kind, ItemState State, int Quantity,
    int Encumbrance, int SoakBonus, int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus, int Load, string Description);
public record DerivedDto(int WoundThreshold, int StrainThreshold, int Soak, int MeleeDefense, int RangedDefense,
    int EncumbranceThreshold, int EncumbranceLoad, bool Encumbered);

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
    DerivedDto Derived,
    List<CharacterSkillDto> Skills,
    List<CharacterTalentDto> Talents,
    Dictionary<int, int> TalentTierCounts,
    HeroicAbilityDto? HeroicAbility,
    List<CharacterItemDto> Items);

public record ErrorResponse(string Message);
