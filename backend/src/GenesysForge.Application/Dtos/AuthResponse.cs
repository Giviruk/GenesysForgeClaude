namespace GenesysForge.Application.Dtos;

public record AuthResponse(string Token, Guid UserId, string Email, string DisplayName);
