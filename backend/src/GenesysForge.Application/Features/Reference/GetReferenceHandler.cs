using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Reference;

public class GetReferenceHandler(IAppDbContext db) : IQueryHandler<GetReferenceQuery, ReferenceResponse>
{
    public async Task<ReferenceResponse> Handle(GetReferenceQuery query, CancellationToken ct = default)
    {
        var (userId, system) = (query.UserId, query.System);

        // Retired виды остаются в БД ради уже созданных персонажей, но не предлагаются при создании.
        // Материализуем с дочерними коллекциями (способности/стартовые навыки) и маппим в памяти.
        var archetypeDefs = await db.ArchetypeDefs.AsNoTracking()
            .Include(a => a.Abilities)
            .Include(a => a.StartingSkills)
            .Where(a => a.System == system && !a.Retired).OrderBy(a => a.NameRu)
            .ToListAsync(ct);
        var archetypes = archetypeDefs.Select(a => a.ToDto()).ToList();

        var careerDefs = await db.CareerDefs.AsNoTracking()
            .Include(c => c.StartingGear)
            .Include(c => c.Rules)
            .Where(c => c.System == system).OrderBy(c => c.NameRu)
            .ToListAsync(ct);
        var careers = careerDefs.Select(c => c.ToDto()).ToList();

        var skills = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && (s.OwnerUserId == null || s.OwnerUserId == userId))
            .OrderBy(s => s.Kind).ThenBy(s => s.Name)
            .Select(s => s.ToDto()).ToListAsync(ct);

        // Genesys Core показывает только таланты «для любого сеттинга»; Realms of Terrinoth — плюс фэнтези.
        // Кастомные таланты владельца показываются всегда, независимо от сеттинга.
        var settingMask = system == GameSystem.RealmsOfTerrinoth
            ? GenesysSetting.Any | GenesysSetting.Fantasy
            : GenesysSetting.Any;

        var talents = await db.TalentDefs.AsNoTracking()
            .Where(t => t.System == system
                && (t.OwnerUserId == userId
                    || (t.OwnerUserId == null && (t.Setting & settingMask) != 0)))
            .OrderBy(t => t.Tier).ThenBy(t => t.Name)
            .Select(t => t.ToDto()).ToListAsync(ct);

        // Материализуем с навигацией Qualities → QualityDef, затем маппим в памяти (ToDto тянет навигацию).
        var itemDefs = await db.ItemDefs.AsNoTracking()
            .Include(i => i.Qualities).ThenInclude(v => v.QualityDef)
            .Where(i => i.System == system && (i.OwnerUserId == null || i.OwnerUserId == userId))
            .OrderBy(i => i.Kind).ThenBy(i => i.Name)
            .ToListAsync(ct);
        var items = itemDefs.Select(i => i.ToDto()).ToList();

        var qualities = await db.QualityDefs.AsNoTracking()
            .OrderBy(q => q.NameRu)
            .Select(q => q.ToDto()).ToListAsync(ct);

        // Героики материализуем вместе с улучшениями и маппим в памяти (ToDto тянет навигацию Upgrades).
        var heroicDefs = system == GameSystem.RealmsOfTerrinoth
            ? await db.HeroicAbilityDefs.AsNoTracking()
                .Include(h => h.Upgrades)
                .Include(h => h.Effects)
                .Where(h => h.OwnerUserId == null || h.OwnerUserId == userId)
                .OrderBy(h => h.NameRu)
                .ToListAsync(ct)
            : [];
        var heroics = heroicDefs.Select(h => h.ToDto()).ToList();

        return new ReferenceResponse(archetypes, careers, skills, talents, items, heroics, qualities);
    }
}
