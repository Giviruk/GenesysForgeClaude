using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Заглушка отправки писем для MVP: реальный email-провайдер и домен отправителя ещё не выбраны
/// (см. docs/mvp-ux-account-readiness.md, пункт 3), поэтому ссылку сброса пароля пишем в лог —
/// оператор передаёт её пользователю вручную. Базовый адрес берётся из App:BaseUrl.
/// </summary>
public class LoggingEmailSender(IConfiguration config, ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var baseUrl = (config["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var link = $"{baseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";
        logger.LogWarning(
            "[STUB EMAIL] Сброс пароля для {Email}. Ссылка восстановления (действует ограниченное время): {Link}",
            email, link);
        return Task.CompletedTask;
    }
}
