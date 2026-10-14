using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

public class NpcStrainTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static NpcInput Input(NpcKind kind) => new(
        "Strain regression", GameSystem.GenesysCore, kind, NpcRole.Brute, null, null,
        2, 2, 2, 2, 2, 2, 12, 10, 2, 0, 0, 1, null, NpcVisibility.Private, null,
        [], [], [], [], [], []);

    [Theory]
    [InlineData(NpcKind.Minion)]
    [InlineData(NpcKind.Rival)]
    public async Task CreateAndUpdate_NormalizeOldClientThresholds(NpcKind kind)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.PostAsJsonAsync("/api/npcs/", Input(kind), Json.Options);
        created.EnsureSuccessStatusCode();
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Null(npc.StrainThreshold);
        var updated = await client.PutAsJsonAsync($"/api/npcs/{npc.Id}", Input(kind) with { StrainThreshold = 27 }, Json.Options);
        updated.EnsureSuccessStatusCode();
        Assert.Null((await updated.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!.StrainThreshold);
        using var scope = factory.Services.CreateScope();
        Assert.Null((await scope.ServiceProvider.GetRequiredService<AppDbContext>().Npcs.SingleAsync(n => n.Id == npc.Id)).StrainThreshold);
    }

    [Theory]
    [InlineData(NpcKind.Minion)]
    [InlineData(NpcKind.Rival)]
    [InlineData(NpcKind.Nemesis)]
    public async Task LegacyThreshold_ReadCopyAndTable_FollowNpcKind(NpcKind kind)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var created = await client.PostAsJsonAsync("/api/npcs/", Input(kind), Json.Options);
        created.EnsureSuccessStatusCode();
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.Npcs.SingleAsync(n => n.Id == npc.Id)).StrainThreshold = 20;
            await db.SaveChangesAsync();
        }
        int? expected = kind == NpcKind.Nemesis ? 20 : null;
        Assert.Equal(expected, (await client.GetFromJsonAsync<NpcDetailDto>($"/api/npcs/{npc.Id}", Json.Options))!.StrainThreshold);
        var list = (await client.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.Equal(expected, list.Single(n => n.Id == npc.Id).StrainThreshold);
        var copied = await client.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        copied.EnsureSuccessStatusCode();
        Assert.Equal(expected, (await copied.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!.StrainThreshold);

        var campaignResponse = await client.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("Strain regression", ""), Json.Options);
        var campaign = (await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        (await client.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session", new CreateSessionRequest("Regression", "", 0, 0), Json.Options)).EnsureSuccessStatusCode();
        var added = await client.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/participants",
            new AddParticipantRequest(null, npc.Id, null, ParticipantType.Npc, null, 1, null, null, null, null, null), Json.Options);
        added.EnsureSuccessStatusCode();
        var participant = Assert.Single((await added.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!.Participants);
        Assert.Equal(expected, participant.StrainThreshold);
        var patched = await client.PatchAsJsonAsync($"/api/campaigns/{campaign.Id}/session/participants/{participant.Id}",
            new UpdateParticipantRequest(null, null, null, 7, 30, null, null, null, null, null, null, null, null, null), Json.Options);
        patched.EnsureSuccessStatusCode();
        var result = Assert.Single((await patched.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!.Participants);
        Assert.Equal(kind == NpcKind.Nemesis ? 30 : null, result.StrainThreshold);
        Assert.Equal(kind == NpcKind.Nemesis ? 7 : 0, result.StrainCurrent);
    }
}
