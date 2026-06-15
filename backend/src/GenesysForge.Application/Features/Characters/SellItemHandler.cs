using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class SellItemHandler(IAppDbContext db) : ICommandHandler<SellItemCommand, Unit>
{
    public async Task<Unit> Handle(SellItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");

        if (req.Quantity < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");
        if (req.Quantity > item.Quantity)
            throw new DomainRuleException($"Нельзя продать больше, чем есть ({item.Quantity}).");
        if (req.Proceeds < 0) throw new DomainRuleException("Выручка не может быть отрицательной.");

        c.Money += req.Proceeds;

        if (req.Quantity == item.Quantity)
        {
            c.Items.Remove(item);
            db.CharacterItems.Remove(item);
        }
        else
        {
            item.Quantity -= req.Quantity;
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
