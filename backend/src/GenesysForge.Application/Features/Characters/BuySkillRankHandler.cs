using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class BuySkillRankHandler(IAppDbContext db) : ICommandHandler<BuySkillRankCommand, Unit>
{
    public async Task<Unit> Handle(BuySkillRankCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, c.System, command.CharacterId, ct: ct);
        var skillDef = await db.SkillDefs.FirstOrDefaultAsync(s =>
                s.Id == command.SkillDefId && s.System == c.System
                && (s.OwnerUserId == null
                    || (s.OwnerUserId == command.UserId
                        && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Навык не найден.");

        // Карьерный статус берётся из резолвера на момент покупки: талант, выдавший навык,
        // делает следующий ранг дешевле, но ранее уплаченную надбавку не возвращает.
        var systemSkills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == c.System
                && (s.OwnerUserId == null
                    || (s.OwnerUserId == command.UserId
                        && (s.HomebrewPackId == null || visiblePackIds.Contains(s.HomebrewPackId.Value)))))
            .ToListAsync(ct);
        var careerSkills = CareerSkills.Resolve(c, c.Career!, systemSkills);

        var row = c.Skills.FirstOrDefault(s => s.SkillDefId == command.SkillDefId);
        var isCareer = careerSkills.IsCareer(command.SkillDefId);
        var currentRank = row?.Ranks ?? 0;

        var result = PurchaseValidator.BuySkillRank(currentRank, isCareer, c.AvailableXp, c.IsCreationPhase);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        if (row is null)
        {
            row = new CharacterSkill
            {
                Id = Guid.NewGuid(), CharacterId = c.Id, SkillDefId = command.SkillDefId, IsCareer = isCareer,
            };
            db.CharacterSkills.Add(row); // явный Add: через навигацию EF счёл бы ключ существующим
            c.Skills.Add(row);
        }
        row.Ranks++;
        c.SpentXp += result.Cost;

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.SkillRankBought,
            $"Куплен ранг навыка «{skillDef.Name}» (→{row.Ranks})", -result.Cost,
            new { skill = skillDef.Name, rank = row.Ranks, cost = result.Cost });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
