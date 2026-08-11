using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class TalentChoiceApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AnimalCompanion_RequiresAndPersistsApprovedCompanion()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Companion Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
            [career.CareerSkillNames[0]]));
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, 100, null, null));

        // Пирамида 3/2 открывает первый ранг таланта тира 3. Берём только таланты без выбора,
        // чтобы подготовка теста не подменяла проверяемый контракт.
        foreach (var tier in new[] { 1, 2 })
        {
            var count = tier == 1 ? 3 : 2;
            var fillers = reference.Talents.Where(t => t.Tier == tier && !t.IsRanked
                    && t.ChoiceKind == TalentChoiceKind.None
                    && string.IsNullOrEmpty(t.RequiresTalentCode)
                    && (t.ExcludesTalentCodes?.Count ?? 0) == 0)
                .Take(count).ToList();
            Assert.Equal(count, fillers.Count);
            foreach (var filler in fillers)
            {
                var response = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                    new BuyTalentRequest(filler.Id));
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        var companion = reference.Talents.Single(t => t.Name == "Animal Companion");
        Assert.Equal(TalentChoiceKind.AnimalCompanion, companion.ChoiceKind);
        async Task<NpcDetailDto> CreateCompanionAsync(string name, int silhouette)
        {
            var response = await client.PostAsJsonAsync("/api/npcs/", new NpcInput(
                name, GameSystem.GenesysCore, NpcKind.Minion, NpcRole.Skirmisher,
                "", "", 1, 3, 1, 2, 1, 1, 5, null, 1, 0, 0, silhouette, "",
                NpcVisibility.Private, null, [], [], [], [], [], ["animal"]));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        }

        var selectedCompanion = await CreateCompanionAsync("Серый сокол", 0);
        var tooLargeCompanion = await CreateCompanionAsync("Большой волк", 1);
        var before = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;

        var missing = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var afterMissing = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.SpentXp, afterMissing.SpentXp);
        Assert.DoesNotContain(afterMissing.Talents!, t => t.TalentDefId == companion.Id);

        var tooLarge = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id, Choices: [tooLargeCompanion.Id.ToString()]));
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        var afterTooLarge = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.SpentXp, afterTooLarge.SpentXp);
        Assert.DoesNotContain(afterTooLarge.Talents!, t => t.TalentDefId == companion.Id);

        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id, Choices: [selectedCompanion.Id.ToString()]));
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        var owned = Assert.Single(sheet.Talents!, t => t.TalentDefId == companion.Id);
        var choice = Assert.Single(owned.Choices!);
        Assert.Equal(TalentChoiceKind.AnimalCompanion, choice.Kind);
        Assert.Equal(selectedCompanion.Id.ToString(), choice.Value);
        Assert.Equal(selectedCompanion.Name, choice.DisplayName);
    }
}
