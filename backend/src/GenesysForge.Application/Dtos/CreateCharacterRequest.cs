using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CreateCharacterRequest(string Name, GameSystem System, Guid ArchetypeId, Guid CareerId,
    List<string>? FreeCareerSkillNames, List<ArchetypeSkillChoice>? ArchetypeSkillChoices = null,
    List<CareerGearChoice>? CareerGearChoices = null);

/// <summary>Выбор игрока для группы стартовых навыков вида (например «any-noncareer» → 2 навыка).</summary>
public record ArchetypeSkillChoice(string ChoiceGroup, List<string> SkillNames);

/// <summary>Выбор игрока для слота стартового снаряжения карьеры: какой вариант (ChoiceOption) выбран.</summary>
public record CareerGearChoice(string ChoiceGroup, int OptionIndex);
