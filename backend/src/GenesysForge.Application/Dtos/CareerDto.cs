using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Dtos;

public record CareerDto(Guid Id, string Name, string NameRu, string Description, string SafeDescription,
    string Source, bool IsCustom, List<string> CareerSkillNames,
    int StartingMoneyFixed, string StartingMoneyDice,
    IReadOnlyList<CareerStartingGearDto> StartingGear, IReadOnlyList<CareerRuleDto> Rules,
    string DescriptionEn = "");

public record CareerStartingGearDto(string ItemCode, string ItemNameRu, int Quantity,
    bool IsChoice, string ChoiceGroup, int ChoiceOption);

public record CareerRuleDto(string Code, CareerRuleKind Kind, string Description, string DescriptionEn = "");
