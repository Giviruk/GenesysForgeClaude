using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomHeroicAbilityHandler(IAppDbContext db)
    : ICommandHandler<UpdateCustomHeroicAbilityCommand, HeroicAbilityDto>
{
    public async Task<HeroicAbilityDto> Handle(UpdateCustomHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название способности не может быть пустым.");

        var def = await db.HeroicAbilityDefs.FirstOrDefaultAsync(
                h => h.Id == command.HeroicAbilityId && h.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомная героическая способность не найдена.");

        await ContentMembership.ApplyAsync(db, command.UserId, GameSystem.RealmsOfTerrinoth, CustomEntryType.HeroicAbility, def.Id, req.PackIds, null, ct);
        var before = def.ToDto();

        def.Name = req.Name.Trim();
        def.Description = req.Description ?? "";
        CustomContentAudit.Updated(db, "heroicAbility", def.Id, def.HomebrewPackId, def.Name, command.UserId, before, def.ToDto());
        await CustomContentAudit.ReplicateToPacksAsync(db, def.Id, ct);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
