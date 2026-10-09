using GenesysForge.Application.Common;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomHeroicAbilityHandler(IAppDbContext db) : ICommandHandler<DeleteCustomHeroicAbilityCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var def = await db.HeroicAbilityDefs.FirstOrDefaultAsync(
                h => h.Id == command.HeroicAbilityId && h.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомная героическая способность не найдена.");

        if (await db.Characters.AnyAsync(c => c.HeroicAbilityId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить способность: она выбрана персонажем. Сначала смените её на листе.");

        db.HeroicAbilityDefs.Remove(def);
        CustomContentAudit.Deleted(db, "heroicAbility", def.Id, def.HomebrewPackId, def.Name, command.UserId);
        await ContentMembership.RemoveDefinitionAsync(db, CustomEntryType.HeroicAbility, def.Id, ct);
        await CustomContentAudit.ReplicateToPacksAsync(db, def.Id, ct);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
