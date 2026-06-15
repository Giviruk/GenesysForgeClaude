using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class AddItemHandler(IAppDbContext db) : ICommandHandler<AddItemCommand, Guid>
{
    public async Task<Guid> Handle(AddItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var itemDef = await db.ItemDefs.FirstOrDefaultAsync(i =>
                i.Id == req.ItemDefId && i.System == c.System
                && (i.OwnerUserId == null || i.OwnerUserId == command.UserId), ct)
            ?? throw new DomainRuleException("Предмет не найден.");
        if (req.Quantity < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");

        // Покупка: списываем монеты. Cost == null/≤0 — бесплатное добавление.
        if (req.Cost is > 0)
        {
            if (c.Money < req.Cost.Value)
                throw new DomainRuleException($"Недостаточно монет: нужно {req.Cost.Value}, в наличии {c.Money}.");
            c.Money -= req.Cost.Value;
        }

        var item = new CharacterItem
        {
            Id = Guid.NewGuid(), CharacterId = c.Id, ItemDefId = itemDef.Id, ItemDef = itemDef,
            Quantity = req.Quantity, State = req.State,
        };
        db.CharacterItems.Add(item);
        c.Items.Add(item);
        await db.SaveChangesAsync(ct);
        return item.Id;
    }
}
