using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomItemHandler(IAppDbContext db) : ICommandHandler<DeleteCustomItemCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomItemCommand command, CancellationToken ct = default)
    {
        var def = await db.ItemDefs.FirstOrDefaultAsync(
                i => i.Id == command.ItemDefId && i.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный предмет не найден.");

        if (await db.CharacterItems.AnyAsync(i => i.ItemDefId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить предмет: он в инвентаре персонажа. Сначала уберите его из инвентаря.");

        db.ItemDefs.Remove(def);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
