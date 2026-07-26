using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record ArchetypeDto(Guid Id, string Name, string NameRu, int Brawn, int Agility, int Intellect, int Cunning,
    int Willpower, int Presence, int WoundBase, int StrainBase, int StartingXp,
    string Description, string SafeDescription, string Source, bool IsCustom,
    IReadOnlyList<ArchetypeAbilityDto> Abilities, IReadOnlyList<ArchetypeStartingSkillDto> StartingSkills,
    string DescriptionEn = "",
    /// <summary>Размер существа: 1 у всех видов RoT, 0 у обоих гномов (ROT-SPECIES-01).</summary>
    int Silhouette = 1);

public record ArchetypeAbilityDto(string Code, string NameRu, string NameEn, string SafeDescription,
    ArchetypeAbilityAutomationKind AutomationKind, string DescriptionEn = "",
    /// <summary>Исполняемый тип правила — единственный источник механики, не имя и не описание.</summary>
    SpeciesAbilityRuleKind RuleKind = SpeciesAbilityRuleKind.Manual,
    int RuleValue = 0, string RuleParameters = "",
    int UsesPerScope = 0, AbilityUseScope UseScope = AbilityUseScope.None, int StoryPointCost = 0,
    /// <summary>Допустимые коды для способности-выбора; пусто у обычных способностей.</summary>
    IReadOnlyList<string>? ChoiceOptions = null);

public record ArchetypeStartingSkillDto(string SkillName, string NameRu, int FreeRanks,
    bool IsChoice, string ChoiceGroup, int ChoiceCount, bool GrantsCareerSkill = false);
