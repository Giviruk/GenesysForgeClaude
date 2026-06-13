using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomTalentHandler(IAppDbContext db) : ICommandHandler<DeleteCustomTalentCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomTalentCommand command, CancellationToken ct = default)
    {
        var def = await db.TalentDefs.FirstOrDefaultAsync(
                t => t.Id == command.TalentDefId && t.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный талант не найден.");

        if (await db.CharacterTalents.AnyAsync(t => t.TalentDefId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить талант: он куплен персонажем. Сначала верните его на листе.");

        db.TalentDefs.Remove(def);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
