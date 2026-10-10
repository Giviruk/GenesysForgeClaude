using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomTalentHandler(IAppDbContext db) : ICommandHandler<UpdateCustomTalentCommand, TalentDefDto>
{
    public async Task<TalentDefDto> Handle(UpdateCustomTalentCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название таланта не может быть пустым.");
        if (req.Tier is < 1 or > GenesysRules.MaxTalentTier)
            throw new DomainRuleException("Тир таланта должен быть от 1 до 5.");

        var def = await db.TalentDefs.FirstOrDefaultAsync(
                t => t.Id == command.TalentDefId && t.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный талант не найден.");

        await ContentMembership.ApplyAsync(db, command.UserId, req.System, CustomEntryType.Talent, def.Id, req.PackIds, null, ct);
        var before = def.ToDto();

        def.System = req.System;
        def.Name = req.Name.Trim();
        def.Tier = req.Tier;
        def.IsRanked = req.IsRanked;
        def.Category = req.Category;
        def.Activation = string.IsNullOrWhiteSpace(req.Activation) ? "Пассивный" : req.Activation.Trim();
        def.Description = req.Description ?? "";
        def.WoundBonus = req.WoundBonus;
        def.StrainBonus = req.StrainBonus;
        def.SoakBonus = req.SoakBonus;
        def.MeleeDefenseBonus = req.MeleeDefenseBonus;
        def.RangedDefenseBonus = req.RangedDefenseBonus;
        CustomContentAudit.Updated(db, "talent", def.Id, def.HomebrewPackId, def.Name, command.UserId, before, def.ToDto());
        await CustomContentAudit.ReplicateToPacksAsync(db, def.Id, ct);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
