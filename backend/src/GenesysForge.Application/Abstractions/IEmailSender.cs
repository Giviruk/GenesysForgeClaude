namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Отправка транзакционных писем. Реализация выбирается по конфигу <c>Email:Provider</c>:
/// SmtpEmailSender (реальный SMTP, MailKit) или LoggingEmailSender (заглушка в лог для dev).
/// </summary>
public interface IEmailSender
{
    /// <summary>Отправить письмо со ссылкой сброса пароля. <paramref name="rawToken"/> — исходный токен (не хеш).</summary>
    Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default);
}
