using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class RemoveItemHandler(IAppDbContext db) : ICommandHandler<RemoveItemCommand, Unit>
{
    public async Task<Unit> Handle(RemoveItemCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var item = c.Items.FirstOrDefault(i => i.Id == command.ItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.");
        var itemName = item.ItemDef?.Name ?? "предмет";
        c.Items.Remove(item);
        db.CharacterItems.Remove(item);

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ItemRemoved,
            $"Удалён предмет «{itemName}»", null, new { item = itemName });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
