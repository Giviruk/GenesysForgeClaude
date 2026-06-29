using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

public static class CharacterLoader
{
    /// <summary>Загружает персонажа со всеми связями и проверяет владельца.</summary>
    public static async Task<Character> GetOwnedAsync(
        this IAppDbContext db, Guid userId, Guid characterId, bool tracking = true, CancellationToken ct = default)
    {
        var character = await db.LoadWithRelationsAsync(characterId, tracking, ct);
        if (character is null || character.OwnerUserId != userId)
            throw new DomainRuleException("Персонаж не найден.");
        return character;
    }

    /// <summary>
    /// Загружает персонажа со всеми связями <b>без</b> проверки владельца (или <c>null</c>, если не найден).
    /// Вызывающий обязан сам авторизовать доступ (напр. GM-доступ к листу участника кампании, U-20).
    /// </summary>
    public static async Task<Character?> LoadWithRelationsAsync(
        this IAppDbContext db, Guid characterId, bool tracking = true, CancellationToken ct = default)
    {
        var query = db.Characters
            .Include(c => c.Archetype)
            .Include(c => c.Career)
            .Include(c => c.HeroicAbility).ThenInclude(h => h!.Upgrades)
            .Include(c => c.Skills).ThenInclude(s => s.SkillDef)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef)
            .Include(c => c.CriticalInjuries);
        return await (tracking ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);
    }
}
