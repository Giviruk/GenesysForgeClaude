using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomArchetypeHandler(IAppDbContext db) : ICommandHandler<DeleteCustomArchetypeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomArchetypeCommand command, CancellationToken ct = default)
    {
        var def = await db.ArchetypeDefs
                .Include(a => a.Abilities)
                .Include(a => a.StartingSkills)
                .FirstOrDefaultAsync(a => a.Id == command.ArchetypeId && a.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный архетип не найден.");

        if (await db.Characters.AnyAsync(c => c.ArchetypeId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить архетип: он используется персонажем.");

        db.ArchetypeDefs.Remove(def);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
