using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class RefundTalentHandler(IAppDbContext db) : ICommandHandler<RefundTalentCommand, Unit>
{
    public async Task<Unit> Handle(RefundTalentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var row = c.Talents.FirstOrDefault(t => t.TalentDefId == command.TalentDefId)
            ?? throw new DomainRuleException("Этот талант не куплен.");

        var result = PurchaseValidator.RefundTalent(
            row.TalentDef!.Tier,
            row.Ranks,
            TalentTierCounter.Count(c.Talents),
            c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        row.Ranks--;
        if (row.Ranks == 0)
        {
            c.Talents.Remove(row);
            db.CharacterTalents.Remove(row);
        }
        c.SpentXp -= result.Cost;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
