namespace GenesysForge.Application.Dtos;

public record HeroicAbilityUpgradeDto(int Level, int Cost, string Description, string Notes);

public record HeroicAbilityDto(Guid Id, string Name, string NameRu, string Description, string SafeDescription,
    string Source, bool IsCustom,
    string Requirement, string ActivationCost, string Activation, string Duration, string Frequency, string Notes,
    List<HeroicAbilityUpgradeDto> Upgrades);
