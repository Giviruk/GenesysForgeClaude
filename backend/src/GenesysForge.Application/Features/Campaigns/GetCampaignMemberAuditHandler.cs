using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public record GetCampaignMemberAuditQuery(Guid UserId, Guid CampaignId, Guid CharacterId, int Take)
    : IQuery<IReadOnlyList<CharacterAuditEntryDto>>;

/// <summary>Read-only история персонажа для мастера и участников той же кампании.</summary>
public class GetCampaignMemberAuditHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignMemberAuditQuery, IReadOnlyList<CharacterAuditEntryDto>>
{
    public async Task<IReadOnlyList<CharacterAuditEntryDto>> Handle(
        GetCampaignMemberAuditQuery query, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        var isMember = await db.CampaignCharacters.AsNoTracking().AnyAsync(
            value => value.CampaignId == query.CampaignId && value.CharacterId == query.CharacterId, ct);
        if (!isMember) throw new DomainRuleException("Персонаж не найден в кампании.");

        return await db.CharacterAuditEntries.AsNoTracking()
            .Where(value => value.CharacterId == query.CharacterId)
            .OrderByDescending(value => value.CreatedAt)
            .Take(Math.Clamp(query.Take, 1, 500))
            .Select(value => new CharacterAuditEntryDto(
                value.Id, value.CreatedAt, value.Action, value.Summary, value.XpDelta,
                value.TotalXpAfter, value.SpentXpAfter))
            .ToListAsync(ct);
    }
}
