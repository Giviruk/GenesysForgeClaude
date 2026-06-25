using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class ConfirmPasswordResetHandler(IAppDbContext db, IPasswordHasherService hasher)
    : ICommandHandler<ConfirmPasswordResetCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmPasswordResetCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Token))
            throw new DomainRuleException("Ссылка восстановления недействительна.");
        if ((req.NewPassword?.Length ?? 0) < 6)
            throw new DomainRuleException("Пароль должен быть не короче 6 символов.");

        var hash = ResetTokens.Hash(req.Token.Trim());
        var now = DateTime.UtcNow;
        var token = await db.PasswordResetTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now, ct);
        if (token is null)
            throw new DomainRuleException("Ссылка восстановления недействительна или устарела.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null)
            throw new DomainRuleException("Ссылка восстановления недействительна или устарела.");

        user.PasswordHash = hasher.Hash(user, req.NewPassword!);

        // Гасим использованный и все прочие активные токены пользователя (одноразовость).
        var active = await db.PasswordResetTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.UsedAt = now;

        // Смена пароля разлогинивает все сессии: отзываем активные refresh-токены пользователя.
        var sessions = await db.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var r in sessions) r.RevokedAt = now;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
