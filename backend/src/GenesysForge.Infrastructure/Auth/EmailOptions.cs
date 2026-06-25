namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Настройки отправки транзакционных писем (секция <c>Email</c>). <see cref="Provider"/> выбирает
/// реализацию <see cref="GenesysForge.Application.Abstractions.IEmailSender"/>: <c>Smtp</c> — реальная
/// отправка через MailKit, иначе — заглушка <see cref="LoggingEmailSender"/> (ссылка в лог).
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Провайдер: <c>Smtp</c> или <c>Logging</c> (по умолчанию заглушка).</summary>
    public string Provider { get; set; } = "Logging";

    /// <summary>Адрес отправителя (From).</summary>
    public string From { get; set; } = "no-reply@genesysforge.local";

    /// <summary>Отображаемое имя отправителя.</summary>
    public string FromName { get; set; } = "GenesysForge";

    public SmtpOptions Smtp { get; set; } = new();

    public bool UsesSmtp => string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase);

    public sealed class SmtpOptions
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string? Username { get; set; }
        public string? Password { get; set; }
        /// <summary>Использовать STARTTLS (типично для порта 587). Для 465 (implicit TLS) выставьте false.</summary>
        public bool UseStartTls { get; set; } = true;
    }
}
