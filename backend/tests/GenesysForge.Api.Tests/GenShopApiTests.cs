using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class GenShopApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, Guid CharacterId, ReferenceResponse Reference)> CreateBuyerAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Покупатель", GameSystem.RealmsOfTerrinoth,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, null, null, null, Money: 100), Json.Options);
        return (client, id, reference);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    [Fact]
    public async Task BuyingServiceChargesMoneyButNeverCreatesInventoryItem()
    {
        var (client, id, reference) = await CreateBuyerAsync();
        var service = reference.Items.First(i => i.ShopCategory == ShopItemCategory.Service && i.Price > 0);
        var before = await SheetAsync(client, id);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/services",
            new BuyServiceRequest(service.Id, Quantity: 2), Json.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(
            before.Money + before.StartingPurchaseBudget - service.Price * 2,
            after.Money + after.StartingPurchaseBudget);
        Assert.Equal(before.Items!.Count, after.Items!.Count);
    }

    [Fact]
    public async Task FreeServiceGrantDoesNotTouchMoneyOrInventory()
    {
        var (client, id, reference) = await CreateBuyerAsync();
        var service = reference.Items.First(i => i.ShopCategory == ShopItemCategory.Service);
        var before = await SheetAsync(client, id);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/services",
            new BuyServiceRequest(service.Id, Quantity: 3, Free: true), Json.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(before.Money, after.Money);
        Assert.Equal(before.Items!.Count, after.Items!.Count);
    }

    [Fact]
    public async Task ServiceCannotBeAddedThroughInventoryEndpoint()
    {
        var (client, id, reference) = await CreateBuyerAsync();
        var service = reference.Items.First(i => i.ShopCategory == ShopItemCategory.Service);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(service.Id, 1, ItemState.Carried, Free: true), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("service.not_inventory",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task PhysicalItemCannotBeSentToServiceEndpoint()
    {
        var (client, id, reference) = await CreateBuyerAsync();
        var item = reference.Items.First(i => i.ShopCategory == ShopItemCategory.Gear);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/services",
            new BuyServiceRequest(item.Id), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("service.definition_required",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }
}
