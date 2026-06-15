using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Устанавливает купленный ранг улучшения героической способности (0 — базовая, 1 — Improved, 2 — Supreme).
/// Очки улучшения: 1 стартовое + по 1 каждые 50 заработанного XP. Supreme требует Improved (ранги последовательны).
/// Понижение ранга возвращает очки (рефанд), поэтому отдельной операции рефанда не нужно.
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

        // Стоимость достижения целевого ранга — сумма стоимостей улучшений с уровнем ≤ ранга.
        var cost = c.HeroicAbility.Upgrades
            .Where(u => (int)u.Level <= command.Rank)
            .Sum(u => u.Cost);
        if (cost > c.HeroicUpgradePointsTotal)
            throw new DomainRuleException(
                $"Недостаточно очков улучшения: нужно {cost}, доступно {c.HeroicUpgradePointsTotal}.");

        c.HeroicUpgradeRank = command.Rank;
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
