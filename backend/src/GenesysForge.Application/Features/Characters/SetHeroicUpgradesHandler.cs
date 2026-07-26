using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>Покупает или, во время создания, возвращает универсальные улучшения Heroic Ability.</summary>
public class SetHeroicUpgradesHandler(IAppDbContext db) : ICommandHandler<SetHeroicUpgradesCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicUpgradesCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException("Героические способности доступны только в Realms of Terrinoth.");
        if (c.HeroicAbility is null)
            throw new DomainRuleException("Сначала выберите героическую способность.");
        HeroicIdentityGate.EnsureUpgradesAllowed(c);

        var req = command.Request;
        if (req.PowerRank is < 0 or > 2 || req.DurationRanks < 0 || req.FrequencyRanks < 0)
            throw new DomainRuleException("Ранги улучшений не могут быть отрицательными, Power допускает ранги 0–2.");
        if (req.PowerRank > c.HeroicAbility.Upgrades.Count)
            throw new DomainRuleException("Для выбранной способности нет указанного улучшения Power.");

        var requestedIds = (req.SecondaryEffectIds ?? []).Distinct().ToList();
        if (requestedIds.Count > 2)
            throw new DomainRuleException("Можно выбрать не более двух разных вторичных эффектов.");
        if (requestedIds.Count != (req.SecondaryEffectIds?.Count ?? 0))
            throw new DomainRuleException("Нельзя выбрать один вторичный эффект дважды.");

        var effects = await db.HeroicSecondaryEffectDefs
            .Where(x => requestedIds.Contains(x.Id)).ToListAsync(ct);
        if (effects.Count != requestedIds.Count)
            throw new DomainRuleException("Один из вторичных эффектов не найден.");

        var powerCost = c.HeroicAbility.Upgrades
            .Where(u => (int)u.Level <= req.PowerRank).Sum(u => u.Cost);
        var targetCost = (long)powerCost + req.DurationRanks + (long)req.FrequencyRanks * 2
            + (req.Story ? 1 : 0) + requestedIds.Count;
        if (targetCost > c.HeroicUpgradePointsTotal)
            throw new DomainRuleException(
                $"Недостаточно ability points: нужно {targetCost}, доступно {c.HeroicUpgradePointsTotal}.");

        var currentIds = c.HeroicSecondaryEffects.Select(x => x.HeroicSecondaryEffectDefId).ToHashSet();
        var removesPurchase = req.PowerRank < c.HeroicUpgradeRank
            || req.DurationRanks < c.HeroicDurationRanks
            || req.FrequencyRanks < c.HeroicFrequencyRanks
            || (c.HeroicStoryUpgrade && !req.Story)
            || !currentIds.IsSubsetOf(requestedIds);
        if (removesPurchase && !c.IsCreationPhase)
            throw new DomainRuleException("После завершения создания улучшения героической способности постоянны.");

        c.HeroicUpgradeRank = req.PowerRank;
        c.HeroicDurationRanks = req.DurationRanks;
        c.HeroicFrequencyRanks = req.FrequencyRanks;
        c.HeroicStoryUpgrade = req.Story;

        var requestedSet = requestedIds.ToHashSet();
        var removed = c.HeroicSecondaryEffects
            .Where(x => !requestedSet.Contains(x.HeroicSecondaryEffectDefId)).ToList();
        db.CharacterHeroicSecondaryEffects.RemoveRange(removed);
        foreach (var effectId in requestedIds.Where(id => !currentIds.Contains(id)))
        {
            db.CharacterHeroicSecondaryEffects.Add(new CharacterHeroicSecondaryEffect
            {
                Id = Guid.NewGuid(),
                CharacterId = c.Id,
                HeroicSecondaryEffectDefId = effectId,
            });
        }

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
