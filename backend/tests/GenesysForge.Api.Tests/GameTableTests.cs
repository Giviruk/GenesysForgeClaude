using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class GameTableTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public GameTableTests(ApiFactory factory) => _factory = factory;

    private static async Task<Guid> CreateCharacterAsync(HttpClient client, string name = "Hero")
    {
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest(name, GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        return (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static async Task<CampaignDetailDto> CreateCampaignAsync(HttpClient gm)
    {
        var resp = await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("Кампания", "desc"), Json.Options);
        return (await resp.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
    }

    private static async Task<GameSessionDto> CreateSessionAsync(HttpClient gm, Guid campaignId)
    {
        var resp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/session",
            new CreateSessionRequest("Сцена 1", "ночь в лесу", 2, 1), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
    }

    private static async Task<(HttpClient gm, HttpClient player, Guid campaignId, Guid charId)> SetupCampaignWithPlayerAsync(ApiFactory factory)
    {
        var gm = await factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        var player = await factory.CreateAuthorizedClientAsync();
        var charId = await CreateCharacterAsync(player, "Лучник");
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, charId), Json.Options);
        return (gm, player, campaign.Id, charId);
    }

    [Fact]
    public async Task Gm_CreatesSession_OnlyOneActive()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        var session = await CreateSessionAsync(gm, campaign.Id);
        Assert.True(session.IsGm);
        Assert.True(session.IsActive);
        Assert.Equal(2, session.PlayerStoryPoints);

        // вторую активную создать нельзя
        var second = await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session",
            new CreateSessionRequest("Сцена 2", null, 0, 0), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Player_CannotCreateSession_ButCanView()
    {
        var (gm, player, campaignId, _) = await SetupCampaignWithPlayerAsync(_factory);
        var forbidden = await player.PostAsJsonAsync($"/api/campaigns/{campaignId}/session",
            new CreateSessionRequest("Сцена", null, 0, 0), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);

        await CreateSessionAsync(gm, campaignId);
        var view = (await player.GetFromJsonAsync<GameSessionDto>($"/api/campaigns/{campaignId}/session", Json.Options))!;
        Assert.False(view.IsGm);
        Assert.Null(view.GmNotes); // приватные заметки не отдаются игроку
    }

    [Fact]
    public async Task NonMember_CannotViewSession()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/campaigns/{campaign.Id}/session");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task AddParticipants_FromCharacter_AndNpc()
    {
        var (gm, _, campaignId, charId) = await SetupCampaignWithPlayerAsync(_factory);
        await CreateSessionAsync(gm, campaignId);

        var pcResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/session/participants",
            new AddParticipantRequest(charId, null, null, null, null, null, null, null, null, null, null), Json.Options);
        var s1 = (await pcResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var pc = Assert.Single(s1.Participants);
        Assert.Equal(ParticipantType.PlayerCharacter, pc.ParticipantType);
        Assert.True(pc.WoundsThreshold > 0); // скопирован из листа

        // создаём NPC и добавляем группой
        var npcResp = await gm.PostAsJsonAsync("/api/npcs/", new NpcInput(
            "Гоблин", GameSystem.GenesysCore, NpcKind.Minion, NpcRole.Skirmisher, null, null,
            2, 3, 2, 2, 2, 2, 5, null, 2, 0, 0, NpcVisibility.Private, null, null, null, null, null, null, null), Json.Options);
        var npc = (await npcResp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var grpResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/session/participants",
            new AddParticipantRequest(null, npc.Id, null, ParticipantType.MinionGroup, null, 3, null, null, null, null, null), Json.Options);
        var s2 = (await grpResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var group = s2.Participants.First(p => p.NpcId == npc.Id);
        Assert.Equal(ParticipantType.MinionGroup, group.ParticipantType);
        Assert.Equal(3, group.Count);
        Assert.Equal(15, group.WoundsThreshold); // 5 × 3
    }

    [Fact]
    public async Task StoryPoints_NeverNegative()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);

        var resp = await gm.PatchAsJsonAsync($"/api/campaigns/{campaign.Id}/session",
            new UpdateSessionRequest(null, null, null, "секрет", -5, 3, true), Json.Options);
        var s = (await resp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        Assert.Equal(0, s.PlayerStoryPoints); // клампится к 0
        Assert.Equal(3, s.GmStoryPoints);
        Assert.True(s.AllowPlayerEdits);
        Assert.Equal("секрет", s.GmNotes);
    }

    [Fact]
    public async Task NextTurn_AdvancesSlots_AndRound()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);

        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/slots",
            new AddSlotRequest(InitiativeSlotType.Player, null, null), Json.Options);
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/slots",
            new AddSlotRequest(InitiativeSlotType.Npc, null, null), Json.Options);

        var t1 = await Post<GameSessionDto>(gm, $"/api/campaigns/{campaign.Id}/session/next-turn");
        Assert.Equal(1, t1.CurrentTurnIndex);
        Assert.Equal(1, t1.CurrentRound);
        var t2 = await Post<GameSessionDto>(gm, $"/api/campaigns/{campaign.Id}/session/next-turn");
        Assert.Equal(0, t2.CurrentTurnIndex); // обернулись
        Assert.Equal(2, t2.CurrentRound);     // новый раунд
    }

    [Fact]
    public async Task PlayerEdit_OwnVitals_OnlyWhenAllowed()
    {
        var (gm, player, campaignId, charId) = await SetupCampaignWithPlayerAsync(_factory);
        await CreateSessionAsync(gm, campaignId);
        var addResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/session/participants",
            new AddParticipantRequest(charId, null, null, null, null, null, null, null, null, null, null), Json.Options);
        var s = (await addResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var pid = s.Participants.First(p => p.CharacterId == charId).Id;

        // по умолчанию запрещено
        var denied = await player.PatchAsJsonAsync($"/api/campaigns/{campaignId}/session/participants/{pid}",
            new UpdateParticipantRequest(null, 3, null, null, null, null, null, null, null, null, null, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, denied.StatusCode);

        // GM разрешает
        await gm.PatchAsJsonAsync($"/api/campaigns/{campaignId}/session",
            new UpdateSessionRequest(null, null, null, null, null, null, true), Json.Options);
        var ok = await player.PatchAsJsonAsync($"/api/campaigns/{campaignId}/session/participants/{pid}",
            new UpdateParticipantRequest(null, 3, null, null, null, null, null, null, null, null, null, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var updated = (await ok.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        Assert.Equal(3, updated.Participants.First(p => p.Id == pid).WoundsCurrent);
    }

    [Fact]
    public async Task HiddenParticipant_NotVisibleToPlayer()
    {
        var (gm, player, campaignId, _) = await SetupCampaignWithPlayerAsync(_factory);
        await CreateSessionAsync(gm, campaignId);
        var addResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/session/participants",
            new AddParticipantRequest(null, null, "Скрытый босс", ParticipantType.Npc, InitiativeSlotType.Npc, null, 15, 12, 4, 1, 1), Json.Options);
        var s = (await addResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var pid = s.Participants[0].Id;
        await gm.PatchAsJsonAsync($"/api/campaigns/{campaignId}/session/participants/{pid}",
            new UpdateParticipantRequest(null, null, null, null, null, null, null, null, null, null, IsHiddenFromPlayers: true, null, null), Json.Options);

        var playerView = (await player.GetFromJsonAsync<GameSessionDto>($"/api/campaigns/{campaignId}/session", Json.Options))!;
        Assert.DoesNotContain(playerView.Participants, p => p.Id == pid);

        var gmView = (await gm.GetFromJsonAsync<GameSessionDto>($"/api/campaigns/{campaignId}/session", Json.Options))!;
        Assert.Contains(gmView.Participants, p => p.Id == pid);
    }

    [Fact]
    public async Task ResetAndEnd_Session()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/participants",
            new AddParticipantRequest(null, null, "Волк", ParticipantType.Npc, null, null, 8, null, 2, 0, 0), Json.Options);

        var reset = await Post<GameSessionDto>(gm, $"/api/campaigns/{campaign.Id}/session/reset");
        Assert.Empty(reset.Participants);
        Assert.Equal(1, reset.CurrentRound);

        var end = await gm.DeleteAsync($"/api/campaigns/{campaign.Id}/session");
        Assert.Equal(HttpStatusCode.NoContent, end.StatusCode);
        var after = await gm.GetAsync($"/api/campaigns/{campaign.Id}/session");
        Assert.Equal(HttpStatusCode.NoContent, after.StatusCode); // активной сцены больше нет
    }

    [Fact]
    public async Task RemoveParticipant_AlsoFreesAssignedSlot()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);

        var addResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/participants",
            new AddParticipantRequest(null, null, "Волк", ParticipantType.Npc, null, null, 8, null, 2, 0, 0), Json.Options);
        var s = (await addResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var pid = s.Participants[0].Id;

        var slotResp = await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/slots",
            new AddSlotRequest(InitiativeSlotType.Npc, pid, null), Json.Options);
        var withSlot = (await slotResp.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        Assert.Equal(pid, withSlot.Slots[0].AssignedParticipantId);

        var del = await gm.DeleteAsync($"/api/campaigns/{campaign.Id}/session/participants/{pid}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var view = (await gm.GetFromJsonAsync<GameSessionDto>($"/api/campaigns/{campaign.Id}/session", Json.Options))!;
        Assert.Empty(view.Participants);
        Assert.Null(view.Slots[0].AssignedParticipantId); // слот освобождён
    }

    [Fact]
    public async Task RemoveSlot_NormalizesOrder()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = await CreateCampaignAsync(gm);
        await CreateSessionAsync(gm, campaign.Id);
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/slots",
            new AddSlotRequest(InitiativeSlotType.Player, null, null), Json.Options);
        var second = await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/session/slots",
            new AddSlotRequest(InitiativeSlotType.Npc, null, null), Json.Options);
        var s = (await second.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        var firstSlotId = s.Slots[0].Id;

        var del = await gm.DeleteAsync($"/api/campaigns/{campaign.Id}/session/slots/{firstSlotId}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var view = (await gm.GetFromJsonAsync<GameSessionDto>($"/api/campaigns/{campaign.Id}/session", Json.Options))!;
        Assert.Single(view.Slots);
        Assert.Equal(0, view.Slots[0].Order); // порядок перенормирован
    }

    private static async Task<T> Post<T>(HttpClient client, string url)
    {
        var resp = await client.PostAsync(url, null);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>(Json.Options))!;
    }
}
