using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public class BuyTalentHandler(IAppDbContext db) : ICommandHandler<BuyTalentCommand, Unit>
{
    public async Task<Unit> Handle(BuyTalentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, command.UserId, c.System, command.CharacterId, ct: ct);
        var talentDef = await db.TalentDefs.FirstOrDefaultAsync(t =>
                t.Id == command.TalentDefId && t.System == c.System
                && (t.OwnerUserId == null
                    || (t.OwnerUserId == command.UserId
                        && (t.HomebrewPackId == null || visiblePackIds.Contains(t.HomebrewPackId.Value)))), ct)
            ?? throw new DomainRuleException("Талант не найден.");

        var row = c.Talents.FirstOrDefault(t => t.TalentDefId == command.TalentDefId);
        var result = PurchaseValidator.BuyTalent(
            talentDef.Tier,
            row?.Ranks ?? 0,
            talentDef.IsRanked,
            TalentTierCounter.Count(c.Talents),
            c.AvailableXp);
        if (!result.Allowed) throw new DomainRuleException(result.Error!);

        // Таланты вида Dedication увеличивают выбранную характеристику на 1 за ранг.
        CharacteristicType? grant = null;
        if (talentDef.GrantsCharacteristic)
        {
            if (command.Characteristic is not { } chosen)
                throw new DomainRuleException("Для этого таланта нужно выбрать характеристику для увеличения.");
            if ((row?.ParseGrants() ?? []).Contains(chosen))
                throw new DomainRuleException("Этим талантом нельзя дважды увеличить одну и ту же характеристику.");
            if (c.GetCharacteristic(chosen) >= GenesysRules.MaxCharacteristicAtCreation)
                throw new DomainRuleException(
                    $"Талант не может увеличить характеристику выше {GenesysRules.MaxCharacteristicAtCreation}.");
            grant = chosen;
        }

        if (row is null)
        {
            row = new CharacterTalent
            {
                Id = Guid.NewGuid(), CharacterId = c.Id, TalentDefId = command.TalentDefId,
                TalentDef = talentDef, Ranks = 0,
            };
            db.CharacterTalents.Add(row);
            c.Talents.Add(row);
        }
        row.Ranks++;
        if (grant is { } g)
        {
            c.IncreaseCharacteristic(g);
            row.SetGrants([.. row.ParseGrants(), g]);
        }
        c.SpentXp += result.Cost;

        var grantNote = grant is { } gc ? $" (+1 к «{CharacterAudit.CharacteristicLabel(gc)}»)" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.TalentBought,
            $"Куплен талант «{talentDef.Name}» (→{row.Ranks}){grantNote}", -result.Cost,
            new { talent = talentDef.Name, rank = row.Ranks, cost = result.Cost, grant = grant?.ToString() });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
