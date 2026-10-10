using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

/// <summary>Сборка детального DTO кампании с учётом роли запрашивающего (GM/игрок).</summary>
public static class CampaignMapper
{
    public static async Task<CampaignDetailDto> BuildDetailAsync(
        IAppDbContext db, Campaign campaign, Guid userId, CancellationToken ct)
    {
        var isGm = campaign.GmUserId == userId;

        // Сортируем по исходному полю до проекции: OrderBy после Select в record-DTO
        // не транслируется в SQL реляционным провайдером (Npgsql).
        var members = await db.CampaignCharacters.AsNoTracking()
            .Where(cc => cc.CampaignId == campaign.Id)
            .OrderBy(cc => cc.Character!.Name)
            .Select(cc => new CampaignMemberDto(
                cc.CharacterId,
                cc.Character!.Name,
                cc.Character.System,
                cc.Character.Archetype!.NameRu,
                cc.Character.Career!.NameRu,
                cc.PlayerUserId == userId,
                cc.Character.PortraitUrl,
                cc.PlayerUserId == userId
                    ? cc.Character.TotalXp - cc.Character.SpentXp
                    : null))
            .ToListAsync(ct);

        // GM видит все заметки; игрок — только общие (не приватные)
        var notes = await db.CampaignNotes.AsNoTracking()
            .Where(n => n.CampaignId == campaign.Id && (isGm || !n.IsPrivate))
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new CampaignNoteDto(n.Id, n.Title, n.Body, n.IsPrivate, n.CreatedAt, n.UpdatedAt))
            .ToListAsync(ct);

        var players = await (from member in db.CampaignMembers.AsNoTracking()
            join player in db.Users.AsNoTracking() on member.UserId equals player.Id
            where member.CampaignId == campaign.Id
            orderby player.DisplayName
            select new CampaignPlayerDto(player.Id, player.DisplayName, player.AvatarUrl,
                player.Id == userId, member.JoinedAt)).ToListAsync(ct);

        return new CampaignDetailDto(
            campaign.Id, campaign.Name, campaign.Description, isGm,
            isGm ? campaign.JoinCode : null,
            members, notes, players,
            await db.CampaignSystemSettings.Where(x => x.CampaignId == campaign.Id && !x.IsOpen).Select(x => x.System).ToListAsync(ct));
    }

    public static async Task<Campaign> GetAccessibleAsync(
        IAppDbContext db, Guid userId, Guid campaignId, CancellationToken ct)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new DomainRuleException("Кампания не найдена.", "campaign.not_accessible");
        // GM уже авторизован самой строкой кампании; запрос членства для него был лишним round trip.
        if (campaign.GmUserId != userId)
        {
            var isMember = await db.CampaignMembers.AnyAsync(
                m => m.CampaignId == campaignId && m.UserId == userId, ct);
            if (!isMember) throw new DomainRuleException("Кампания не найдена.", "campaign.not_accessible");
        }
        return campaign;
    }

    public static async Task<Campaign> GetAsGmAsync(
        IAppDbContext db, Guid userId, Guid campaignId, CancellationToken ct)
    {
        var campaign = await db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, ct)
            ?? throw new DomainRuleException("Кампания не найдена.", "campaign.not_accessible");
        if (campaign.GmUserId != userId)
            throw new DomainRuleException("Только мастер кампании может выполнять это действие.");
        return campaign;
    }
}
