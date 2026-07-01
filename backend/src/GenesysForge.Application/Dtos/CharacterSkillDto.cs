using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterSkillDto(Guid SkillDefId, string Name, string NameRu, SkillKind Kind, CharacteristicType Characteristic,
    int Ranks, bool IsCareer, DicePoolDto Pool, int NextRankCost, int FreeRanks);
