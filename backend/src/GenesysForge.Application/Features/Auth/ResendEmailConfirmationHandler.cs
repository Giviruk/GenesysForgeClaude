using GenesysForge.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class ResendEmailConfirmationHandler(IAppDbContext db, IEmailSender email)
    : ICommandHandler<ResendEmailConfirmationCommand, Unit>
{
    public async Task<Unit> Handle(ResendEmailConfirmationCommand command, CancellationToken ct = default)
    {
        var emailAddr = (command.Request.Email ?? "").Trim().ToLowerInvariant();
        // Всегда успех: не раскрываем наличие аккаунта; подтверждённым письмо не шлём.
        if (emailAddr.Length == 0) return Unit.Value;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == emailAddr, ct);
        if (user is null || user.EmailConfirmed) return Unit.Value;

        await EmailTokens.IssueAndSendAsync(db, email, user, ct);
        return Unit.Value;
    }
}
