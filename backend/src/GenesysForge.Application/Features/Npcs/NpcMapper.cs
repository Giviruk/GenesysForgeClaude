using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Npcs;

/// <summary>Маппинг NPC в DTO и проверки доступа (см. спецификацию §8).</summary>
public static class NpcMapper
{
    public static NpcDetailDto ToDetail(Npc npc, Guid userId) => new(
        npc.Id, npc.Name, npc.System, npc.Kind, npc.Role, npc.Description, npc.Source,
        npc.Brawn, npc.Agility, npc.Intellect, npc.Cunning, npc.Willpower, npc.Presence,
        npc.WoundThreshold, npc.StrainThreshold, npc.Soak, npc.MeleeDefense, npc.RangedDefense,
        npc.Silhouette, npc.Tactics,
        npc.Visibility, npc.CampaignId, npc.OwnerUserId == userId,
        npc.Skills.OrderBy(s => s.Name).Select(s => new NpcSkillDto(s.Name, s.Ranks)).ToList(),
        npc.Abilities.Select(a => new NpcAbilityDto(a.Name, a.Description)).ToList(),
        npc.Attacks.Select(a => new NpcAttackDto(a.Name, a.SkillName, a.Damage, a.Critical, a.RangeBand, a.Notes,
            a.Qualities.Select(q => new NpcAttackQualityDto(q.QualityCode, q.NameRu, q.Rating)).ToList(),
            a.SourceWeapon)).ToList(),
        npc.Talents, npc.Equipment, npc.Tags,
        NpcValidator.Validate(npc).Warnings,
        npc.CreatedAt, npc.UpdatedAt);

    public static NpcListItemDto ToListItem(Npc npc, Guid userId) => new(
        npc.Id, npc.Name, npc.System, npc.Kind, npc.Role, npc.Soak, npc.WoundThreshold,
        npc.StrainThreshold, npc.Visibility, npc.CampaignId, npc.OwnerUserId == userId,
        npc.Skills.OrderBy(s => s.Name).Select(s => new NpcSkillDto(s.Name, s.Ranks)).ToList(),
        npc.Tags, npc.CreatedAt);

    /// <summary>Загружает NPC с коллекциями или бросает «не найден».</summary>
    public static async Task<Npc> LoadAsync(IAppDbContext db, Guid id, CancellationToken ct, bool tracking = false)
    {
        var query = db.Npcs.Include(n => n.Skills).Include(n => n.Abilities)
            .Include(n => n.Attacks).ThenInclude(a => a.Qualities).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(n => n.Id == id, ct)
            ?? throw new DomainRuleException("NPC не найден.");
    }

    /// <summary>NPC, который пользователь имеет право видеть (владелец или участник кампании с visible).</summary>
    public static async Task<Npc> GetViewableAsync(IAppDbContext db, Guid userId, Guid id, CancellationToken ct)
    {
        var npc = await LoadAsync(db, id, ct);
        if (await CanViewAsync(db, npc, userId, ct)) return npc;
        throw new DomainRuleException("NPC не найден.");
    }

    /// <summary>NPC, которым пользователь владеет; иначе ошибка доступа.</summary>
    public static async Task<Npc> GetOwnedAsync(IAppDbContext db, Guid userId, Guid id, CancellationToken ct, bool tracking = false)
    {
        var npc = await LoadAsync(db, id, ct, tracking);
        if (npc.OwnerUserId != userId)
            throw new DomainRuleException("NPC не найден.");
        return npc;
    }

    public static async Task<bool> CanViewAsync(IAppDbContext db, Npc npc, Guid userId, CancellationToken ct)
    {
        if (npc.OwnerUserId == userId) return true;
        if (npc.Visibility == NpcVisibility.CampaignVisible && npc.CampaignId is { } cid)
            return await db.CampaignCharacters.AnyAsync(cc => cc.CampaignId == cid && cc.PlayerUserId == userId, ct);
        return false;
    }

    public static void Apply(Npc npc, NpcInput input)
    {
        npc.Name = input.Name.Trim();
        npc.System = input.System;
        npc.Kind = input.Kind;
        npc.Role = input.Role;
        npc.Description = input.Description?.Trim() ?? "";
        npc.Source = input.Source?.Trim() ?? "";
        npc.Brawn = input.Brawn;
        npc.Agility = input.Agility;
        npc.Intellect = input.Intellect;
        npc.Cunning = input.Cunning;
        npc.Willpower = input.Willpower;
        npc.Presence = input.Presence;
        npc.WoundThreshold = input.WoundThreshold;
        npc.StrainThreshold = input.Kind == NpcKind.Minion ? null : input.StrainThreshold;
        npc.Soak = input.Soak;
        npc.MeleeDefense = input.MeleeDefense;
        npc.RangedDefense = input.RangedDefense;
        npc.Silhouette = input.Silhouette;
        npc.Tactics = input.Tactics?.Trim() ?? "";
        npc.Visibility = input.Visibility;
        npc.CampaignId = input.CampaignId;
        npc.Talents = Clean(input.Talents);
        npc.Equipment = Clean(input.Equipment);
        npc.Tags = Clean(input.Tags);
        // Миньон использует групповые навыки: индивидуальные ранги не значимы (ранг = размер группы − 1).
        var minion = input.Kind == NpcKind.Minion;
        npc.Skills = (input.Skills ?? [])
            .Where(s => !string.IsNullOrWhiteSpace(s.Name))
            .Select(s => new NpcSkill { NpcId = npc.Id, Name = s.Name.Trim(), Ranks = minion ? 0 : s.Ranks })
            .ToList();
        npc.Abilities = (input.Abilities ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new NpcAbility { NpcId = npc.Id, Name = a.Name.Trim(), Description = a.Description?.Trim() ?? "" })
            .ToList();
        npc.Attacks = (input.Attacks ?? [])
            .Where(a => !string.IsNullOrWhiteSpace(a.Name))
            .Select(a => new NpcAttack
            {
                NpcId = npc.Id,
                Name = a.Name.Trim(),
                SkillName = a.SkillName?.Trim() ?? "",
                Damage = a.Damage?.Trim() ?? "",
                Critical = a.Critical?.Trim() ?? "",
                RangeBand = a.RangeBand?.Trim() ?? "",
                Notes = a.Notes?.Trim() ?? "",
                SourceWeapon = a.SourceWeapon?.Trim() ?? "",
                Qualities = (a.Qualities ?? [])
                    .Where(q => !string.IsNullOrWhiteSpace(q.QualityCode) || !string.IsNullOrWhiteSpace(q.NameRu))
                    .Select(q => new NpcAttackQuality
                    {
                        QualityCode = q.QualityCode?.Trim() ?? "",
                        NameRu = q.NameRu?.Trim() ?? "",
                        Rating = q.Rating,
                    }).ToList(),
            }).ToList();
        npc.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Привязывает качества атак к справочнику <see cref="QualityDef"/> по коду: проставляет
    /// <c>QualityDefId</c>, канонизирует <c>NameRu</c> и обнуляет рейтинг у безрейтинговых качеств.
    /// Несопоставленные коды остаются кастомными (QualityDefId = null). Вызывать после <see cref="Apply"/>.
    /// </summary>
    public static async Task ResolveAttackQualitiesAsync(IAppDbContext db, Npc npc, CancellationToken ct)
    {
        var codes = npc.Attacks.SelectMany(a => a.Qualities)
            .Select(q => q.QualityCode).Where(c => c.Length > 0).Distinct().ToList();
        if (codes.Count == 0) return;

        var defs = await db.QualityDefs.AsNoTracking()
            .Where(q => codes.Contains(q.Code)).ToDictionaryAsync(q => q.Code, ct);

        foreach (var quality in npc.Attacks.SelectMany(a => a.Qualities))
        {
            if (!defs.TryGetValue(quality.QualityCode, out var def)) continue;
            quality.QualityDefId = def.Id;
            if (!string.IsNullOrWhiteSpace(def.NameRu)) quality.NameRu = def.NameRu;
            if (!def.HasRating) quality.Rating = null;
        }
    }

    private static List<string> Clean(List<string>? values) =>
        (values ?? []).Select(v => v.Trim()).Where(v => v.Length > 0).ToList();
}
