using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class ConfirmEmailHandler(IAppDbContext db) : ICommandHandler<ConfirmEmailCommand, Unit>
{
    public async Task<Unit> Handle(ConfirmEmailCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.Token))
            throw new DomainRuleException("Ссылка подтверждения недействительна.");

        var hash = EmailTokens.Hash(command.Request.Token.Trim());
        var now = DateTime.UtcNow;
        var token = await db.EmailConfirmationTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.UsedAt == null && t.ExpiresAt > now, ct);
        if (token is null)
            throw new DomainRuleException("Ссылка подтверждения недействительна или устарела.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct);
        if (user is null)
            throw new DomainRuleException("Ссылка подтверждения недействительна или устарела.");

        user.EmailConfirmed = true;

        // Гасим использованный и прочие активные токены пользователя.
        var active = await db.EmailConfirmationTokens
            .Where(t => t.UserId == user.Id && t.UsedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.UsedAt = now;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
