using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Features.Characters;

public class UpdateCharacterHandler(IAppDbContext db) : ICommandHandler<UpdateCharacterCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCharacterCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);

        if (req.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(req.Name))
                throw new DomainRuleException("Имя персонажа не может быть пустым.");
            c.Name = req.Name.Trim();
        }
        if (req.TotalXp is not null)
        {
            if (req.TotalXp < c.SpentXp)
                throw new DomainRuleException($"Суммарный XP не может быть меньше потраченного ({c.SpentXp}).");
            var oldTotal = c.TotalXp;
            c.TotalXp = req.TotalXp.Value;
            if (c.TotalXp != oldTotal)
            {
                var delta = c.TotalXp - oldTotal;
                CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.ManualEdit,
                    $"Изменён суммарный XP: {oldTotal}→{c.TotalXp}", delta,
                    new { from = oldTotal, to = c.TotalXp });
            }
        }
        if (req.WoundsCurrent is not null) c.WoundsCurrent = Math.Max(0, req.WoundsCurrent.Value);
        if (req.StrainCurrent is not null) c.StrainCurrent = Math.Max(0, req.StrainCurrent.Value);
        if (req.Money is not null) c.Money = Math.Max(0, req.Money.Value);

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
