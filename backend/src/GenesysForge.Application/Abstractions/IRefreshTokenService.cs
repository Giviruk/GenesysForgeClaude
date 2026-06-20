namespace GenesysForge.Application.Abstractions;

/// <summary>Метаданные запроса для аудита refresh-токенов.</summary>
public record RequestMeta(string? UserAgent, string? Ip);

/// <summary>Результат ротации: новый access JWT + новый refresh-токен (raw) и его срок.</summary>
public record RefreshRotation(
    string AccessToken, string RawRefreshToken, DateTime RefreshExpiresAt,
    Guid UserId, string Email, string DisplayName);

/// <summary>
/// Управление серверными refresh-токенами: выпуск (на входе), ротация (на обновлении),
/// отзыв семейства (на выходе). Ротация каждый раз и детект повторного использования.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>Выпустить новый refresh-токен (новое семейство) для пользователя.</summary>
    Task<(string RawToken, DateTime ExpiresAt)> IssueAsync(Guid userId, RequestMeta meta, CancellationToken ct = default);

    /// <summary>
    /// Ротация: проверить refresh-токен, погасить старый и выпустить новый в том же семействе,
    /// выдать новый access JWT. Повторное предъявление отозванного токена гасит всё семейство.
    /// </summary>
    Task<RefreshRotation> RotateAsync(string rawRefresh, RequestMeta meta, CancellationToken ct = default);

    /// <summary>Отозвать семейство текущего refresh-токена (logout). Идемпотентно.</summary>
    Task RevokeFamilyAsync(string rawRefresh, CancellationToken ct = default);
}
