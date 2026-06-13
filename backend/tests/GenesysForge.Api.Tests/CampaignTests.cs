using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class CampaignTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CampaignTests(ApiFactory factory) => _factory = factory;

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string name = "Hero")
    {
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest(name, GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        return (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static async Task<CampaignDetailDto> CreateCampaignAsync(HttpClient gm, string name = "Поход на север")
    {
        var resp = await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest(name, "описание"), Json.Options);
        return (await resp.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
    }

    [Fact]
    public async Task Gm_CreatesCampaign_GetsJoinCode()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        Assert.True(campaign.IsGm);
        Assert.False(string.IsNullOrEmpty(campaign.JoinCode));
        Assert.Empty(campaign.Members);
    }

    [Fact]
    public async Task Player_JoinsWithOwnCharacter_GmSeesMember()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);

        var player = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player, "Бард");
        var join = await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);
        Assert.Equal(HttpStatusCode.OK, join.StatusCode);

        var gmView = (await gm.GetFromJsonAsync<CampaignDetailDto>($"/api/campaigns/{campaign.Id}", Json.Options))!;
        Assert.Contains(gmView.Members, m => m.CharacterId == charId && m.CharacterName == "Бард");
    }

    [Fact]
    public async Task Player_CannotJoinWithForeignCharacter()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);

        var owner = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(owner, "Чужой");

        var attacker = await _factory.CreateAuthorizedClientAsync();
        var join = await attacker.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, join.StatusCode); // не его персонаж
    }

    [Fact]
    public async Task PrivateGmNotes_HiddenFromPlayers_SharedVisible()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/notes",
            new SaveCampaignNoteRequest("Секрет злодея", "Барон — вампир", IsPrivate: true), Json.Options);
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/notes",
            new SaveCampaignNoteRequest("Объявление", "Сбор в таверне", IsPrivate: false), Json.Options);

        // игрок присоединяется и видит только общую заметку
        var player = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);

        var playerView = (await player.GetFromJsonAsync<CampaignDetailDto>($"/api/campaigns/{campaign.Id}", Json.Options))!;
        Assert.Single(playerView.Notes);
        Assert.Equal("Объявление", playerView.Notes[0].Title);
        Assert.Null(playerView.JoinCode); // код игроку не показывается

        var gmView = (await gm.GetFromJsonAsync<CampaignDetailDto>($"/api/campaigns/{campaign.Id}", Json.Options))!;
        Assert.Equal(2, gmView.Notes.Count); // GM видит обе
    }

    [Fact]
    public async Task Player_CannotCreateCampaignNotes()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        var player = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);

        var resp = await player.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/notes",
            new SaveCampaignNoteRequest("Хочу", "заметку", IsPrivate: false), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // только GM
    }

    [Fact]
    public async Task NonMember_CannotViewCampaign()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/campaigns/{campaign.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CampaignList_ShowsForGmAndPlayer()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm, "Общая кампания");
        var player = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);

        var gmList = (await gm.GetFromJsonAsync<List<CampaignListItemDto>>("/api/campaigns/", Json.Options))!;
        Assert.Contains(gmList, c => c.Id == campaign.Id && c.IsGm && c.CharacterCount == 1);

        var playerList = (await player.GetFromJsonAsync<List<CampaignListItemDto>>("/api/campaigns/", Json.Options))!;
        Assert.Contains(playerList, c => c.Id == campaign.Id && !c.IsGm);
    }

    [Fact]
    public async Task Player_CanLeave_GmCanRemove()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        var player = await _factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);

        // игрок убирает своего персонажа
        var leave = await player.DeleteAsync($"/api/campaigns/{campaign.Id}/characters/{charId}");
        Assert.Equal(HttpStatusCode.NoContent, leave.StatusCode);
        var gmView = (await gm.GetFromJsonAsync<CampaignDetailDto>($"/api/campaigns/{campaign.Id}", Json.Options))!;
        Assert.Empty(gmView.Members);
    }
}
