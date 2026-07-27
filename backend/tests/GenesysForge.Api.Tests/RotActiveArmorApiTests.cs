using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>ROT-CMB-02: выбор активной брони и её влияние на лист.</summary>
public class RotActiveArmorApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    /// <summary>Персонаж RoT с двумя надетыми бронями разного поглощения.</summary>
    private async Task<(HttpClient Client, Guid Id, Guid WeakItemId, Guid StrongItemId)> CreateWithTwoArmorsAsync()
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

        var weakId = await AddAsync(client, id, weak.Id);
        var strongId = await AddAsync(client, id, strong.Id);
        return (client, id, weakId, strongId);
    }

    private static async Task<Guid> AddAsync(HttpClient client, Guid characterId, Guid itemDefId)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, ItemState.Equipped), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static Task<HttpResponseMessage> SetActiveAsync(HttpClient client, Guid id, Guid? itemId) =>
        client.PutAsJsonAsync($"/api/characters/{id}/active-armor", new SetActiveArmorRequest(itemId), Json.Options);

    [Fact]
    public async Task FirstEquippedArmorBecomesActive_AndTheSecondDoesNotSwitchItSilently()
    {
        var (client, id, weakId, _) = await CreateWithTwoArmorsAsync();

        var sheet = await SheetAsync(client, id);

        Assert.Equal(weakId, sheet.ActiveArmorCharacterItemId);
        Assert.True(sheet.Items.Single(i => i.Id == weakId).IsActiveArmor);
    }

    [Fact]
    public async Task TwoArmors_DoNotStackSoak_AndSwitchingChangesItWithoutChangingLoad()
    {
        var (client, id, weakId, strongId) = await CreateWithTwoArmorsAsync();

        var withWeak = await SheetAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await SetActiveAsync(client, id, strongId)).StatusCode);
        var withStrong = await SheetAsync(client, id);

        Assert.True(withStrong.Derived.Soak > withWeak.Derived.Soak);
        // Обе брони остаются надетыми, поэтому переносимый вес не меняется.
        Assert.Equal(withWeak.Derived.EncumbranceLoad, withStrong.Derived.EncumbranceLoad);
        Assert.True(withStrong.Items.Single(i => i.Id == strongId).IsActiveArmor);
        Assert.False(withStrong.Items.Single(i => i.Id == weakId).IsActiveArmor);
    }

    [Fact]
    public async Task UnequippingTheActiveArmor_ClearsTheChoice_WithoutPickingTheNextOne()
    {
        var (client, id, weakId, _) = await CreateWithTwoArmorsAsync();

        var resp = await client.PatchAsJsonAsync($"/api/characters/{id}/items/{weakId}",
            new UpdateItemRequest(ItemState.Backpack, null), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Null(sheet.ActiveArmorCharacterItemId);
        Assert.DoesNotContain(sheet.Items, i => i.IsActiveArmor);
    }

    [Fact]
    public async Task ChoiceCanBeCleared_AndThenNoArmorProtectionApplies()
    {
        var (client, id, _, strongId) = await CreateWithTwoArmorsAsync();
        // Самая слабая броня может иметь поглощение 0, поэтому снимать выбор нужно с сильной.
        await SetActiveAsync(client, id, strongId);
        var withArmor = await SheetAsync(client, id);

        Assert.Equal(HttpStatusCode.NoContent, (await SetActiveAsync(client, id, null)).StatusCode);

        var without = await SheetAsync(client, id);
        Assert.Null(without.ActiveArmorCharacterItemId);
        Assert.True(without.Derived.Soak < withArmor.Derived.Soak);
    }

    [Fact]
    public async Task ForeignOrUnsuitableItem_IsRejectedAtomically()
    {
        var (client, id, weakId, _) = await CreateWithTwoArmorsAsync();

        var foreign = await SetActiveAsync(client, id, Guid.NewGuid());
        Assert.Equal(HttpStatusCode.BadRequest, foreign.StatusCode);
        Assert.Equal("armor.item_not_found",
            (await foreign.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // Выбор не изменился ни на шаг.
        Assert.Equal(weakId, (await SheetAsync(client, id)).ActiveArmorCharacterItemId);
    }

    [Fact]
    public async Task NonArmorItem_CannotBeMadeActive()
    {
        var (client, id, _, _) = await CreateWithTwoArmorsAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var gearId = await AddAsync(client, id, reference.Items.First(i => i.Kind == ItemKind.Gear).Id);

        var resp = await SetActiveAsync(client, id, gearId);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("armor.not_armor",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task ArmorThatIsNotWorn_CannotBeMadeActive()
    {
        var (client, id, weakId, strongId) = await CreateWithTwoArmorsAsync();
        await client.PatchAsJsonAsync($"/api/characters/{id}/items/{strongId}",
            new UpdateItemRequest(ItemState.Backpack, null), Json.Options);

        var resp = await SetActiveAsync(client, id, strongId);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("armor.not_equipped",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal(weakId, (await SheetAsync(client, id)).ActiveArmorCharacterItemId);
    }
}
