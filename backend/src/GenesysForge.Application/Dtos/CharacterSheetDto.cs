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
    List<CharacterItemDto> Items);
