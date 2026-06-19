namespace GenesysForge.Domain.Entities;

/// <summary>
/// Серверный refresh-токен. В БД хранится только SHA-256 хеш; сам токен живёт в HttpOnly-cookie.
/// Токены одной сессии связаны <see cref="FamilyId"/>; при ротации старый помечается
/// <see cref="RevokedAt"/> и ссылается на новый через <see cref="ReplacedByTokenId"/>.
/// Повторное предъявление уже отозванного токена считается компрометацией и гасит всё семейство.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    /// <summary>Идентификатор семейства (одна сессия/устройство).</summary>
    public Guid FamilyId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    /// <summary>Какой токен заменил этот при ротации (для аудита/детекта повтора).</summary>
    public Guid? ReplacedByTokenId { get; set; }
    public string? UserAgent { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => RevokedAt is null && DateTime.UtcNow < ExpiresAt;
}
