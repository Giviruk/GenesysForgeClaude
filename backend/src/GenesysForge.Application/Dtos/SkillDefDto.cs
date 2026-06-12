using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record SkillDefDto(Guid Id, string Name, CharacteristicType Characteristic, SkillKind Kind, bool IsCustom);
