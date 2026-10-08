using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

internal static class CampaignMembership
{
    // SaveChanges belongs to the caller so membership and character links are committed together.
    public static async Task EnsureMemberAsync(IAppDbContext db, Campaign campaign, Guid userId, CancellationToken ct)
    {
        if (campaign.GmUserId == userId) return;
        if (!await db.CampaignMembers.AnyAsync(m => m.CampaignId == campaign.Id && m.UserId == userId, ct))
            db.CampaignMembers.Add(new CampaignMember
            {
                Id = Guid.NewGuid(), CampaignId = campaign.Id, UserId = userId,
            });
    }

    public static async Task AddCharacterAsync(IAppDbContext db, Campaign campaign, Guid userId,
        Guid characterId, CancellationToken ct)
    {
        if (await db.CampaignCharacters.AnyAsync(cc => cc.CampaignId == campaign.Id && cc.CharacterId == characterId, ct))
            throw new DomainRuleException("Этот персонаж уже участвует в кампании.");
        await EnsureMemberAsync(db, campaign, userId, ct);
        db.CampaignCharacters.Add(new CampaignCharacter
        {
            Id = Guid.NewGuid(), CampaignId = campaign.Id, CharacterId = characterId, PlayerUserId = userId,
        });
    }
}
