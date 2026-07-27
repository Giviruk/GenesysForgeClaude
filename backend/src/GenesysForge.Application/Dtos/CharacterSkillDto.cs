using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>Откуда навык получил карьерный статус: <c>Career</c>, <c>Species</c> или <c>Talent</c>.</summary>
public record CareerSkillSourceDto(string Source, string SourceName);

public record CharacterSkillDto(Guid SkillDefId, string Name, string NameRu, SkillKind Kind, CharacteristicType Characteristic,
    int Ranks, bool IsCareer, DicePoolDto Pool, int NextRankCost, int FreeRanks,
    IReadOnlyList<CareerSkillSourceDto> CareerSources,
    /// <summary>
    /// Навык исключён из набора навыков системы, но у персонажа остались купленные ранги
    /// (ROT-CLEAN-3.2). Ранги работают, новый ранг купить нельзя.
    /// </summary>
    bool SourceMismatch = false);
