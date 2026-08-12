using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Взять в руки и сломать можно не всё: у верёвки и провизии этих состояний нет.
/// </summary>
public class ItemUseAndDamageTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid Id)> CreateAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Носильщик", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, reference, id);
    }

    /// <summary>Обычная ноша: ни бонусов, ни помех — надевать нечего.</summary>
    private static ItemDefDto PlainGear(ReferenceResponse reference) => reference.Items.First(i =>
        i.Kind == ItemKind.Gear && i.Implement is null && i.Shard is null
        && i.EncumbranceThresholdBonus == 0 && i.SoakBonus == 0
        && i.MeleeDefense == 0 && i.RangedDefense == 0 && i.CheckModifiers.Count == 0
        && i.Purchasable);

    private static Task<HttpResponseMessage> AddAsync(
        HttpClient client, Guid id, Guid defId, ItemState state) =>
        client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(defId, 1, state, Free: true), Json.Options);

    [Fact]
    public async Task PlainGear_CannotBeEquipped_NeitherOnAddNorLater()
    {
        var (client, reference, id) = await CreateAsync();
        var rope = PlainGear(reference);

        var added = await AddAsync(client, id, rope.Id, ItemState.Equipped);
        Assert.Equal(HttpStatusCode.BadRequest, added.StatusCode);
        Assert.Equal("item.not_equippable",
            (await added.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // И через смену состояния уже лежащей вещи — тем же правилом.
        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, rope.Id, ItemState.Carried)).StatusCode);
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.ItemDefId == rope.Id);
        var moved = await client.PatchAsJsonAsync($"/api/characters/{id}/items/{item.Id}",
            new UpdateItemRequest(ItemState.Equipped, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, moved.StatusCode);
        Assert.Equal("item.not_equippable",
            (await moved.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        Assert.False(item.CanEquip);
        Assert.False(item.CanBeDamaged);
    }

    [Fact]
    public async Task GearThatDoesSomethingWhenWorn_StillEquips()
    {
        var (client, reference, id) = await CreateAsync();
        // Рюкзак поднимает порог веса только надетым — значит, надевается.
        var pack = reference.Items.First(i =>
            i.Kind == ItemKind.Gear && i.EncumbranceThresholdBonus > 0);

        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, pack.Id, ItemState.Equipped)).StatusCode);
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.ItemDefId == pack.Id);
        Assert.True(item.CanEquip);
        // Контейнер книга ломает отдельно: серьёзное повреждение снимает прибавку к порогу.
        Assert.True(item.CanBeDamaged);
    }

    [Fact]
    public async Task MagicImplement_CanBeDamaged_AffectsMagicChecks_AndCanBeRepaired()
    {
        var (client, reference, id) = await CreateAsync();
        var implement = reference.Items.First(i => i.Implement is not null);

        Assert.Equal(HttpStatusCode.Created,
            (await AddAsync(client, id, implement.Id, ItemState.Equipped)).StatusCode);
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.ItemDefId == implement.Id);
        Assert.True(item.CanBeDamaged);

        var minor = await client.PutAsJsonAsync($"/api/characters/{id}/items/{item.Id}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Minor, "повреждён в бою"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, minor.StatusCode);
        item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == item.Id);
        Assert.Equal(1, item.Implement!.DamageSetbackDice);
        Assert.Equal(0, item.Implement.DamageDifficultyIncrease);

        var moderate = await client.PutAsJsonAsync($"/api/characters/{id}/items/{item.Id}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Moderate), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, moderate.StatusCode);
        item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == item.Id);
        Assert.Equal(0, item.Implement!.DamageSetbackDice);
        Assert.Equal(1, item.Implement.DamageDifficultyIncrease);

        var major = await client.PutAsJsonAsync($"/api/characters/{id}/items/{item.Id}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Major), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, major.StatusCode);
        item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == item.Id);
        Assert.False(item.IsUsable);

        var repaired = await client.PostAsJsonAsync($"/api/characters/{id}/items/{item.Id}/repair",
            new RepairItemRequest(Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, repaired.StatusCode);
        item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == item.Id);
        Assert.Equal(ItemDamageState.Undamaged, item.DamageState);
        Assert.True(item.IsUsable);
    }

    [Fact]
    public async Task PlainGear_HasNoDamageState_AndRejectsRepair()
    {
        var (client, reference, id) = await CreateAsync();
        var rope = PlainGear(reference);
        await AddAsync(client, id, rope.Id, ItemState.Carried);
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.ItemDefId == rope.Id);

        var broken = await client.PutAsJsonAsync($"/api/characters/{id}/items/{item.Id}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Major, "порвалась"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, broken.StatusCode);
        Assert.Equal("item.not_breakable",
            (await broken.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var repaired = await client.PostAsJsonAsync($"/api/characters/{id}/items/{item.Id}/repair",
            new RepairItemRequest(), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, repaired.StatusCode);
        Assert.Equal("item.not_breakable",
            (await repaired.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }
}
