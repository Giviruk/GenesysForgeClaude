using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Заглушка отправки писем для MVP: email-провайдер и домен отправителя ещё не выбраны
/// (см. docs/mvp-ux-account-readiness.md, пункты 3–4), поэтому ссылки сброса пароля и
/// подтверждения e-mail пишем в лог. Базовый адрес — из App:BaseUrl.
/// </summary>
public class LoggingEmailSender(IConfiguration config, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    private string BaseUrl => (config["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    public Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var link = $"{BaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        logger.LogWarning(
            "[STUB EMAIL] Сброс пароля для {Email}. Ссылка восстановления (действует ограниченное время): {Link}",
            email, link);
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var link = $"{BaseUrl}/confirm-email?confirmToken={Uri.EscapeDataString(rawToken)}";
        logger.LogWarning(
            "[STUB EMAIL] Подтверждение e-mail для {Email}. Ссылка (действует ограниченное время): {Link}",
            email, link);
        return Task.CompletedTask;
    }
}
