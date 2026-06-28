namespace GenesysForge.Application.Dtos;

public record UpdateCharacterRequest(string? Name, int? TotalXp, int? WoundsCurrent, int? StrainCurrent, int? Money = null,
    string? Desire = null, string? Fear = null, string? Strength = null, string? Flaw = null, string? Background = null);
