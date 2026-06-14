using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record SkillDefDto(Guid Id, string Name, string NameRu, CharacteristicType Characteristic, SkillKind Kind,
    string SafeDescription, string Source, bool IsCustom);
