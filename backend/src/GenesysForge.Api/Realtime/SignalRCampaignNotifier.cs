using GenesysForge.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace GenesysForge.Api.Realtime;

/// <summary>Реализация уведомителя поверх SignalR: шлёт события в группу кампании.</summary>
public class SignalRCampaignNotifier(IHubContext<CampaignHub> hub) : ICampaignNotifier
{
    public Task GameTableChangedAsync(Guid campaignId) =>
        hub.Clients.Group(CampaignHub.Group(campaignId)).SendAsync("GameTableChanged", campaignId);

    public Task CampaignChangedAsync(Guid campaignId) =>
        hub.Clients.Group(CampaignHub.Group(campaignId)).SendAsync("CampaignChanged", campaignId);
}
