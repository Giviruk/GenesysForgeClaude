namespace GenesysForge.Application.Dtos;

/// <summary>Доступные внешние провайдеры входа. <c>GoogleClientId</c> null — Google не настроен.</summary>
public record AuthProvidersResponse(string? GoogleClientId);
