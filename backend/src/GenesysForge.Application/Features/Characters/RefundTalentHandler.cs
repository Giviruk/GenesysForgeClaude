using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

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
        if (!result.Allowed) throw new DomainRuleException(result.Error!, TalentPurchasePolicy.ReasonPyramidOrXp);

        // Последний ранг нельзя вернуть, пока он остаётся основанием уже купленного таланта:
        // иначе персонаж окажется в состоянии, которое сам не смог бы купить.
        var dependencyError = TalentPurchasePolicy.ValidateRefund(
            row.TalentDef, row.Ranks - 1,
            c.Talents.Where(t => t.TalentDef is not null).Select(t => t.TalentDef!));
        if (dependencyError is not null)
            throw new DomainRuleException(dependencyError.Message, dependencyError.ReasonCode);

        row.Ranks--;

        // Выбор последнего ранга снимается вместе с ним — иначе повторная покупка увидела бы
        // «уже выбранное» значение, которого персонаж больше не имеет (ROT-TAL-03).
        foreach (var choice in row.Choices.Where(x => x.RankIndex == row.Ranks).ToList())
        {
            row.Choices.Remove(choice);
            db.CharacterTalentChoices.Remove(choice);
        }

        // Откатываем увеличение характеристики, выданное последним рангом (Dedication).
        if (row.TalentDef.GrantsCharacteristic)
        {
            var grants = row.ParseGrants();
            if (grants.Count > 0)
            {
                var last = grants[^1];
                grants.RemoveAt(grants.Count - 1);
                c.DecreaseCharacteristic(last);
                row.SetGrants(grants);
            }
        }
        if (row.Ranks == 0)
        {
            c.Talents.Remove(row);
            db.CharacterTalents.Remove(row);
        }
        c.SpentXp -= result.Cost;

        var talentName = row.TalentDef!.Name;
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.TalentRefunded,
            $"Возврат таланта «{talentName}» (→{row.Ranks})", result.Cost,
            new { talent = talentName, rank = row.Ranks, cost = result.Cost });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
