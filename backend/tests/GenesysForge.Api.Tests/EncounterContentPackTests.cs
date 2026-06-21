using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Регрессия: добавление участника энкаунтера и записи Content Pack раньше падало 500
/// (DbUpdateConcurrencyException) из-за добавления в Include-коллекцию на InMemory-провайдере.
/// </summary>
public class EncounterContentPackTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public EncounterContentPackTests(ApiFactory factory) => _factory = factory;

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
    }

    [Fact]
    public async Task AddContentPackEntry_Succeeds()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campId = await CreateCampaignAsync(gm);

        var packResp = await gm.PostAsJsonAsync($"/api/campaigns/{campId}/content-packs/",
            new CreateContentPackRequest("Пак", "", GameSystem.RealmsOfTerrinoth), Json.Options);
        Assert.Equal(HttpStatusCode.Created, packResp.StatusCode);
        var pack = (await packResp.Content.ReadFromJsonAsync<ContentPackDetailDto>(Json.Options))!;

        var input = new ContentPackEntryInput(
            ContentType: ContentEntryType.HouseRule, ContentId: null, Title: "Дикая магия",
            AllowedState: AllowedState.Allowed, Category: HouseRuleCategory.Magic,
            SafeSummary: "", Source: "", PageRef: "", GmNotes: "", PlayerNotes: "", Tags: null);
        var resp = await gm.PostAsJsonAsync($"/api/content-packs/{pack.Id}/entries", input, Json.Options);

        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var detail = (await resp.Content.ReadFromJsonAsync<ContentPackDetailDto>(Json.Options))!;
        Assert.Contains(detail.Entries, e => e.Title == "Дикая магия");
    }
}
