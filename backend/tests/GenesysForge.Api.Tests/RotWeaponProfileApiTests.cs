using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-WPN-01 на листе: базовый урон профиля считает сервер, щит прибавляет защиту к броне,
/// а метнутое оружие недоступно до возврата и не исчезает.
/// </summary>
public class RotWeaponProfileApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateCharacterAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Метатель", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<Guid> AddAsync(
        HttpClient client, Guid characterId, Guid itemDefId, ItemState state = ItemState.Equipped)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, state, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static Task<HttpResponseMessage> SetThrownAsync(
        HttpClient client, Guid characterId, Guid itemId, bool isThrown) =>
        client.PutAsJsonAsync($"/api/characters/{characterId}/items/{itemId}/thrown",
            new SetItemThrownRequest(isThrown), Json.Options);

    [Fact]
    public async Task ServerComputesBaseDamage_ForEveryProfileOfTheItem()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var dagger = reference.Items.Single(i => i.Name == "Dagger" && i.Kind == ItemKind.Weapon);
        await AddAsync(client, id, dagger.Id);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var item = sheet.Items.Single(i => i.ItemDefId == dagger.Id);

        var melee = item.AttackProfiles!.Single(p => p.IsDefault);
        var thrown = item.AttackProfiles!.Single(p => p.Code == "thrown");

        // Оба профиля — «Мощь +2», клиенту строку «+2» разбирать не нужно.
        Assert.Equal(brawn + 2, melee.BaseDamage);
        Assert.Equal(brawn + 2, thrown.BaseDamage);
        Assert.Equal("Ranged", thrown.SkillName);
        Assert.Equal(WeaponRange.Short, thrown.Range);
    }

    [Fact]
    public async Task ShieldAddsDefenseToArmor_InsteadOfCompetingWithIt()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);
        var shield = reference.Items.Single(i => i.Name == "Shield, Large" && i.Kind == ItemKind.Weapon);

        await AddAsync(client, id, plate.Id);
        var withArmor = await SheetAsync(client, id);
        await AddAsync(client, id, shield.Id);
        var withShield = await SheetAsync(client, id);

        // Латы дают Defense 1, большой щит — Defensive 2 и Deflection 2: 1 + 2, а не max(1, 2).
        Assert.Equal(1, withArmor.Derived.MeleeDefense);
        Assert.Equal(3, withShield.Derived.MeleeDefense);
        Assert.Equal(3, withShield.Derived.RangedDefense);
    }

    [Fact]
    public async Task ThrownWeapon_BecomesUnavailable_AndComesBackWhenPickedUp()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var axe = reference.Items.Single(i => i.Name == "Throwing Axe" && i.Kind == ItemKind.Weapon);
        var itemId = await AddAsync(client, id, axe.Id);

        var loadBefore = (await SheetAsync(client, id)).Derived.EncumbranceLoad;
        Assert.Equal(HttpStatusCode.NoContent, (await SetThrownAsync(client, id, itemId, true)).StatusCode);

        var thrown = await SheetAsync(client, id);
        var item = thrown.Items.Single(i => i.Id == itemId);
        Assert.True(item.IsThrown);
        // Топорик не исчез — он лежит у цели и просто не висит на персонаже.
        Assert.Equal(loadBefore - 1, thrown.Derived.EncumbranceLoad);

        Assert.Equal(HttpStatusCode.NoContent, (await SetThrownAsync(client, id, itemId, false)).StatusCode);
        var recovered = await SheetAsync(client, id);
        Assert.False(recovered.Items.Single(i => i.Id == itemId).IsThrown);
        Assert.Equal(loadBefore, recovered.Derived.EncumbranceLoad);
    }

    [Fact]
    public async Task WeaponWithoutAThrowingProfile_CannotBeThrown()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var greatsword = reference.Items.Single(i => i.Name == "Greatsword" && i.Kind == ItemKind.Weapon);
        var itemId = await AddAsync(client, id, greatsword.Id);

        var resp = await SetThrownAsync(client, id, itemId, true);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("weapon.profile.not_throwable", error!.ReasonCode);
    }

    [Fact]
    public async Task ThrownShield_StopsDefending_UntilItIsPickedUp()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var spear = reference.Items.Single(i => i.Name == "Spear, Light" && i.Kind == ItemKind.Weapon);
        var itemId = await AddAsync(client, id, spear.Id);

        // Лёгкое копьё даёт Defensive 1, пока оно в руках.
        Assert.Equal(1, (await SheetAsync(client, id)).Derived.MeleeDefense);

        Assert.Equal(HttpStatusCode.NoContent, (await SetThrownAsync(client, id, itemId, true)).StatusCode);
        Assert.Equal(0, (await SheetAsync(client, id)).Derived.MeleeDefense);
    }
}
