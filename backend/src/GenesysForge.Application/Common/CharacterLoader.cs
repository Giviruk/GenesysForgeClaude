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
            .Include(c => c.Archetype).ThenInclude(a => a!.StartingSkills)
            .Include(c => c.Archetype).ThenInclude(a => a!.Abilities)
            .Include(c => c.Career)
            .Include(c => c.HeroicAbility).ThenInclude(h => h!.Upgrades)
            .Include(c => c.HeroicSecondaryEffects).ThenInclude(x => x.HeroicSecondaryEffectDef)
            .Include(c => c.Skills).ThenInclude(s => s.SkillDef)
            .Include(c => c.Talents).ThenInclude(t => t.TalentDef)
            .Include(c => c.Talents).ThenInclude(t => t.Choices)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef)
            // Штрафы снаряжения к проверкам нужны и листу, и Game Table (ROT-ARM-01).
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.CheckModifiers)
            // Качества предмета участвуют в защите (щит — оружие, ROT-WPN-01), профили — в атаках.
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.Qualities).ThenInclude(q => q.QualityDef)
            .Include(c => c.Items).ThenInclude(i => i.ItemDef!.AttackProfiles)
            .Include(c => c.CriticalInjuries)
            .Include(c => c.HeroicConfiguration).ThenInclude(x => x!.ParagonSkillDef)
            .Include(c => c.SignatureWeapon);
        return await (tracking ? query : query.AsNoTracking())
            .FirstOrDefaultAsync(c => c.Id == characterId, ct);
    }
}
