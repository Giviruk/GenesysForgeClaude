using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Заглушка отправки писем для MVP: email-провайдер и домен отправителя ещё не выбраны
/// (см. docs/mvp-ux-account-readiness.md, пункт 4), поэтому ссылку подтверждения пишем в лог.
/// Базовый адрес — из App:BaseUrl.
/// </summary>
public class LoggingEmailSender(IConfiguration config, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendEmailConfirmationAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var link = $"{baseUrl}/confirm-email?confirmToken={Uri.EscapeDataString(rawToken)}";
        logger.LogWarning(
            "[STUB EMAIL] Подтверждение e-mail для {Email}. Ссылка (действует ограниченное время): {Link}",
            email, link);
        return Task.CompletedTask;
    }
}
