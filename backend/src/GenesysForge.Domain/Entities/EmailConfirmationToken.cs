namespace GenesysForge.Domain.Entities;

/// <summary>
/// Одноразовый токен подтверждения e-mail. В БД хранится только SHA-256 хеш токена;
/// сам токен уходит пользователю в письме (см. IEmailSender).
/// </summary>
public class EmailConfirmationToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Когда токен был использован/аннулирован; null — ещё активен.</summary>
    public DateTime? UsedAt { get; set; }
}
