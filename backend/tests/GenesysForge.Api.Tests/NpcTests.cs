using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class NpcTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public NpcTests(ApiFactory factory) => _factory = factory;

    private static NpcInput SampleInput(string name = "Гоблин-разведчик", NpcKind kind = NpcKind.Rival) => new(
        name, GameSystem.RealmsOfTerrinoth, kind, NpcRole.Skirmisher, "Юркий враг", "homebrew",
        Brawn: 2, Agility: 3, Intellect: 2, Cunning: 3, Willpower: 2, Presence: 2,
        WoundThreshold: 12, StrainThreshold: kind == NpcKind.Minion ? null : 10,
        Soak: 3, MeleeDefense: 1, RangedDefense: 0,
        Visibility: NpcVisibility.Private, CampaignId: null,
        Skills: [new NpcSkillDto("Ближний бой", 2)],
        Abilities: [new NpcAbilityDto("Засада", "Добавляет преимущество при внезапной атаке")],
        Talents: ["Быстрый"], Equipment: ["Кинжал"], Tags: ["гоблин", "лес"]);

    [Fact]
    public async Task Create_Get_AppearsInLibrary()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var resp = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.True(npc.IsMine);
        Assert.Equal("Засада", npc.Abilities[0].Name);

        var list = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.Contains(list, n => n.Id == npc.Id && n.Name == "Гоблин-разведчик");
    }

    [Fact]
    public async Task Filters_BySystemAndKindAndSearch()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        await gm.PostAsJsonAsync("/api/npcs/", SampleInput("Орк-вожак", NpcKind.Nemesis), Json.Options);
        await gm.PostAsJsonAsync("/api/npcs/", SampleInput("Крыса", NpcKind.Minion) with { StrainThreshold = null }, Json.Options);

        var minions = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/?kind=minion", Json.Options))!;
        Assert.All(minions, n => Assert.Equal(NpcKind.Minion, n.Kind));

        var found = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/?search=орк", Json.Options))!;
        Assert.Contains(found, n => n.Name == "Орк-вожак");
        Assert.DoesNotContain(found, n => n.Name == "Крыса");
    }

    [Fact]
    public async Task QuickDraft_IsDeterministic_AndValid()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var req = new QuickDraftRequest(GameSystem.GenesysCore, NpcKind.Nemesis, NpcRole.Brute,
            NpcPowerLevel.Strong, null, NpcCombatStyle.Melee, null);

        var a = await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options);
        var b = await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options);
        var npcA = (await a.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var npcB = (await b.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        // одинаковые входы → одинаковые статы (детерминизм)
        Assert.Equal(npcA.Brawn, npcB.Brawn);
        Assert.Equal(npcA.Soak, npcB.Soak);
        Assert.Equal(npcA.WoundThreshold, npcB.WoundThreshold);
        Assert.NotNull(npcA.StrainThreshold); // Nemesis обязан иметь
        Assert.NotEmpty(npcA.Skills);
    }

    [Fact]
    public async Task Nemesis_WithoutStrain_IsRejected()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var bad = SampleInput("Босс", NpcKind.Nemesis) with { StrainThreshold = null };
        var resp = await gm.PostAsJsonAsync("/api/npcs/", bad, Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Duplicate_CreatesIndependentCopy()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var dup = await gm.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, dup.StatusCode);
        var copy = (await dup.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.NotEqual(npc.Id, copy.Id);
        Assert.Contains("копия", copy.Name);
        Assert.Equal(npc.Skills.Count, copy.Skills.Count);
    }

    [Fact]
    public async Task PrivateNpc_NotVisibleToOtherUsers()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // не найден

        var list = (await stranger.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.DoesNotContain(list, n => n.Id == npc.Id);
    }

    [Fact]
    public async Task Update_And_Delete_OwnerOnly()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var forbidden = await stranger.PutAsJsonAsync($"/api/npcs/{npc.Id}",
            SampleInput("Взлом"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);

        var updated = await gm.PutAsJsonAsync($"/api/npcs/{npc.Id}", SampleInput("Гоблин-ветеран"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = (await updated.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal("Гоблин-ветеран", detail.Name);

        var del = await gm.DeleteAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        var gone = await gm.GetAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, gone.StatusCode);
    }
}
