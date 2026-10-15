namespace GenesysForge.Application.Exceptions;

/// <summary>Конфликт уникальности или конкурентного изменения — транслируется в HTTP 409.</summary>
public class ConflictException(string message) : Exception(message);
