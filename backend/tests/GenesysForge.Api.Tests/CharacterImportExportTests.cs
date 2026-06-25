using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class CharacterImportExportTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterImportExportTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid CharacterId, ReferenceResponse Reference)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Экспортируемый", GameSystem.GenesysCore,
                reference.Archetypes[0].Id, reference.Careers[0].Id, null), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<CharacterExportDto> ExportAsync(HttpClient client, Guid id)
    {
        var resp = await client.GetAsync($"/api/characters/{id}/export");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<CharacterExportDto>(Json.Options))!;
    }

    [Fact]
    public async Task Export_ReturnsValidFormat()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var export = await ExportAsync(client, id);

        Assert.Equal("genesysforge.character.v1", export.Format);
        Assert.Equal("Экспортируемый", export.Character.Name);
        Assert.Equal(GameSystem.GenesysCore, export.Character.System);
        Assert.NotEmpty(export.Character.ArchetypeCode);
        Assert.Equal(6, export.Character.Characteristics.Count);
        Assert.Contains("brawn", export.Character.Characteristics.Keys);
    }

    [Fact]
    public async Task RoundTrip_Import_CreatesEquivalentNewCharacter()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Добавим предмет, чтобы проверить перенос инвентаря.
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(reference.Items[0].Id, 2, ItemState.Backpack), Json.Options);

        var export = await ExportAsync(client, id);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importResp.StatusCode);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Empty(result.Warnings);
        Assert.NotEqual(id, result.CharacterId); // создан новый персонаж

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{result.CharacterId}", Json.Options))!;
        Assert.Equal(export.Character.Name, sheet.Name);
        Assert.Equal(export.Character.System, sheet.System);
        Assert.Equal(export.Character.Characteristics["brawn"], sheet.Characteristics["brawn"]);
        Assert.Equal(export.Character.TotalXp, sheet.TotalXp);
        Assert.Contains(sheet.Items, i => i.Quantity == 2);
    }

    [Fact]
    public async Task Import_InvalidFormat_Rejected()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var export = await ExportAsync(client, id);
        var bad = export with { Format = "something.else.v9" };

        var resp = await client.PostAsJsonAsync("/api/characters/import", bad, Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Import_UnknownArchetype_Blocks()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var export = await ExportAsync(client, id);
        var broken = export with
        {
            Character = export.Character with { ArchetypeCode = "nope", ArchetypeName = "Несуществующий" },
        };

        var resp = await client.PostAsJsonAsync("/api/characters/import", broken, Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Import_UnknownSkill_SkippedWithWarning()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var export = await ExportAsync(client, id);
        var withGhost = export with
        {
            Character = export.Character with
            {
                Skills = [.. export.Character.Skills,
                    new CharacterSkillExport("ghost.code", "Призрачный навык", 3, false, 0)],
            },
        };

        var resp = await client.PostAsJsonAsync("/api/characters/import", withGhost, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var result = (await resp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(result.Warnings, w => w.Contains("Призрачный навык"));
    }

    [Fact]
    public async Task Preview_ReturnsSummaryWithoutCreating()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var export = await ExportAsync(client, id);

        var before = (await client.GetFromJsonAsync<List<CharacterListItemDto>>("/api/characters/", Json.Options))!.Count;

        var resp = await client.PostAsJsonAsync("/api/characters/import/preview", export, Json.Options);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var preview = (await resp.Content.ReadFromJsonAsync<ImportPreviewDto>(Json.Options))!;
        Assert.Equal(export.Character.Name, preview.Name);
        Assert.Equal(GameSystem.GenesysCore, preview.System);

        // Предпросмотр ничего не создаёт.
        var after = (await client.GetFromJsonAsync<List<CharacterListItemDto>>("/api/characters/", Json.Options))!.Count;
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task ForeignCharacter_CannotBeExported()
    {
        var (_, id, _) = await CreateCharacterAsync();
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/characters/{id}/export");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // «персонаж не найден»
    }
}
