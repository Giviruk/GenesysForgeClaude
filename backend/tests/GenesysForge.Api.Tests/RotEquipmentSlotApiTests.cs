using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-EQP-01 на листе: две руки и одна броня. До этого персонаж мог держать в руках четыре
/// двуручных меча и носить три доспеха — приложение считало это нормой.
/// </summary>
public class RotEquipmentSlotApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
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
            "Боец", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
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

    private static async Task<string?> ReasonAsync(HttpResponseMessage resp) =>
        (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode;

    private static ItemDefDto Item(ReferenceResponse reference, string name) =>
        reference.Items.Single(i => i.Name == name);

    [Fact]
    public async Task TwoLightWeapons_Fit_ButThirdDoesNot()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword");

        await AddAndGetIdAsync(client, id, sword.Id);
        await AddAndGetIdAsync(client, id, sword.Id);

        var third = await AddAsync(client, id, sword.Id);
        Assert.Equal(HttpStatusCode.BadRequest, third.StatusCode);
        Assert.Equal("equipment.hands_full", await ReasonAsync(third));
    }

    [Fact]
    public async Task SecondTwoHandedWeapon_IsRejected()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var greatsword = Item(reference, "Greatsword");

        await AddAndGetIdAsync(client, id, greatsword.Id);

        var second = await AddAsync(client, id, greatsword.Id);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.Equal("equipment.hands_full", await ReasonAsync(second));
    }

    [Fact]
    public async Task TwoHandedWeapon_LeavesNoRoomForALightOne()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        await AddAndGetIdAsync(client, id, Item(reference, "Greatsword").Id);

        var light = await AddAsync(client, id, Item(reference, "Dagger").Id);
        Assert.Equal(HttpStatusCode.BadRequest, light.StatusCode);
        Assert.Equal("equipment.hands_full", await ReasonAsync(light));
    }

    [Fact]
    public async Task Bow_CountsAsTwoHanded()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        await AddAndGetIdAsync(client, id, Item(reference, "Bow").Id);

        var dagger = await AddAsync(client, id, Item(reference, "Dagger").Id);
        Assert.Equal(HttpStatusCode.BadRequest, dagger.StatusCode);
        Assert.Equal("equipment.hands_full", await ReasonAsync(dagger));
    }

    [Fact]
    public async Task ArmorDoesNotOccupyHands_AndGearIsNotLimited()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        await AddAndGetIdAsync(client, id, Item(reference, "Plate").Id);
        await AddAndGetIdAsync(client, id, Item(reference, "Greatsword").Id);

        // Снаряжение рук не занимает. Берём то, что вообще можно надеть: рюкзак поднимает порог
        // веса именно надетым, а верёвка с рационом больше «в руки» не берутся.
        var gear = reference.Items.First(i =>
            i.Kind == ItemKind.Gear && i.EncumbranceThresholdBonus > 0);
        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, gear.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, gear.Id)).StatusCode);
    }

    [Fact]
    public async Task HandsFreeUp_AfterTheWeaponIsPutAway()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var greatswordId = await AddAndGetIdAsync(client, id, Item(reference, "Greatsword").Id);
        var daggerId = await AddAndGetIdAsync(client, id, Item(reference, "Dagger").Id, ItemState.Backpack);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.PatchAsJsonAsync(
            $"/api/characters/{id}/items/{daggerId}",
            new UpdateItemRequest(ItemState.Equipped, null), Json.Options)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync(
            $"/api/characters/{id}/items/{greatswordId}",
            new UpdateItemRequest(ItemState.Backpack, null), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PatchAsJsonAsync(
            $"/api/characters/{id}/items/{daggerId}",
            new UpdateItemRequest(ItemState.Equipped, null), Json.Options)).StatusCode);
    }

    [Fact]
    public async Task RejectedEquip_DoesNotChargeTheWallet()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        await AddAndGetIdAsync(client, id, Item(reference, "Greatsword").Id);

        var before = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        var sword = Item(reference, "Sword");
        var paid = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(sword.Id, 1, ItemState.Equipped), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, paid.StatusCode);

        var after = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.Money, after.Money);
        Assert.Equal(before.StartingPurchaseBudget, after.StartingPurchaseBudget);
        Assert.Equal(before.Items!.Count, after.Items!.Count);
    }

    [Fact]
    public async Task ThrownWeapon_FreesTheHand()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var daggerId = await AddAndGetIdAsync(client, id, Item(reference, "Dagger").Id);
        await AddAndGetIdAsync(client, id, Item(reference, "Dagger").Id);

        // Обе руки заняты кинжалами: третье оружие не берётся.
        Assert.Equal(HttpStatusCode.BadRequest,
            (await AddAsync(client, id, Item(reference, "Sword").Id)).StatusCode);

        // Метнутый кинжал лежит у цели и руки не занимает (ROT-WPN-01).
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/items/{daggerId}/thrown",
            new SetItemThrownRequest(true), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await AddAsync(client, id, Item(reference, "Sword").Id)).StatusCode);
    }
}
