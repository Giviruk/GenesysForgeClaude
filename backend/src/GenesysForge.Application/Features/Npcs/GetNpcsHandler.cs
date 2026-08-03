using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Npcs;

public class GetNpcsHandler(IAppDbContext db) : IQueryHandler<GetNpcsQuery, List<NpcListItemDto>>
{
    public async Task<List<NpcListItemDto>> Handle(GetNpcsQuery q, CancellationToken ct = default)
    {
        var uid = q.UserId;

        // Retired-существо (девять записей Haunted City, ROT-CLEAN-3.6) исключено из активного
        // бестиария, но остаётся доступным по id — в уже созданных столкновениях и при копировании.
        var query = db.Npcs.AsNoTracking()
            .Include(n => n.Skills)
            .Where(n => !n.Retired)
            .Where(n => n.OwnerUserId == uid
                || n.IsBuiltIn
                || (n.Visibility == NpcVisibility.CampaignVisible && n.CampaignId != null
                    && db.CampaignCharacters.Any(cc => cc.PlayerUserId == uid
                        && cc.CampaignId == n.CampaignId.Value)));

        if (q.System is { } system) query = query.Where(n => n.System == system);
        if (q.Kind is { } kind) query = query.Where(n => n.Kind == kind);
        if (q.Role is { } role) query = query.Where(n => n.Role == role);
        if (q.CampaignId is { } cid) query = query.Where(n => n.CampaignId == cid);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(n => n.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(q.Tag))
        {
            var tag = q.Tag.Trim();
            query = query.Where(n => n.Tags.Contains(tag));
        }

        query = q.Sort == "name"
            ? query.OrderBy(n => n.Name)
            : query.OrderByDescending(n => n.CreatedAt);

        var npcs = await query.ToListAsync(ct);
        return npcs.Select(n => NpcMapper.ToListItem(n, uid)).ToList();
    }
}
