namespace GenesysForge.Application.Abstractions;

/// <summary>Проверенные данные внешней личности (после валидации ID-токена провайдера).</summary>
public record ExternalIdentityInfo(string ProviderUserId, string Email, bool EmailVerified, string? Name);

/// <summary>
/// Валидация ID-токенов внешних провайдеров входа. Реализация в Infrastructure;
/// при отсутствии конфигурации провайдера бросает доменную ошибку («не настроено»).
/// </summary>
public interface IExternalIdentityValidator
{
    /// <summary>Провайдер настроен (есть client id) — можно показывать кнопку входа.</summary>
    bool GoogleConfigured { get; }

    /// <summary>Проверить Google ID-токен и вернуть подтверждённые данные пользователя.</summary>
    Task<ExternalIdentityInfo> ValidateGoogleAsync(string idToken, CancellationToken ct = default);
}
