using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class UpdateItemHandler(IAppDbContext db) : ICommandHandler<UpdateItemCommand, Unit>
{
    public async Task<Unit> Handle(UpdateItemCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        if (req.Quantity is < 1) throw new DomainRuleException("Количество должно быть не меньше 1.");

        if (req.State is not null) item.State = req.State.Value;
        if (req.Quantity is not null) item.Quantity = req.Quantity.Value;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
