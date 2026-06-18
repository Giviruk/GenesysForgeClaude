using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Encounters;

/// <summary>Список энкаунтеров кампании. GM видит все, игрок — только visible.</summary>
public record GetEncountersQuery(Guid UserId, Guid CampaignId, string? Search, string? Type, string? Tag)
    : IQuery<List<EncounterListItemDto>>;

public class GetEncountersHandler(IAppDbContext db) : IQueryHandler<GetEncountersQuery, List<EncounterListItemDto>>
{
    public async Task<List<EncounterListItemDto>> Handle(GetEncountersQuery q, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, q.UserId, q.CampaignId, ct);
        var isGm = campaign.GmUserId == q.UserId;

        var query = db.Encounters.AsNoTracking()
            .Include(e => e.Participants)
            .Where(e => e.CampaignId == q.CampaignId && (isGm || e.IsVisibleToPlayers));

        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var term = q.Search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(q.Type)
            && Enum.TryParse<Domain.EncounterType>(q.Type, ignoreCase: true, out var type))
            query = query.Where(e => e.Type == type);
        if (!string.IsNullOrWhiteSpace(q.Tag))
        {
            var tag = q.Tag.Trim();
            query = query.Where(e => e.Tags.Contains(tag));
        }

        var encounters = await query.OrderByDescending(e => e.UpdatedAt).ToListAsync(ct);
        return encounters.Select(EncounterMapper.ToListItem).ToList();
    }
}
