namespace GenesysForge.Application.Dtos;

/// <summary>Вход через Google: ID-токен, полученный фронтендом от Google Identity Services.</summary>
public record GoogleSignInRequest(string IdToken);
