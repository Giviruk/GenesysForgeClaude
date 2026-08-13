using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomHeroicAbilityHandler(IAppDbContext db)
    : ICommandHandler<CreateCustomHeroicAbilityCommand, HeroicAbilityDto>
{
    public async Task<HeroicAbilityDto> Handle(CreateCustomHeroicAbilityCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var packId = await CampaignCustomContent.GetOrCreatePackIdAsync(
            db, command.CampaignId, command.UserId, GameSystem.RealmsOfTerrinoth, ct);
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название способности не может быть пустым.");

        var def = new HeroicAbilityDef
        {
            Id = Guid.NewGuid(), Name = req.Name.Trim(), Description = req.Description ?? "",
            OwnerUserId = command.UserId,
            HomebrewPackId = packId,
        };
        db.HeroicAbilityDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
