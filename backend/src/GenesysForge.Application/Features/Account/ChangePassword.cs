using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Account;

public record ChangePasswordCommand(Guid UserId, ChangePasswordRequest Request) : ICommand<Unit>;

public class ChangePasswordHandler(IAppDbContext db, IPasswordHasherService hasher)
    : ICommandHandler<ChangePasswordCommand, Unit>
{
    public async Task<Unit> Handle(ChangePasswordCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if ((req.NewPassword?.Length ?? 0) < 6)
            throw new DomainRuleException("Пароль должен быть не короче 6 символов.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == command.UserId, ct)
            ?? throw new DomainRuleException("Пользователь не найден.");

        if (string.IsNullOrEmpty(req.CurrentPassword) || !hasher.Verify(user, user.PasswordHash, req.CurrentPassword))
            throw new DomainRuleException("Текущий пароль указан неверно.");

        user.PasswordHash = hasher.Hash(user, req.NewPassword!);

        // Смена пароля разлогинивает все сессии: отзываем активные refresh-токены пользователя
        // (текущему устройству эндпоинт выдаст свежий cookie, чтобы не разлогинить себя).
        var now = DateTime.UtcNow;
        var sessions = await db.RefreshTokens
            .Where(r => r.UserId == user.Id && r.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var r in sessions) r.RevokedAt = now;

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
