namespace GenesysForge.Domain.Entities;

/// <summary>
/// Привязка внешнего провайдера входа (например, Google) к локальному пользователю.
/// Уникальность по паре (Provider, ProviderUserId).
/// </summary>
public class ExternalAuthIdentity
{
    public Guid Id { get; set; }
    /// <summary>Идентификатор провайдера, напр. "google".</summary>
    public required string Provider { get; set; }
    /// <summary>Стабильный id пользователя у провайдера (для Google — claim "sub").</summary>
    public required string ProviderUserId { get; set; }
    public Guid UserId { get; set; }
    /// <summary>Подтверждённый провайдером e-mail на момент привязки.</summary>
    public required string Email { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
