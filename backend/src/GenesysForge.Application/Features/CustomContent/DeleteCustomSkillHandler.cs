using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomSkillHandler(IAppDbContext db) : ICommandHandler<DeleteCustomSkillCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomSkillCommand command, CancellationToken ct = default)
    {
        var def = await db.SkillDefs.FirstOrDefaultAsync(
                s => s.Id == command.SkillDefId && s.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомный навык не найден.");

        if (await db.CharacterSkills.AnyAsync(s => s.SkillDefId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить навык: он используется персонажем. Сначала уберите ранги с листа.");

        db.SkillDefs.Remove(def);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
