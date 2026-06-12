using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CreateCharacterRequest(string Name, GameSystem System, Guid ArchetypeId, Guid CareerId,
    List<string>? FreeCareerSkillNames);
