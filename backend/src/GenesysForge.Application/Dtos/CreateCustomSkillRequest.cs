using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CreateCustomSkillRequest(GameSystem System, string Name, CharacteristicType Characteristic, SkillKind Kind);
