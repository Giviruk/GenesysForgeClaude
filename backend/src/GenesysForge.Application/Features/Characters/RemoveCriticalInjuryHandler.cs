using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class RemoveCriticalInjuryHandler(IAppDbContext db) : ICommandHandler<RemoveCriticalInjuryCommand, Unit>
{
    public async Task<Unit> Handle(RemoveCriticalInjuryCommand command, CancellationToken ct = default)
    {
        await db.EnsureOwnedAsync(command.UserId, command.CharacterId, ct);
        var injury = await db.CharacterCriticalInjuries
            .FirstOrDefaultAsync(ci => ci.Id == command.InjuryId && ci.CharacterId == command.CharacterId, ct)
            ?? throw new DomainRuleException("Крит-ранение не найдено.");
        db.CharacterCriticalInjuries.Remove(injury);

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
