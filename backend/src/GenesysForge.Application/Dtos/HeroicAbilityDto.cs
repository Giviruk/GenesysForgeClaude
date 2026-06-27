using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record HeroicAbilityUpgradeDto(int Level, int Cost, string Description, string Notes);

/// <summary>Структурный эффект автоматизации способности (U-18).</summary>
public record RuleEffectDto(RuleEffectKind Kind, int Amount, string Duration, string Description);

public record HeroicAbilityDto(Guid Id, string Code, string Name, string NameRu, string Description, string SafeDescription,
    string Source, bool IsCustom,
    string Requirement, string ActivationCost, string Activation, string Duration, string Frequency, string Notes,
    List<HeroicAbilityUpgradeDto> Upgrades, List<RuleEffectDto> Effects);
