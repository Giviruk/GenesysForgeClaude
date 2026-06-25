using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class RollLogTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RollLogTests(ApiFactory factory) => _factory = factory;

    private const string Pool = "{\"ability\":2,\"proficiency\":1}";
    private const string Result = "{\"success\":2,\"advantage\":1}";

    private static async Task<(HttpClient gm, HttpClient player, Guid campaignId)> SetupAsync(ApiFactory factory)
    {
        var gm = await factory.CreateAuthorizedClientAsync();
        var campResp = await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Кампания", "desc"), Json.Options);
        var campaign = (await campResp.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;

        var player = await factory.CreateAuthorizedClientAsync();
        var reference = (await player.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await player.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Лучник", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var charId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);

        return (gm, player, campaign.Id);
    }

    [Fact]
    public async Task Member_CanRoll_AndSeeItInLog()
    {
        var (_, player, campaignId) = await SetupAsync(_factory);

        var resp = await player.PostAsJsonAsync($"/api/campaigns/{campaignId}/rolls",
            new CreateRollRequest("Лучник", "Стрельба", Pool, Result, "2 успеха, 1 преимущество", false), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var entry = (await resp.Content.ReadFromJsonAsync<RollLogEntryDto>(Json.Options))!;
        Assert.Equal("Лучник", entry.ActorName);
        Assert.False(entry.IsSecret);

        var log = (await player.GetFromJsonAsync<List<RollLogEntryDto>>($"/api/campaigns/{campaignId}/rolls", Json.Options))!;
        Assert.Single(log);
        Assert.Equal("Стрельба", log[0].Label);
    }

    [Fact]
    public async Task SecretRoll_HiddenFromPlayer_VisibleToGm()
    {
        var (gm, player, campaignId) = await SetupAsync(_factory);

        var resp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/rolls",
            new CreateRollRequest("Мастер", "тайный бросок", Pool, Result, null, true), Json.Options);
        var entry = (await resp.Content.ReadFromJsonAsync<RollLogEntryDto>(Json.Options))!;
        Assert.True(entry.IsSecret);

        var playerLog = (await player.GetFromJsonAsync<List<RollLogEntryDto>>($"/api/campaigns/{campaignId}/rolls", Json.Options))!;
        Assert.Empty(playerLog); // игрок не видит секретный бросок

        var gmLog = (await gm.GetFromJsonAsync<List<RollLogEntryDto>>($"/api/campaigns/{campaignId}/rolls", Json.Options))!;
        Assert.Single(gmLog);
    }

    [Fact]
    public async Task PlayerCannotMakeSecretRoll_FlagIgnored()
    {
        var (_, player, campaignId) = await SetupAsync(_factory);

        var resp = await player.PostAsJsonAsync($"/api/campaigns/{campaignId}/rolls",
            new CreateRollRequest("Лучник", null, Pool, Result, null, true), Json.Options); // просит секрет
        var entry = (await resp.Content.ReadFromJsonAsync<RollLogEntryDto>(Json.Options))!;
        Assert.False(entry.IsSecret); // флаг проигнорирован — игрок не может секретить
    }

    [Fact]
    public async Task NonMember_CannotRollOrRead()
    {
        var (_, _, campaignId) = await SetupAsync(_factory);
        var stranger = await _factory.CreateAuthorizedClientAsync();

        var post = await stranger.PostAsJsonAsync($"/api/campaigns/{campaignId}/rolls",
            new CreateRollRequest("Чужак", null, Pool, Result, null, false), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, post.StatusCode);

        var get = await stranger.GetAsync($"/api/campaigns/{campaignId}/rolls");
        Assert.Equal(HttpStatusCode.BadRequest, get.StatusCode);
    }

    [Fact]
    public async Task EmptyRoll_IsRejected()
    {
        var (gm, _, campaignId) = await SetupAsync(_factory);
        var resp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/rolls",
            new CreateRollRequest("Мастер", null, "", "", null, false), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
