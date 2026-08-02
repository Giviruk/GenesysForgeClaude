using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Одинаковые предметы складываются в одну строку, разные экземпляры остаются раздельными.
/// </summary>
public class InventoryStackingTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
            "Скупщик", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, reference, id);
    }

    private static Task<HttpResponseMessage> AddAsync(
        HttpClient client, Guid id, Guid defId, int quantity = 1,
        ItemState state = ItemState.Carried,
        WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Steel) =>
        client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(defId, quantity, state, Free: true, Craftsmanship: craftsmanship),
            Json.Options);

    [Fact]
    public async Task AddingTheSameItemTwice_RaisesTheCount_InsteadOfMakingASecondRow()
    {
        var (client, reference, id) = await CreateAsync();
        var rope = reference.Items.First(i => i.Kind == ItemKind.Gear && i.Purchasable);

        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, rope.Id, 2)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await AddAsync(client, id, rope.Id, 3)).StatusCode);

        var rows = (await SheetAsync(client, id)).Items.Where(i => i.ItemDefId == rope.Id).ToList();
        Assert.Single(rows);
        Assert.Equal(5, rows[0].Quantity);
    }

    [Fact]
    public async Task ItemsThatDifferAsInstances_StayInSeparateRows()
    {
        var (client, reference, id) = await CreateAsync();
        var sword = reference.Items.First(i =>
            i.Kind == ItemKind.Weapon && i.Purchasable && i.Price is > 0);

        await AddAsync(client, id, sword.Id, craftsmanship: WeaponCraftsmanship.Steel);
        // Другая работа — другой экземпляр: гномий меч не «второй такой же».
        await AddAsync(client, id, sword.Id, craftsmanship: WeaponCraftsmanship.Dwarven);
        // Другое состояние тоже: то, что в рюкзаке, не лежит в руках.
        await AddAsync(client, id, sword.Id, state: ItemState.Backpack);

        var rows = (await SheetAsync(client, id)).Items.Where(i => i.ItemDefId == sword.Id).ToList();
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.Equal(1, r.Quantity));
    }

    [Fact]
    public async Task EquippedItems_NeverStack_BecauseTheyOccupyHands()
    {
        var (client, reference, id) = await CreateAsync();
        var sword = reference.Items.First(i =>
            i.Kind == ItemKind.Weapon && i.Purchasable && i.Name == "Sword");

        await AddAsync(client, id, sword.Id, state: ItemState.Equipped);
        await AddAsync(client, id, sword.Id, state: ItemState.Equipped);

        // Два меча в руках — две занятые руки, а не «×2» в одной строке (ROT-EQP-01).
        var rows = (await SheetAsync(client, id)).Items.Where(i => i.ItemDefId == sword.Id).ToList();
        Assert.Equal(2, rows.Count);
    }

    [Fact]
    public async Task DamagedInstance_DoesNotSwallowAFreshPurchase()
    {
        var (client, reference, id) = await CreateAsync();
        var sword = reference.Items.First(i => i.Kind == ItemKind.Weapon && i.Purchasable);

        await AddAsync(client, id, sword.Id);
        var damaged = (await SheetAsync(client, id)).Items.Single(i => i.ItemDefId == sword.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/items/{damaged.Id}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Moderate, "затупился"), Json.Options)).StatusCode);

        await AddAsync(client, id, sword.Id);

        var rows = (await SheetAsync(client, id)).Items.Where(i => i.ItemDefId == sword.Id).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, r => r.DamageState == ItemDamageState.Moderate);
        Assert.Contains(rows, r => r.DamageState == ItemDamageState.Undamaged);
    }
}
