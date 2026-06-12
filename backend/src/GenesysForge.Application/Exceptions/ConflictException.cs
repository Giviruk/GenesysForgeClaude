namespace GenesysForge.Application.Exceptions;

/// <summary>Конфликт уникальности (например, занятый e-mail) — транслируется в HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);
