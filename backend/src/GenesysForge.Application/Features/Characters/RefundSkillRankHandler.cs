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

        if (command.ExpectedRank is { } expectedRank && row.Ranks != expectedRank)
            throw new DomainRuleException("Эта запись истории больше не соответствует текущему рангу навыка.");

        var result = command.AllowAfterCreation
            ? PurchaseValidator.UndoSkillRank(row.Ranks, row.FreeRanks, row.IsCareer)
            : PurchaseValidator.RefundSkillRank(row.Ranks, row.FreeRanks, row.IsCareer, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        row.Ranks--;
        c.SpentXp -= result.Cost;

        var skillName = row.SkillDef?.Name ?? "навык";
        var summary = command.RevertedAuditId is null ? "Возврат" : "Откат покупки";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.SkillRankRefunded,
            $"{summary} ранга навыка «{skillName}» (→{row.Ranks})", result.Cost,
            new
            {
                skillDefId = row.SkillDefId, skill = skillName, rank = row.Ranks, cost = result.Cost,
                revertedAuditId = command.RevertedAuditId,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
