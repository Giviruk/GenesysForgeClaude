using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class RequestPasswordResetHandler(IAppDbContext db, IEmailSender email)
    : ICommandHandler<RequestPasswordResetCommand, Unit>
{
    /// <summary>Срок жизни ссылки сброса.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(1);

    public async Task<Unit> Handle(RequestPasswordResetCommand command, CancellationToken ct = default)
    {
        var email_ = (command.Request.Email ?? "").Trim().ToLowerInvariant();
        // Всегда отвечаем успехом и ничего не раскрываем: нет аккаунта — просто не шлём письмо.
        if (email_.Length == 0) return Unit.Value;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email_, ct);
        if (user is null) return Unit.Value;

        var now = DateTime.UtcNow;

        // Гасим прежние активные токены пользователя — действителен только самый свежий.
        var active = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.UsedAt = now;

        var raw = ResetTokens.NewRawToken();
        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = ResetTokens.Hash(raw),
            ExpiresAt = now.Add(Lifetime),
            CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        await email.SendPasswordResetAsync(user.Email, raw, ct);
        return Unit.Value;
    }
}
