using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class RemoveCriticalInjuryHandler(IAppDbContext db) : ICommandHandler<RemoveCriticalInjuryCommand, Unit>
{
    public async Task<Unit> Handle(RemoveCriticalInjuryCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var injury = c.CriticalInjuries.FirstOrDefault(ci => ci.Id == command.InjuryId)
            ?? throw new DomainRuleException("Крит-ранение не найдено.");
        c.CriticalInjuries.Remove(injury);
        db.CharacterCriticalInjuries.Remove(injury);

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
