using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Устанавливает купленный ранг улучшения героической способности (0 — базовая, 1 — Improved, 2 — Supreme).
/// Legacy endpoint только для Power. Очки начисляются по 1 за каждые 50 XP сверх стартового XP вида.
/// Понижение ранга разрешено только до завершения создания.
/// </summary>
public class SetHeroicUpgradeRankHandler(IAppDbContext db) : ICommandHandler<SetHeroicUpgradeRankCommand, Unit>
{
    public async Task<Unit> Handle(SetHeroicUpgradeRankCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.System != GameSystem.RealmsOfTerrinoth)
            throw new DomainRuleException("Героические способности доступны только в Realms of Terrinoth.");
        if (c.HeroicAbility is null)
            throw new DomainRuleException("Сначала выберите героическую способность.");

        var maxRank = c.HeroicAbility.Upgrades.Count;
        if (command.Rank < 0 || command.Rank > maxRank)
            throw new DomainRuleException($"Недопустимый ранг улучшения: {command.Rank}.");
        if (command.Rank < c.HeroicUpgradeRank && !c.IsCreationPhase)
            throw new DomainRuleException("После завершения создания улучшения героической способности постоянны.");

        // Стоимость достижения целевого ранга — сумма стоимостей улучшений с уровнем ≤ ранга.
        var cost = c.HeroicAbility.Upgrades
            .Where(u => (int)u.Level <= command.Rank)
            .Sum(u => u.Cost);
        var otherCost = c.HeroicDurationRanks + c.HeroicFrequencyRanks * 2
            + (c.HeroicStoryUpgrade ? 1 : 0) + c.HeroicSecondaryEffects.Count;
        if (cost + otherCost > c.HeroicUpgradePointsTotal)
            throw new DomainRuleException(
                $"Недостаточно ability points: нужно {cost + otherCost}, доступно {c.HeroicUpgradePointsTotal}.");

        c.HeroicUpgradeRank = command.Rank;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
