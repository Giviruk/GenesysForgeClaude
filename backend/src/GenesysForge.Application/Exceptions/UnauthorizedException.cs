namespace GenesysForge.Application.Exceptions;

/// <summary>Неуспешная аутентификация — транслируется в HTTP 401.</summary>
public class UnauthorizedException(string message) : Exception(message);
