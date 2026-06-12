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
        var skillDef = await db.SkillDefs.FirstOrDefaultAsync(s =>
                s.Id == command.SkillDefId && s.System == c.System
                && (s.OwnerUserId == null || s.OwnerUserId == command.UserId), ct)
            ?? throw new DomainRuleException("Навык не найден.");

        var row = c.Skills.FirstOrDefault(s => s.SkillDefId == command.SkillDefId);
        var isCareer = row?.IsCareer ?? c.Career!.CareerSkillNames.Contains(skillDef.Name);
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
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
