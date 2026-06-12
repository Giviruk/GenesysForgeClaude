namespace GenesysForge.Application.Dtos;

public record CareerDto(Guid Id, string Name, string Description, List<string> CareerSkillNames);
