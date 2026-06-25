using GenesysForge.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Реальная отправка писем через SMTP (MailKit). Универсально для любого relay: собственного сервера
/// или SMTP-режима Resend/SendGrid/Mailgun. Параметры — секция <c>Email</c> (<see cref="EmailOptions"/>),
/// базовый адрес ссылки — <c>App:BaseUrl</c>. Тело письма — собственный транзакционный текст.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    IConfiguration config,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;
    private string BaseUrl => (config["App:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

    public async Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default)
    {
        var link = $"{BaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Сброс пароля GenesysForge";
        message.Body = new TextPart("plain")
        {
            Text =
                "Вы запросили сброс пароля в GenesysForge.\n\n" +
                $"Чтобы задать новый пароль, перейдите по ссылке (действует ограниченное время):\n{link}\n\n" +
                "Если вы не запрашивали сброс — просто проигнорируйте это письмо, пароль не изменится.",
        };

        var smtp = _options.Smtp;
        using var client = new SmtpClient();
        var socketOptions = smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, ct);
        if (!string.IsNullOrEmpty(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation("Письмо сброса пароля отправлено на {Email} через SMTP {Host}.", email, smtp.Host);
    }
}
