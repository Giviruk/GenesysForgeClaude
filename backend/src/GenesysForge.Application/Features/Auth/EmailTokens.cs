using System.Security.Cryptography;
using System.Text;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

/// <summary>Генерация, хеширование и выпуск одноразовых токенов подтверждения e-mail.</summary>
internal static class EmailTokens
{
    /// <summary>Срок жизни ссылки подтверждения.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(7);

    /// <summary>Криптослучайный токен (URL-safe base64, 256 бит энтропии).</summary>
    public static string NewRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>SHA-256 хеш токена (хранится в БД; hex, 64 символа).</summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    /// <summary>
    /// Гасит прежние активные токены пользователя, выпускает новый и шлёт письмо со ссылкой.
    /// Изменения сохраняются в БД вызывающим (тут только Add/мутации), затем отправка письма.
    /// </summary>
    public static async Task<string> IssueAndSendAsync(
        IAppDbContext db, IEmailSender email, User user, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var active = await db.EmailConfirmationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.UsedAt = now;

        var raw = NewRawToken();
        db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = Hash(raw),
            ExpiresAt = now.Add(Lifetime),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        await email.SendEmailConfirmationAsync(user.Email, raw, ct);
        return raw;
    }
}
