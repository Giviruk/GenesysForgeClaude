namespace GenesysForge.Domain;

/// <summary>Нарушение правил игры или валидации — транслируется в HTTP 400.</summary>
public class DomainRuleException(string message) : Exception(message);
