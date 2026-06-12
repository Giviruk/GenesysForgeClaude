using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

public class RefundSkillRankHandler(IAppDbContext db) : ICommandHandler<RefundSkillRankCommand, Unit>
{
    public async Task<Unit> Handle(RefundSkillRankCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var row = c.Skills.FirstOrDefault(s => s.SkillDefId == command.SkillDefId)
            ?? throw new DomainRuleException("У навыка нет рангов для возврата.");

        var result = PurchaseValidator.RefundSkillRank(row.Ranks, row.FreeRanks, row.IsCareer, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        row.Ranks--;
        c.SpentXp -= result.Cost;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
