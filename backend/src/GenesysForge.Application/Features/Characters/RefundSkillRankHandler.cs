using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

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

        var skillName = row.SkillDef?.Name ?? "навык";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.SkillRankRefunded,
            $"Возврат ранга навыка «{skillName}» (→{row.Ranks})", result.Cost,
            new { skill = skillName, rank = row.Ranks, cost = result.Cost });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
