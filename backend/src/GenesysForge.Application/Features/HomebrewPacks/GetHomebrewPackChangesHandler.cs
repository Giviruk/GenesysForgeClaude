using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.HomebrewPacks;

public record GetHomebrewPackChangesQuery(Guid UserId, Guid PackId, Guid? CampaignId = null, int Take = 100)
    : IQuery<List<CustomContentChangeDto>>;

public class GetHomebrewPackChangesHandler(IAppDbContext db)
    : IQueryHandler<GetHomebrewPackChangesQuery, List<CustomContentChangeDto>>
{
    public async Task<List<CustomContentChangeDto>> Handle(GetHomebrewPackChangesQuery query, CancellationToken ct = default)
    {
        var pack = await db.HomebrewPacks.AsNoTracking().FirstOrDefaultAsync(p => p.Id == query.PackId, ct)
            ?? throw new DomainRuleException("Набор не найден.", "homebrew.changes_not_accessible");
        if (pack.OwnerUserId != query.UserId)
        {
            if (query.CampaignId is not { } campaignId)
                throw new DomainRuleException("История набора недоступна.", "homebrew.changes_not_accessible");
            await CampaignMapper.GetAccessibleAsync(db, query.UserId, campaignId, ct);
            if (!await db.HomebrewPackCampaigns.AnyAsync(x => x.CampaignId == campaignId && x.HomebrewPackId == pack.Id, ct))
                throw new DomainRuleException("Набор не подключён к кампании.", "homebrew.changes_not_accessible");
        }
        var rows = await (from change in db.CustomContentChanges.AsNoTracking()
            join author in db.Users.AsNoTracking() on change.UserId equals author.Id into authors
            from author in authors.DefaultIfEmpty()
            where change.HomebrewPackId == pack.Id
            orderby change.CreatedAt descending, change.Id descending
            select new { Change = change, UserName = author == null ? "Удалённый пользователь" : author.DisplayName })
            .Take(Math.Clamp(query.Take, 1, 200)).ToListAsync(ct);
        return rows.Select(r => new CustomContentChangeDto(r.Change.Id, r.Change.HomebrewPackId,
            r.Change.DefinitionType, r.Change.DefinitionId, r.Change.DefinitionName, r.Change.UserId,
            r.UserName, r.Change.Action, CustomContentAudit.Changes(r.Change.ChangesJson), r.Change.CreatedAt)).ToList();
    }
}
