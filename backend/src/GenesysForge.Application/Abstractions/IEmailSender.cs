namespace GenesysForge.Application.Abstractions;

/// <summary>
/// Отправка транзакционных писем. Реализация выбирается по конфигу <c>Email:Provider</c>:
/// SmtpEmailSender (реальный SMTP, MailKit) или LoggingEmailSender (заглушка в лог для dev).
/// </summary>
public interface IEmailSender
{
    /// <summary>Отправить письмо со ссылкой сброса пароля. <paramref name="rawToken"/> — исходный токен (не хеш).</summary>
    Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default);

    /// <summary>Переслать сообщение из формы обратной связи на адрес поддержки.</summary>
    Task SendFeedbackAsync(FeedbackMessage feedback, CancellationToken ct = default);
}

/// <summary>Сообщение формы обратной связи. <c>ReplyTo</c> — адрес для ответа, если он известен.</summary>
public record FeedbackMessage(string Text, string? ReplyTo, string? AccountEmail, string? Page);
