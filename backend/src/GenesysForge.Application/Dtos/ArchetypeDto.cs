using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record ArchetypeDto(Guid Id, string Name, string NameRu, int Brawn, int Agility, int Intellect, int Cunning,
    int Willpower, int Presence, int WoundBase, int StrainBase, int StartingXp,
    string Description, string SafeDescription, string Source, bool IsCustom,
    IReadOnlyList<ArchetypeAbilityDto> Abilities, IReadOnlyList<ArchetypeStartingSkillDto> StartingSkills,
    string DescriptionEn = "");

public record ArchetypeAbilityDto(string Code, string NameRu, string NameEn, string SafeDescription,
    ArchetypeAbilityAutomationKind AutomationKind, string DescriptionEn = "");

public record ArchetypeStartingSkillDto(string SkillName, string NameRu, int FreeRanks,
    bool IsChoice, string ChoiceGroup, int ChoiceCount);
