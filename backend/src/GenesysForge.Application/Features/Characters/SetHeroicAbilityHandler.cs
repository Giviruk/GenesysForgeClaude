using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class SetHeroicAbilityHandler(IAppDbContext db) : ICommandHandler<SetHeroicAbilityCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException("Героические способности доступны только в Realms of Terrinoth.");

        string? abilityName = null;
        if (command.HeroicAbilityId is not null)
        {
            var ability = await db.HeroicAbilityDefs.FirstOrDefaultAsync(h =>
                h.Id == command.HeroicAbilityId
                && (h.OwnerUserId == null || h.OwnerUserId == command.UserId), ct);
            if (ability is null) throw new DomainRuleException("Героическая способность не найдена.");
            abilityName = ability.Name;
        }

        if (c.HeroicAbilityId == command.HeroicAbilityId) return Unit.Value; // без изменений — не логируем

        // Улучшения привязаны к конкретной способности — при смене/сбросе откатываем купленный ранг.
        c.HeroicUpgradeRank = 0;
        c.HeroicAbilityId = command.HeroicAbilityId;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.HeroicAbilityChanged,
            abilityName is null ? "Героическая способность сброшена" : $"Героическая способность: «{abilityName}»",
            null, new { ability = abilityName });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
