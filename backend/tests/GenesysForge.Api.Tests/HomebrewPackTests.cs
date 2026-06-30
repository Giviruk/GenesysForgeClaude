using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class HomebrewPackTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public HomebrewPackTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Import_Toggle_Export_And_SharedImport()
    {
        var owner = await _factory.CreateAuthorizedClientAsync();
        var document = new HomebrewPackExportDto(
            "genesysforge.homebrew-pack.v1",
            "Airships",
            "User-authored airship options.",
            GameSystem.GenesysCore,
            [new HomebrewSkillDto("airships.skill.sky-sailing", "Sky Sailing", "Небесное мореходство",
                CharacteristicType.Agility, SkillKind.General, "Operate airships.", "Operate airships.", "User")],
            null, null, null, null, null);

        var importedResponse = await owner.PostAsJsonAsync("/api/homebrew-packs/import", document, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importedResponse.StatusCode);
        var imported = (await importedResponse.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(1, imported.EntryCount);

        var reference = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var skill = Assert.Single(reference.Skills, s => s.Name == "Sky Sailing");

        var exported = (await owner.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{imported.Id}/export", Json.Options))!;
        Assert.Equal("genesysforge.homebrew-pack.v1", exported.Format);
        Assert.Equal("airships.skill.sky-sailing", exported.Skills![0].Code);

        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.PutAsJsonAsync($"/api/homebrew-packs/{imported.Id}/default",
                new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
        var hidden = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.DoesNotContain(hidden.Skills, s => s.Id == skill.Id);

        var builtIn = hidden;
        var created = await owner.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Pilot", GameSystem.GenesysCore, builtIn.Archetypes[0].Id, builtIn.Careers[0].Id, null),
            Json.Options);
        var characterId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        var hiddenForCharacter = (await owner.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/GenesysCore?characterId={characterId}", Json.Options))!;
        Assert.DoesNotContain(hiddenForCharacter.Skills, s => s.Id == skill.Id);

        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.PutAsJsonAsync($"/api/characters/{characterId}/homebrew-packs/{imported.Id}",
                new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        var visibleForCharacter = (await owner.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/GenesysCore?characterId={characterId}", Json.Options))!;
        Assert.Contains(visibleForCharacter.Skills, s => s.Id == skill.Id);

        var share = (await (await owner.PostAsync($"/api/homebrew-packs/{imported.Id}/share", null))
            .Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var importedSharedResponse = await stranger.PostAsync($"/api/homebrew-packs/shared/{share.Token}/import", null);
        Assert.Equal(HttpStatusCode.Created, importedSharedResponse.StatusCode);
        var strangerRef = (await stranger.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.Contains(strangerRef.Skills, s => s.Name == "Sky Sailing");
    }
}
