using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Регрессия: добавление участника энкаунтера раньше падало 500
/// (DbUpdateConcurrencyException) из-за добавления в Include-коллекцию на InMemory-провайдере.
/// </summary>
public class EncounterParticipantTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EncounterParticipantTests(ApiFactory factory) => _factory = factory;

    private static async Task<Guid> CreateCampaignAsync(HttpClient gm)
    {
        var resp = await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Кампания", "desc"), Json.Options);
        var c = (await resp.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        return c.Id;
    }

    private static EncounterInput SampleEncounter() => new(
        "Засада", GameSystem.RealmsOfTerrinoth, EncounterType.Combat, ThreatLevel.Standard,
        GmDescription: "", PlayerDescription: "", PlayerGoals: "", NpcGoals: "",
        Location: "", Environment: "", Complications: "", Rewards: "", IsVisibleToPlayers: true, Tags: null);

    [Fact]
    public async Task AddEncounterParticipant_Succeeds()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campId = await CreateCampaignAsync(gm);

        var encResp = await gm.PostAsJsonAsync($"/api/campaigns/{campId}/encounters/", SampleEncounter(), Json.Options);
        Assert.Equal(HttpStatusCode.Created, encResp.StatusCode);
        var enc = (await encResp.Content.ReadFromJsonAsync<EncounterDetailDto>(Json.Options))!;

        var req = new AddEncounterParticipantRequest(
            CharacterId: null, NpcId: null, DisplayName: "Гоблин", ParticipantType: ParticipantType.Npc,
            InitiativeSide: InitiativeSlotType.Npc, Quantity: 2, Notes: null,
            StartsHidden: null, StartsDefeated: null, StartingWoundsOverride: null, StartingStrainOverride: null);
        var resp = await gm.PostAsJsonAsync($"/api/encounters/{enc.Id}/participants", req, Json.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var detail = (await resp.Content.ReadFromJsonAsync<EncounterDetailDto>(Json.Options))!;
        Assert.Contains(detail.Participants, p => p.DisplayName == "Гоблин" && p.Quantity == 2);
        Assert.Null(Assert.Single(detail.Participants).NpcKind);
    }

    [Theory]
    [InlineData(NpcKind.Minion)]
    [InlineData(NpcKind.Rival)]
    [InlineData(NpcKind.Nemesis)]
    public async Task NpcKind_IsIncludedInMutationAndReadResponses(NpcKind kind)
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaignId = await CreateCampaignAsync(gm);
        var created = await gm.PostAsJsonAsync($"/api/campaigns/{campaignId}/encounters/", SampleEncounter(), Json.Options);
        created.EnsureSuccessStatusCode();
        var encounter = (await created.Content.ReadFromJsonAsync<EncounterDetailDto>(Json.Options))!;
        var npcInput = new NpcInput("UI kind regression", GameSystem.RealmsOfTerrinoth, kind, NpcRole.Brute, null, null,
            2, 2, 2, 2, 2, 2, 12, 10, 2, 0, 0, 1, null, NpcVisibility.Private, null, [], [], [], [], [], []);
        var npcResponse = await gm.PostAsJsonAsync("/api/npcs/", npcInput, Json.Options);
        npcResponse.EnsureSuccessStatusCode();
        var npc = (await npcResponse.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var input = new AddEncounterParticipantRequest(null, npc.Id, null, null, null, 1, null, null, null, null, null);
        var added = await gm.PostAsJsonAsync($"/api/encounters/{encounter.Id}/participants", input, Json.Options);
        added.EnsureSuccessStatusCode();
        var detail = (await added.Content.ReadFromJsonAsync<EncounterDetailDto>(Json.Options))!;
        Assert.Equal(kind, Assert.Single(detail.Participants).NpcKind);

        var read = await gm.GetFromJsonAsync<EncounterDetailDto>($"/api/encounters/{encounter.Id}", Json.Options);
        Assert.Equal(kind, Assert.Single(read!.Participants).NpcKind);
    }
}
