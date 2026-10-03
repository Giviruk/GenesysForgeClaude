using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Эти команды проверяют владельца лёгким <c>EXISTS</c> / узким запросом вместо загрузки всего
/// графа персонажа. Проверка обязана остаться: чужой пользователь получает «не найден», а у
/// персонажа ничего не меняется.
/// </summary>
public class CharacterOwnershipChecksTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterOwnershipChecksTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Owned", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    [Fact]
    public async Task Stranger_CannotChangeSomeoneElsesCharacter()
    {
        var (owner, id) = await CreateCharacterAsync();
        var injury = await owner.PostAsJsonAsync($"/api/characters/{id}/critical-injuries",
            new AddCriticalInjuryRequest(null, "Шрам", null, null, null));
        Assert.Equal(HttpStatusCode.Created, injury.StatusCode);
        var injuryId = (await SheetAsync(owner, id)).CriticalInjuries!.Single().Id;
        var before = await SheetAsync(owner, id);

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var attempts = new[]
        {
            await stranger.PostAsJsonAsync($"/api/characters/{id}/notes/",
                new SaveCharacterNoteRequest("Чужая", "заметка"), Json.Options),
            await stranger.PostAsJsonAsync($"/api/characters/{id}/critical-injuries",
                new AddCriticalInjuryRequest(null, "Подброшенное", null, null, null)),
            await stranger.DeleteAsync($"/api/characters/{id}/critical-injuries/{injuryId}"),
            await stranger.DeleteAsync($"/api/characters/{id}/share"),
            await stranger.PostAsJsonAsync($"/api/characters/{id}/xp-awards", new AwardXpRequest(50, null)),
        };
        foreach (var response in attempts)
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var after = await SheetAsync(owner, id);
        Assert.Equal(before.TotalXp, after.TotalXp);
        Assert.Equal(injuryId, after.CriticalInjuries!.Single().Id);
        var notes = (await owner.GetFromJsonAsync<List<CharacterNoteDto>>($"/api/characters/{id}/notes/", Json.Options))!;
        Assert.Empty(notes);
    }

    [Fact]
    public async Task CriticalInjury_OfAnotherCharacter_IsNotRemovedThroughThisOne()
    {
        // Крит-ранение ищется по паре (ранение, персонаж): владелец двух персонажей не снимет
        // ранение одного, подставив в маршрут другого.
        var (owner, first) = await CreateCharacterAsync();
        var reference = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await owner.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Second", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var second = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        await owner.PostAsJsonAsync($"/api/characters/{first}/critical-injuries",
            new AddCriticalInjuryRequest(null, "Перелом", null, null, null));
        var injuryId = (await SheetAsync(owner, first)).CriticalInjuries!.Single().Id;

        var wrongRoute = await owner.DeleteAsync($"/api/characters/{second}/critical-injuries/{injuryId}");
        Assert.Equal(HttpStatusCode.BadRequest, wrongRoute.StatusCode);
        Assert.Single((await SheetAsync(owner, first)).CriticalInjuries!);

        var rightRoute = await owner.DeleteAsync($"/api/characters/{first}/critical-injuries/{injuryId}");
        Assert.Equal(HttpStatusCode.NoContent, rightRoute.StatusCode);
        Assert.Empty((await SheetAsync(owner, first)).CriticalInjuries!);
    }

    [Fact]
    public async Task AwardXp_StillValidatesAgainstSpentXp()
    {
        // Узкий запрос UpdateQuery(needsXpValidation: true) должен давать те же проверки, что и
        // полный граф: штраф ниже потраченного XP отклоняется, обычная награда проходит.
        var (owner, id) = await CreateCharacterAsync();
        var before = await SheetAsync(owner, id);

        var tooBig = await owner.PostAsJsonAsync($"/api/characters/{id}/xp-awards",
            new AwardXpRequest(-(before.TotalXp - before.SpentXp) - 1, null));
        Assert.Equal(HttpStatusCode.BadRequest, tooBig.StatusCode);

        var award = await owner.PostAsJsonAsync($"/api/characters/{id}/xp-awards", new AwardXpRequest(25, "сессия"));
        Assert.Equal(HttpStatusCode.NoContent, award.StatusCode);
        Assert.Equal(before.TotalXp + 25, (await SheetAsync(owner, id)).TotalXp);
    }
}
