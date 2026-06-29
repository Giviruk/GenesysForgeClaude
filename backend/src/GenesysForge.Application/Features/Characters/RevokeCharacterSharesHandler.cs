using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class RevokeCharacterSharesHandler(IAppDbContext db) : ICommandHandler<RevokeCharacterSharesCommand, Unit>
{
    public async Task<Unit> Handle(RevokeCharacterSharesCommand command, CancellationToken ct = default)
    {
        var character = await db.GetOwnedAsync(command.UserId, command.CharacterId, tracking: false, ct);
        var now = DateTime.UtcNow;
        var active = await db.CharacterShareTokens
            .Where(t => t.CharacterId == character.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in active) token.RevokedAt = now;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
