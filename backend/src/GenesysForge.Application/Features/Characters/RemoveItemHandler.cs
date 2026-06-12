using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class RemoveItemHandler(IAppDbContext db) : ICommandHandler<RemoveItemCommand, Unit>
{
    public async Task<Unit> Handle(RemoveItemCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        c.Items.Remove(item);
        db.CharacterItems.Remove(item);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
