using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-CMB-02 + ROT-EQP-01: одновременно носят ровно одну броню, поэтому надетая броня и есть
/// активная — выбирать не из чего. Раньше можно было надеть три доспеха и переключать «активный».
/// </summary>
public class RotActiveArmorApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    /// <summary>Персонаж RoT и две брони разного поглощения из справочника.</summary>
    private async Task<(HttpClient Client, Guid Id, ItemDefDto Weak, ItemDefDto Strong)> CreateAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Латник", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var armors = reference.Items.Where(i => i.Kind == ItemKind.Armor).OrderBy(i => i.SoakBonus).ToList();
        var weak = armors.First();
        var strong = armors.Last();
        Assert.True(strong.SoakBonus > weak.SoakBonus, "Нужны две брони с разным поглощением.");
        return (client, id, weak, strong);
    }

    private static Task<HttpResponseMessage> AddAsync(
        HttpClient client, Guid characterId, Guid itemDefId, ItemState state = ItemState.Equipped) =>
        client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, state, Free: true), Json.Options);

    private static async Task<Guid> AddAndGetIdAsync(
        HttpClient client, Guid characterId, Guid itemDefId, ItemState state = ItemState.Equipped)
    {
        var resp = await AddAsync(client, characterId, itemDefId, state);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static Task<HttpResponseMessage> SetStateAsync(
        HttpClient client, Guid id, Guid itemId, ItemState state) =>
        client.PatchAsJsonAsync($"/api/characters/{id}/items/{itemId}",
            new UpdateItemRequest(state, null), Json.Options);

    [Fact]
    public async Task WornArmor_IsTheActiveOne()
    {
        var (client, id, weak, _) = await CreateAsync();
        var weakId = await AddAndGetIdAsync(client, id, weak.Id);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(weakId, sheet.ActiveArmorCharacterItemId);
        Assert.True(sheet.Items.Single(i => i.Id == weakId).IsActiveArmor);
    }

    [Fact]
    public async Task SecondArmor_CannotBeWorn()
    {
        var (client, id, weak, strong) = await CreateAsync();
        await AddAndGetIdAsync(client, id, weak.Id);

        // Ни сразу «на себя»…
        var added = await AddAsync(client, id, strong.Id);
        Assert.Equal(HttpStatusCode.BadRequest, added.StatusCode);
        Assert.Equal("equipment.armor_limit",
            (await added.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // …ни надеванием той, что лежит в рюкзаке.
        var carriedId = await AddAndGetIdAsync(client, id, strong.Id, ItemState.Backpack);
        var worn = await SetStateAsync(client, id, carriedId, ItemState.Equipped);
        Assert.Equal(HttpStatusCode.BadRequest, worn.StatusCode);
        Assert.Equal("equipment.armor_limit",
            (await worn.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task ArmorSwaps_OnlyAfterTheFirstIsTakenOff()
    {
        var (client, id, weak, strong) = await CreateAsync();
        var weakId = await AddAndGetIdAsync(client, id, weak.Id);
        var strongId = await AddAndGetIdAsync(client, id, strong.Id, ItemState.Backpack);

        var withWeak = await SheetAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetStateAsync(client, id, weakId, ItemState.Backpack)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetStateAsync(client, id, strongId, ItemState.Equipped)).StatusCode);

        var withStrong = await SheetAsync(client, id);
        Assert.True(withStrong.Derived.Soak > withWeak.Derived.Soak);
        Assert.Equal(strongId, withStrong.ActiveArmorCharacterItemId);
        Assert.False(withStrong.Items.Single(i => i.Id == weakId).IsActiveArmor);
    }

    [Fact]
    public async Task UnequippingTheArmor_ClearsTheChoice_AndItsProtection()
    {
        var (client, id, _, strong) = await CreateAsync();
        var strongId = await AddAndGetIdAsync(client, id, strong.Id);
        var withArmor = await SheetAsync(client, id);

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetStateAsync(client, id, strongId, ItemState.Backpack)).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Null(sheet.ActiveArmorCharacterItemId);
        Assert.DoesNotContain(sheet.Items, i => i.IsActiveArmor);
        Assert.True(sheet.Derived.Soak < withArmor.Derived.Soak);
    }
}
