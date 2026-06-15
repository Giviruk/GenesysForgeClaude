using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.CustomContent;

public class CreateCustomItemHandler(IAppDbContext db) : ICommandHandler<CreateCustomItemCommand, ItemDefDto>
{
    public async Task<ItemDefDto> Handle(CreateCustomItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name))
            throw new DomainRuleException("Название предмета не может быть пустым.");
        if (req.Encumbrance < 0)
            throw new DomainRuleException("Вес предмета не может быть отрицательным.");

        var def = new ItemDef
        {
            Id = Guid.NewGuid(), System = req.System, Name = req.Name.Trim(), Kind = req.Kind,
            Encumbrance = req.Encumbrance, SoakBonus = req.SoakBonus, MeleeDefense = req.MeleeDefense,
            RangedDefense = req.RangedDefense, EncumbranceThresholdBonus = req.EncumbranceThresholdBonus,
            Description = req.Description ?? "", Price = req.Price, Rarity = req.Rarity,
            SkillName = req.SkillName ?? "", Damage = req.Damage ?? "", Crit = req.Crit ?? "",
            RangeBand = req.RangeBand ?? "", Properties = req.Properties ?? "",
            OwnerUserId = command.UserId,
        };
        db.ItemDefs.Add(def);
        await db.SaveChangesAsync(ct);
        return def.ToDto();
    }
}
