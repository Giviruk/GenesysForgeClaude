using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;

namespace GenesysForge.Application.Features.CustomContent;

internal static class CampaignCustomContent
{
    public static async Task<Guid?> GetOrCreatePackIdAsync(IAppDbContext db, Guid? campaignId,
        Guid userId, GameSystem system, CancellationToken ct)
    {
        if (!Enum.IsDefined(system)) throw new DomainRuleException("Неизвестная игровая система.");
        if (campaignId is not null) await CampaignMapper.GetAsGmAsync(db, userId, campaignId.Value, ct);
        return null;
    }
}
