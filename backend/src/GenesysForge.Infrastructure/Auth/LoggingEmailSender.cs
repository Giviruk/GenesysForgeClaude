using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Заглушка отправки писем (dev/тесты): реальная отправка не выполняется, ссылку сброса
/// пароля пишем в лог. Активна, когда <c>Email:Provider</c> ≠ <c>Smtp</c>. Для реальной
/// отправки используется <see cref="SmtpEmailSender"/>. Базовый адрес — из App:BaseUrl.
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
}
