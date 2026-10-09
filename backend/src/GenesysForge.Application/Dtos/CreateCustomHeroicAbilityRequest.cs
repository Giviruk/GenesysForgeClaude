namespace GenesysForge.Application.Dtos;

public record CreateCustomHeroicAbilityRequest(string Name, string Description,
    IReadOnlyList<Guid>? PackIds = null);
