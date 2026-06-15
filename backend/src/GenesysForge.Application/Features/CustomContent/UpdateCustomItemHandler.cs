using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class UpdateCustomItemHandler(IAppDbContext db) : ICommandHandler<UpdateCustomItemCommand, ItemDefDto>
{
    public async Task<ItemDefDto> Handle(UpdateCustomItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название предмета не может быть пустым.");
        if (req.Encumbrance < 0)
            throw new DomainRuleException("Вес предмета не может быть отрицательным.");

        var def = await db.ItemDefs.FirstOrDefaultAsync(
                i => i.Id == command.ItemDefId && i.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный предмет не найден.");

        def.System = req.System;
        def.Name = req.Name.Trim();
        def.Kind = req.Kind;
        def.Encumbrance = req.Encumbrance;
        def.SoakBonus = req.SoakBonus;
        def.MeleeDefense = req.MeleeDefense;
        def.RangedDefense = req.RangedDefense;
        def.EncumbranceThresholdBonus = req.EncumbranceThresholdBonus;
        def.Description = req.Description ?? "";
        def.Price = req.Price;
        def.Rarity = req.Rarity;
        def.SkillName = req.SkillName ?? "";
        def.Damage = req.Damage ?? "";
        def.Crit = req.Crit ?? "";
        def.RangeBand = req.RangeBand ?? "";
        def.Properties = req.Properties ?? "";
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
