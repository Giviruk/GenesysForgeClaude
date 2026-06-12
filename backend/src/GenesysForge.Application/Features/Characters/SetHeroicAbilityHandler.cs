using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class SetHeroicAbilityHandler(IAppDbContext db) : ICommandHandler<SetHeroicAbilityCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException("Героические способности доступны только в Realms of Terrinoth.");

        if (command.HeroicAbilityId is not null)
        {
            var exists = await db.HeroicAbilityDefs.AnyAsync(h =>
                h.Id == command.HeroicAbilityId
                && (h.OwnerUserId == null || h.OwnerUserId == command.UserId), ct);
            if (!exists) throw new DomainRuleException("Героическая способность не найдена.");
        }

        c.HeroicAbilityId = command.HeroicAbilityId;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
