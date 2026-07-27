namespace GenesysForge.Domain;

/// <summary>Нарушение правил игры или валидации — транслируется в HTTP 400.</summary>
/// <param name="message">Человекочитаемое сообщение для UI.</param>
/// <param name="reasonCode">
/// Стабильный машинный код причины (напр. <c>career.package.group_missing</c>). Клиент обязан
/// ветвиться по нему, а не по тексту сообщения. <c>null</c> — код причины ещё не назначен.
/// </param>
public class DomainRuleException(string message, string? reasonCode = null) : Exception(message)
{
    public string? ReasonCode { get; } = reasonCode;
}
