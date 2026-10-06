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
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
            db, userId, system, query.CharacterId, query.CampaignId, ct);

        // Retired-записи остаются в БД ради уже созданных персонажей, NPC и экспортов, но не
        // предлагаются при создании, покупке и поиске. Фильтр применяется ко всем справочникам.
        // Материализуем с дочерними коллекциями (способности/стартовые навыки) и маппим в памяти.
        var archetypeDefs = await db.ArchetypeDefs.AsNoTracking()
            .Include(a => a.Abilities)
            .Include(a => a.StartingSkills)
            .Where(a => a.System == system && !a.Retired
                && (a.OwnerUserId == null
                    || (a.HomebrewPackId == null ? a.OwnerUserId == userId
                        : visiblePackIds.Contains(a.HomebrewPackId.Value))))
            .OrderBy(a => a.NameRu)
            .ToListAsync(ct);
        var archetypes = archetypeDefs.Select(a => a.ToDto()).ToList();

        var careerDefs = await db.CareerDefs.AsNoTracking()
            .Include(c => c.StartingGear)
            .Include(c => c.Rules)
            .Where(c => c.System == system && !c.Retired
                && (c.OwnerUserId == null
                    || (c.HomebrewPackId == null ? c.OwnerUserId == userId
                        : visiblePackIds.Contains(c.HomebrewPackId.Value))))
            .OrderBy(c => c.NameRu)
            .ToListAsync(ct);
        var careers = careerDefs.Select(c => c.ToDto()).ToList();

        var skillDefs = await db.SkillDefs.AsNoTracking()
            .Where(s => s.System == system && !s.Retired
                && (s.OwnerUserId == null
                    || (s.HomebrewPackId == null ? s.OwnerUserId == userId
                        : visiblePackIds.Contains(s.HomebrewPackId.Value))))
            .OrderBy(s => s.Kind).ThenBy(s => s.Name)
            .ToListAsync(ct);
        var skills = skillDefs.Select(s => s.ToDto()).ToList();

        // Genesys Core показывает только таланты «для любого сеттинга»; Realms of Terrinoth — плюс фэнтези.
        // Кастомные таланты владельца показываются всегда, независимо от сеттинга.
        var settingMask = system == GameSystem.RealmsOfTerrinoth
            ? GenesysSetting.Any | GenesysSetting.Fantasy
            : GenesysSetting.Any;

        var talentDefs = await db.TalentDefs.AsNoTracking()
            .Where(t => t.System == system && !t.Retired
                && (((t.HomebrewPackId == null ? t.OwnerUserId == userId
                        : visiblePackIds.Contains(t.HomebrewPackId.Value)))
                    || (t.OwnerUserId == null && (t.Setting & settingMask) != 0)))
            .OrderBy(t => t.Tier).ThenBy(t => t.Name)
            .ToListAsync(ct);
        var talents = talentDefs.Select(t => t.ToDto()).ToList();

        // Материализуем с навигацией Qualities → QualityDef, затем маппим в памяти (ToDto тянет навигацию).
        var itemDefs = await db.ItemDefs.AsNoTracking()
            .Include(i => i.Qualities).ThenInclude(v => v.QualityDef)
            .Include(i => i.CheckModifiers)
            .Include(i => i.AttackProfiles)
            .Where(i => i.System == system && !i.Retired
                && (i.OwnerUserId == null
                    || (i.HomebrewPackId == null ? i.OwnerUserId == userId
                        : visiblePackIds.Contains(i.HomebrewPackId.Value))))
            .OrderBy(i => i.Kind).ThenBy(i => i.Name)
            .ToListAsync(ct);
        // Качества альтернативных профилей атаки хранятся кодами (ROT-WPN-01) и резолвятся справочником.
        var qualityDefRows = await db.QualityDefs.AsNoTracking()
            .OrderBy(q => q.NameRu)
            .ToListAsync(ct);
        var qualityDefs = qualityDefRows
            .ToDictionary(q => q.Code, StringComparer.Ordinal);
        var items = itemDefs.Select(i => i.ToDto(qualityDefs)).ToList();

        var qualities = qualityDefRows
            .Where(q => !q.Retired)
            .Select(q => q.ToDto()).ToList();

        // Героики материализуем вместе с улучшениями и маппим в памяти (ToDto тянет навигацию Upgrades).
        var heroicDefs = system == GameSystem.RealmsOfTerrinoth
            ? await db.HeroicAbilityDefs.AsNoTracking()
                .Include(h => h.Upgrades)
                .Include(h => h.Effects)
                .Where(h => !h.Retired
                    && (h.OwnerUserId == null
                    || (h.HomebrewPackId == null ? h.OwnerUserId == userId
                        : visiblePackIds.Contains(h.HomebrewPackId.Value))))
                .OrderBy(h => h.NameRu)
                .ToListAsync(ct)
            : [];
        var heroics = heroicDefs.Select(h => h.ToDto()).ToList();

        var heroicSecondaryEffectDefs = system == GameSystem.RealmsOfTerrinoth
            ? await db.HeroicSecondaryEffectDefs.AsNoTracking().Where(x => !x.Retired).OrderBy(x => x.NameRu).ToListAsync(ct)
            : [];
        var heroicSecondaryEffects = heroicSecondaryEffectDefs.Select(x => x.ToDto()).ToList();

        // Улучшения — отдельный тип контента (ROT-EQP-ATT-01): встроенные и разрешённые custom записи.
        var attachments = (await db.AttachmentDefs.AsNoTracking().Include(a => a.Effects)
                .Where(a => a.System == system && !a.Retired && (a.OwnerUserId == null
                    || (a.HomebrewPackId == null ? a.OwnerUserId == userId : visiblePackIds.Contains(a.HomebrewPackId.Value))))
                .ToListAsync(ct))
            .OrderBy(a => a.NameRu, StringComparer.Ordinal)
            .Select(a => a.ToDto())
            .ToList();

        // Скакуны — тоже собственный тип контента (ROT-MOUNT-ITEM-01): витрине нужен статблок,
        // иначе покупка снова превратилась бы в безликую строку снаряжения.
        var mounts = (await db.MountDefs.AsNoTracking()
                .Include(m => m.Skills).Include(m => m.Abilities).Include(m => m.Attacks)
                .Where(m => m.System == system && !m.Retired && (m.OwnerUserId == null
                    || (m.HomebrewPackId == null ? m.OwnerUserId == userId : visiblePackIds.Contains(m.HomebrewPackId.Value))))
                .ToListAsync(ct))
            .OrderBy(m => m.Price ?? int.MaxValue).ThenBy(m => m.NameRu, StringComparer.Ordinal)
            .Select(MountMapper.DefDto)
            .ToList();

        var editableIds = archetypeDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id)
            .Concat(careerDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id))
            .Concat(skillDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id))
            .Concat(talentDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id))
            .Concat(itemDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id))
            .Concat(heroicDefs.Where(d => d.OwnerUserId == userId).Select(d => d.Id)).ToList();
        return new ReferenceResponse(
            archetypes, careers, skills, talents, items, heroics, qualities, heroicSecondaryEffects,
            attachments, mounts, editableIds);
    }
}
