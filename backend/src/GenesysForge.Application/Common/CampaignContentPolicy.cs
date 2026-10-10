using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>A snapshot of allowed content. Campaign contexts are a union; closing a system only gates entry.</summary>
public sealed class CampaignContentPolicy
{
    public HashSet<Guid> CustomIds { get; } = [];
    private readonly List<(Guid Id, string Name, HashSet<(BaseContentCategory, string)> Blocked)> campaigns = [];
    public bool IsCampaignContext => campaigns.Count != 0;

    public bool Allows(BaseContentCategory category, string key) =>
        campaigns.Count == 0 || campaigns.Any(c => !c.Blocked.Contains((category, key)));

    public string[] BlockedKeys(BaseContentCategory category) => campaigns.Count == 0 ? []
        : campaigns[0].Blocked.Where(x => x.Item1 == category && campaigns.All(c => c.Blocked.Contains(x)))
            .Select(x => x.Item2).ToArray();

    public string? UnavailableReason(BaseContentCategory category, string key, Guid? owner, Guid id) =>
        owner is null ? (Allows(category, key) ? null : $"Контент отключён в кампании «{campaigns[0].Name}» — новые покупки недоступны.")
            : CustomIds.Contains(id) ? null : "Кастомный контент отключён — новые покупки недоступны.";

    public void EnsureAllowed(BaseContentCategory category, string key, string name)
    {
        if (!Allows(category, key))
            throw new DomainRuleException($"«{name}» отключён в кампании «{campaigns[0].Name}».", "content.disabled_in_campaign");
    }

    public static async Task EnsureSystemOpenAsync(IAppDbContext db, Guid campaignId, GameSystem system, CancellationToken ct)
    {
        if (await db.CampaignSystemSettings.AnyAsync(x => x.CampaignId == campaignId && x.System == system && !x.IsOpen, ct))
            throw new DomainRuleException("Эта система закрыта для новых персонажей в кампании.", "content.system_closed");
    }

    public static async Task<CampaignContentPolicy> LoadAsync(IAppDbContext db, Guid userId, GameSystem system,
        Guid? characterId = null, Guid? campaignId = null, CancellationToken ct = default)
    {
        if (!Enum.IsDefined(system)) throw new DomainRuleException("Неизвестная игровая система.");
        if (characterId is not null && !await db.Characters.AnyAsync(c => c.Id == characterId && c.OwnerUserId == userId, ct))
            throw new DomainRuleException("Персонаж не найден.");
        if (campaignId is not null) await CampaignMapper.GetAccessibleAsync(db, userId, campaignId.Value, ct);
        var ids = campaignId is not null ? new List<Guid> { campaignId.Value }
            : characterId is not null ? await db.CampaignCharacters.AsNoTracking()
                .Where(x => x.CharacterId == characterId).Select(x => x.CampaignId).ToListAsync(ct) : [];
        var result = new CampaignContentPolicy();
        if (ids.Count == 0)
        {
            var own = await db.HomebrewPacks.AsNoTracking().Where(x => x.OwnerUserId == userId && x.System == system).ToListAsync(ct);
            var ownPackIds = own.Select(x => x.Id).ToList();
            var allEntries = await db.HomebrewPackEntries.AsNoTracking().Where(x => ownPackIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
            var enabled = own.Where(x => x.IsEnabledByDefault).Select(x => x.Id).ToHashSet();
            if (characterId is not null)
                foreach (var toggle in await db.HomebrewPackCharacters.AsNoTracking().Where(x => x.CharacterId == characterId).ToListAsync(ct))
                {
                    if (!own.Any(p => p.Id == toggle.HomebrewPackId)) continue;
                    if (toggle.IsEnabled) enabled.Add(toggle.HomebrewPackId); else enabled.Remove(toggle.HomebrewPackId);
                }
            var ownDefinitions = await ContentDefinitions.LoadAsync(db, system, userId, ct);
            var packed = allEntries.Select(x => x.EntryId).ToHashSet();
            result.CustomIds.UnionWith(ownDefinitions.Where(x => !packed.Contains(x.Id)).Select(x => x.Id));
            result.CustomIds.UnionWith(allEntries.Where(x => enabled.Contains(x.HomebrewPackId)).Select(x => x.EntryId));
            return result;
        }
        var names = await db.Campaigns.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
        var links = await (from link in db.HomebrewPackCampaigns.AsNoTracking()
            join pack in db.HomebrewPacks.AsNoTracking() on link.HomebrewPackId equals pack.Id
            where ids.Contains(link.CampaignId) && pack.System == system && link.IsEnabled && link.Status == ContentConnectionStatus.Active
            select link).ToListAsync(ct);
        var linkIds = links.Select(x => x.Id).ToList();
        var states = await db.CampaignPackEntryStates.AsNoTracking().Where(x => linkIds.Contains(x.HomebrewPackCampaignId)).ToListAsync(ct);
        var connectedPackIds = links.Select(x => x.HomebrewPackId).ToList();
        var connectedEntries = await db.HomebrewPackEntries.AsNoTracking().Where(x => connectedPackIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        foreach (var link in links)
        {
            var blocked = states.Where(x => x.HomebrewPackCampaignId == link.Id).Select(x => (x.EntryType, x.EntryId)).ToHashSet();
            result.CustomIds.UnionWith(connectedEntries.Where(x => x.HomebrewPackId == link.HomebrewPackId && !blocked.Contains((x.EntryType, x.EntryId))).Select(x => x.EntryId));
        }
        var loose = await db.CampaignContentItems.AsNoTracking()
            .Where(x => ids.Contains(x.CampaignId) && x.IsEnabled && x.Status == ContentConnectionStatus.Active).Select(x => x.EntryId).ToListAsync(ct);
        // Entry IDs remain unique even across definition types; verify system at each definition query.
        result.CustomIds.UnionWith(loose);
        var packIds = links.Select(x => x.HomebrewPackId).ToList();
        var exclusions = await db.HomebrewPackExclusions.AsNoTracking().Where(x => packIds.Contains(x.HomebrewPackId)).ToListAsync(ct);
        var overrides = await db.CampaignBaseOverrides.AsNoTracking().Where(x => ids.Contains(x.CampaignId) && x.System == system).ToListAsync(ct);
        foreach (var id in ids)
        {
            var active = links.Where(x => x.CampaignId == id).Select(x => x.HomebrewPackId).ToHashSet();
            var blocked = exclusions.Where(x => active.Contains(x.HomebrewPackId)).Select(x => (x.Category, x.ContentKey)).ToHashSet();
            foreach (var ov in overrides.Where(x => x.CampaignId == id))
                if (ov.IsEnabled) blocked.Remove((ov.Category, ov.ContentKey)); else blocked.Add((ov.Category, ov.ContentKey));
            result.campaigns.Add((id, names.GetValueOrDefault(id, ""), blocked));
        }
        return result;
    }

    public static List<SpellDef> FilterSpells(IEnumerable<SpellDef> definitions, CampaignContentPolicy policy)
    {
        var rows = definitions.ToList();
        var allowedParents = rows.Where(x => x.Kind == SpellEntryKind.Effect && policy.Allows(BaseContentCategory.Magic, SpellKey(x)))
            .Select(x => (x.MagicSkill, x.NameEn)).ToHashSet();
        return rows.Where(x => x.Kind == SpellEntryKind.Effect ? allowedParents.Contains((x.MagicSkill, x.NameEn))
            : x.MagicSkill.Length > 0 ? allowedParents.Contains((x.MagicSkill, x.ParentEffect))
            : allowedParents.Any(p => p.NameEn == x.ParentEffect && x.AllowedSkills.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Contains(p.MagicSkill))).ToList();
    }

    public static string SpellKey(SpellDef s) => $"{s.MagicSkill}:{s.Kind}:{s.NameEn}";
}
