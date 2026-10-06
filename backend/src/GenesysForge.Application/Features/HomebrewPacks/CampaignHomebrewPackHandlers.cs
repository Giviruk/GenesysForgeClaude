using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.HomebrewPacks;

public record GetCampaignHomebrewPacksQuery(Guid UserId, Guid CampaignId) : IQuery<List<CampaignHomebrewPackDto>>;
public record ConnectSharedCampaignHomebrewPackCommand(Guid UserId, Guid CampaignId, string Token) : ICommand<HomebrewPackImportResult>;

public class GetCampaignHomebrewPacksHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignHomebrewPacksQuery, List<CampaignHomebrewPackDto>>
{
    public async Task<List<CampaignHomebrewPackDto>> Handle(GetCampaignHomebrewPacksQuery query, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, query.UserId, query.CampaignId, ct);
        var rows = await (from link in db.HomebrewPackCampaigns.AsNoTracking()
            join pack in db.HomebrewPacks.AsNoTracking() on link.HomebrewPackId equals pack.Id
            where link.CampaignId == query.CampaignId
            orderby pack.Name
            select new { Pack = pack, link.IsEnabled }).ToListAsync(ct);
        var counts = await HomebrewPackMapper.CountEntriesAsync(db, rows.Select(r => r.Pack.Id).ToHashSet(), ct);
        return rows.Select(r => new CampaignHomebrewPackDto(r.Pack.Id, r.Pack.Name, r.Pack.System,
            r.IsEnabled, r.Pack.OwnerUserId == query.UserId, counts.GetValueOrDefault(r.Pack.Id))).ToList();
    }
}

public class ConnectSharedCampaignHomebrewPackHandler(IAppDbContext db)
    : ICommandHandler<ConnectSharedCampaignHomebrewPackCommand, HomebrewPackImportResult>
{
    public async Task<HomebrewPackImportResult> Handle(ConnectSharedCampaignHomebrewPackCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, command.UserId, command.CampaignId, ct);
        var hash = HomebrewPackTokens.Hash(command.Token);
        var pack = await db.HomebrewPacks.AsNoTracking()
            .FirstOrDefaultAsync(p => p.IsShared && p.ShareTokenHash == hash, ct)
            ?? throw new DomainRuleException("Shared-набор не найден.", "homebrew.shared_not_found");
        var row = await db.HomebrewPackCampaigns.FirstOrDefaultAsync(
            x => x.HomebrewPackId == pack.Id && x.CampaignId == command.CampaignId, ct);
        if (row is null)
            db.HomebrewPackCampaigns.Add(new HomebrewPackCampaign
            {
                Id = Guid.NewGuid(), HomebrewPackId = pack.Id, CampaignId = command.CampaignId, IsEnabled = true,
            });
        else
        {
            row.IsEnabled = true;
            row.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(ct);
        var counts = await HomebrewPackMapper.CountEntriesAsync(db, [pack.Id], ct);
        return new HomebrewPackImportResult(pack.Id, pack.Name, counts.GetValueOrDefault(pack.Id));
    }
}
