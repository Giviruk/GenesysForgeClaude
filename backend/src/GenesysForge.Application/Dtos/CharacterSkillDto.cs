using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>Откуда навык получил карьерный статус: <c>Career</c>, <c>Species</c> или <c>Talent</c>.</summary>
public record CareerSkillSourceDto(string Source, string SourceName);

public record CharacterSkillDto(Guid SkillDefId, string Name, string NameRu, SkillKind Kind, CharacteristicType Characteristic,
    int Ranks, bool IsCareer, DicePoolDto Pool, int NextRankCost, int FreeRanks,
    IReadOnlyList<CareerSkillSourceDto> CareerSources);
