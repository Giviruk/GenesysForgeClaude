using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class RevokeCharacterSharesHandler(IAppDbContext db) : ICommandHandler<RevokeCharacterSharesCommand, Unit>
{
    public async Task<Unit> Handle(RevokeCharacterSharesCommand command, CancellationToken ct = default)
    {
        await db.EnsureOwnedAsync(command.UserId, command.CharacterId, ct);
        var now = DateTime.UtcNow;
        var active = await db.CharacterShareTokens
            .Where(t => t.CharacterId == command.CharacterId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in active) token.RevokedAt = now;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
