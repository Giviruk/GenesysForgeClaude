using GenesysForge.Api.Endpoints;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GenesysForge.Api.Realtime;

/// <summary>
/// SignalR-хаб кампаний. Подключение требует тот же JWT, что и REST. Подписка на события
/// кампании возможна только после проверки доступа (GM или участник) — посторонние не могут
/// подписаться на чужую кампанию. События — «тонкие»: клиент по ним перечитывает REST.
/// </summary>
[Authorize]
public class CampaignHub(IAppDbContext db) : Hub
{
    public static string Group(Guid campaignId) => $"campaign:{campaignId}";

    /// <summary>Подписаться на события кампании после проверки доступа.</summary>
    public async Task SubscribeCampaign(Guid campaignId)
    {
        var userId = Context.User!.UserId();
        try
        {
            // Бросает, если пользователь не GM и не участник кампании.
            await CampaignMapper.GetAccessibleAsync(db, userId, campaignId, Context.ConnectionAborted);
        }
        catch (DomainRuleException)
        {
            throw new HubException("Нет доступа к кампании.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, Group(campaignId), Context.ConnectionAborted);
    }

    /// <summary>Отписаться от событий кампании.</summary>
    public Task UnsubscribeCampaign(Guid campaignId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(campaignId), Context.ConnectionAborted);
}
