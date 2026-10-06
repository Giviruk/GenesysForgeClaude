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

        await SendAsync(message, ct);
        logger.LogInformation("Письмо сброса пароля отправлено на {Email} через SMTP {Host}.", email, _options.Smtp.Host);
    }

    public async Task SendFeedbackAsync(FeedbackMessage feedback, CancellationToken ct = default)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.From));
        message.To.Add(MailboxAddress.Parse(_options.FeedbackTo));
        if (feedback.ReplyTo is { } replyTo) message.ReplyTo.Add(MailboxAddress.Parse(replyTo));
        message.Subject = $"Обратная связь GenesysForge — {new Uri(BaseUrl).Host}";
        message.Body = new TextPart("plain") { Text = FeedbackText.Format(feedback, BaseUrl) };

        await SendAsync(message, ct);
        logger.LogInformation("Обратная связь переслана на {To} через SMTP {Host}.", _options.FeedbackTo, _options.Smtp.Host);
    }

    private async Task SendAsync(MimeMessage message, CancellationToken ct)
    {
        var smtp = _options.Smtp;
        using var client = new SmtpClient();
        var socketOptions = smtp.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.SslOnConnect;
        await client.ConnectAsync(smtp.Host, smtp.Port, socketOptions, ct);
        if (!string.IsNullOrEmpty(smtp.Username))
            await client.AuthenticateAsync(smtp.Username, smtp.Password ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}

/// <summary>Текст письма обратной связи: само сообщение и откуда оно пришло.</summary>
public static class FeedbackText
{
    public static string Format(FeedbackMessage feedback, string baseUrl) =>
        feedback.Text + "\n\n---\n" +
        $"Сайт: {baseUrl}\n" +
        $"Страница: {feedback.Page ?? "—"}\n" +
        $"Аккаунт: {feedback.AccountEmail ?? "не выполнен вход"}\n" +
        $"Ответить на: {feedback.ReplyTo ?? "адрес не указан"}\n" +
        $"Время (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
}
