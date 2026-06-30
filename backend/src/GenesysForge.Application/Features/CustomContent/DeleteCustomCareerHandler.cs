using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.CustomContent;

public class DeleteCustomCareerHandler(IAppDbContext db) : ICommandHandler<DeleteCustomCareerCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomCareerCommand command, CancellationToken ct = default)
    {
        var def = await db.CareerDefs
                .Include(c => c.StartingGear)
                .Include(c => c.Rules)
                .FirstOrDefaultAsync(c => c.Id == command.CareerId && c.OwnerUserId == command.UserId, ct)
            ?? throw new DomainRuleException("Кастомная карьера не найдена.");

        if (await db.Characters.AnyAsync(c => c.CareerId == def.Id, ct))
            throw new DomainRuleException("Нельзя удалить карьеру: она используется персонажем.");

        db.CareerDefs.Remove(def);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
