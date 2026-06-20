namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Отправка транзакционных писем. Провайдер и домен отправителя для MVP ещё не выбраны,
/// поэтому базовая реализация — заглушка, пишущая ссылку в лог (см. LoggingEmailSender).
/// </summary>
public interface IEmailSender
{
    /// <summary>Отправить письмо со ссылкой сброса пароля. <paramref name="rawToken"/> — исходный токен (не хеш).</summary>
    Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default);

    /// <summary>Письмо со ссылкой подтверждения e-mail. <paramref name="rawToken"/> — исходный токен (не хеш).</summary>
    Task SendEmailConfirmationAsync(string email, string rawToken, CancellationToken ct = default);
}
