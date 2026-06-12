namespace GenesysForge.Application.Dtos;

public record UpdateCharacterRequest(string? Name, int? TotalXp, int? WoundsCurrent, int? StrainCurrent);
