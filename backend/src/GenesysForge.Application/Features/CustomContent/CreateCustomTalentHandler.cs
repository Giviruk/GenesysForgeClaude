using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomTalentHandler(IAppDbContext db) : ICommandHandler<CreateCustomTalentCommand, TalentDefDto>
{
    public async Task<TalentDefDto> Handle(CreateCustomTalentCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var packId = await CampaignCustomContent.GetOrCreatePackIdAsync(
            db, command.CampaignId, command.UserId, req.System, ct);
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название таланта не может быть пустым.");
        if (req.Tier is < 1 or > GenesysRules.MaxTalentTier)
            throw new DomainRuleException("Тир таланта должен быть от 1 до 5.");

        var def = new TalentDef
        {
            Id = Guid.NewGuid(), System = req.System, Name = req.Name.Trim(), Tier = req.Tier,
            IsRanked = req.IsRanked,
            Category = req.Category,
            Activation = string.IsNullOrWhiteSpace(req.Activation) ? "Пассивный" : req.Activation.Trim(),
            Description = req.Description ?? "",
            WoundBonus = req.WoundBonus, StrainBonus = req.StrainBonus, SoakBonus = req.SoakBonus,
            MeleeDefenseBonus = req.MeleeDefenseBonus, RangedDefenseBonus = req.RangedDefenseBonus,
            OwnerUserId = command.UserId,
            HomebrewPackId = packId,
        };
        db.TalentDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
