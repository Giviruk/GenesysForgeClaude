namespace GenesysForge.Application.Dtos;

public record CareerDto(Guid Id, string Name, string NameRu, string Description, string SafeDescription,
    string Source, List<string> CareerSkillNames);
