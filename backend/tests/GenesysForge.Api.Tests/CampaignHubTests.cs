using System.Security.Claims;
using GenesysForge.Api.Realtime;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Tests;

public class CampaignHubTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"hub-{Guid.NewGuid():N}").Options);

    private sealed class FakeGroups : IGroupManager
    {
        public readonly List<string> Added = [];
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
        {
            Added.Add(groupName);
            return Task.CompletedTask;
        }
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class FakeCallerContext(Guid userId) : HubCallerContext
    {
        public override string ConnectionId => "conn-1";
        public override string? UserIdentifier => userId.ToString();
        public override ClaimsPrincipal? User =>
            new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId.ToString())], "test"));
        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();
        public override IFeatureCollection Features => throw new NotImplementedException();
        public override CancellationToken ConnectionAborted => CancellationToken.None;
        public override void Abort() { }
    }

    private static CampaignHub Hub(AppDbContext db, Guid userId, FakeGroups groups) =>
        new(db) { Context = new FakeCallerContext(userId), Groups = groups };

    private static Campaign NewCampaign(Guid gmUserId) =>
        new() { Id = Guid.NewGuid(), GmUserId = gmUserId, Name = "Кампания", JoinCode = "JOIN01" };

    [Fact]
    public async Task Gm_can_subscribe()
    {
        var db = NewDb();
        var gm = Guid.NewGuid();
        var campaign = NewCampaign(gm);
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var groups = new FakeGroups();
        await Hub(db, gm, groups).SubscribeCampaign(campaign.Id);

        Assert.Contains(CampaignHub.Group(campaign.Id), groups.Added);
    }

    [Fact]
    public async Task Member_can_subscribe()
    {
        var db = NewDb();
        var player = Guid.NewGuid();
        var campaign = NewCampaign(Guid.NewGuid());
        db.Campaigns.Add(campaign);
        db.CampaignCharacters.Add(new CampaignCharacter
        {
            Id = Guid.NewGuid(), CampaignId = campaign.Id, CharacterId = Guid.NewGuid(), PlayerUserId = player,
        });
        await db.SaveChangesAsync();

        var groups = new FakeGroups();
        await Hub(db, player, groups).SubscribeCampaign(campaign.Id);

        Assert.Contains(CampaignHub.Group(campaign.Id), groups.Added);
    }

    [Fact]
    public async Task Outsider_cannot_subscribe()
    {
        var db = NewDb();
        var campaign = NewCampaign(Guid.NewGuid());
        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();

        var groups = new FakeGroups();
        var hub = Hub(db, Guid.NewGuid(), groups); // не GM и не участник

        await Assert.ThrowsAsync<HubException>(() => hub.SubscribeCampaign(campaign.Id));
        Assert.Empty(groups.Added); // в группу не добавлен
    }
}
