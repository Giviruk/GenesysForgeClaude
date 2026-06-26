using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>U-12: применение структурных стартовых навыков вида при создании персонажа.</summary>
public class CreateCharacterArchetypeTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CreateCharacterArchetypeTests(ApiFactory factory) => _factory = factory;

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static async Task<Guid> CreateOkAsync(HttpClient client, CreateCharacterRequest req)
    {
        var resp = await client.PostAsJsonAsync("/api/characters/", req, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return body!["id"];
    }

    [Fact]
    public async Task FixedStartingSkill_AppliedAsFreeRank_OnCreation()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        // Трудяга (Laborer) стартует с «Атлетика 1»; выборов навыков у него нет.
        var laborer = reference.Archetypes.First(a => a.NameRu == "Трудяга");
        var career = reference.Careers[0];

        var id = await CreateOkAsync(client,
            new CreateCharacterRequest("Трудяга-герой", GameSystem.GenesysCore, laborer.Id, career.Id, null));

        var sheet = await SheetAsync(client, id);
        var athletics = sheet.Skills.Single(s => s.Name == "Athletics");
        Assert.True(athletics.FreeRanks >= 1);
        Assert.True(athletics.Ranks >= 1);
        Assert.Equal(0, sheet.SpentXp); // стартовый ранг бесплатный
    }

    [Fact]
    public async Task ChoiceArchetype_AppliesPickedNonCareerSkills()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var human = reference.Archetypes.First(a => a.NameRu == "Обыватель"); // выбор: 2 некарьерных навыка
        var career = reference.Careers[0];
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name)).Take(2).Select(s => s.Name).ToList();

        var id = await CreateOkAsync(client, new CreateCharacterRequest("Обыватель-герой", GameSystem.GenesysCore,
            human.Id, career.Id, null, [new ArchetypeSkillChoice("any-noncareer", nonCareer)]));

        var sheet = await SheetAsync(client, id);
        foreach (var name in nonCareer)
            Assert.Contains(sheet.Skills, s => s.Name == name && s.FreeRanks >= 1 && s.Ranks >= 1);
        Assert.Equal(0, sheet.SpentXp);
    }

    [Fact]
    public async Task ChoiceArchetype_MissingChoice_Rejected()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var human = reference.Archetypes.First(a => a.NameRu == "Обыватель");

        var resp = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Без выбора", GameSystem.GenesysCore, human.Id, reference.Careers[0].Id, null),
            Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChoiceArchetype_WrongCount_Rejected()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var human = reference.Archetypes.First(a => a.NameRu == "Обыватель");
        var career = reference.Careers[0];
        var one = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name)).Take(1).Select(s => s.Name).ToList();

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest("Один навык",
            GameSystem.GenesysCore, human.Id, career.Id, null, [new ArchetypeSkillChoice("any-noncareer", one)]),
            Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChoiceArchetype_CareerSkillInNonCareerGroup_Rejected()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var human = reference.Archetypes.First(a => a.NameRu == "Обыватель");
        var career = reference.Careers[0];
        var nonCareer = reference.Skills.First(s => !career.CareerSkillNames.Contains(s.Name)).Name;
        var picks = new List<string> { career.CareerSkillNames[0], nonCareer }; // карьерный навык в группе any-noncareer

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest("Карьерный в выборе",
            GameSystem.GenesysCore, human.Id, career.Id, null, [new ArchetypeSkillChoice("any-noncareer", picks)]),
            Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
