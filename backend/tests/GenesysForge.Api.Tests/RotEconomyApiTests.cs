using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

/// <summary>ROT-ECO-01: деньги считает сервер, а не клиент.</summary>
public class RotEconomyApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    /// <summary>Завершённый персонаж Core: стартовый бюджет уже не действует, тратится кошелёк.</summary>
    private async Task<(HttpClient Client, Guid Id, ItemDefDto Item)> CreateBuyerAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Торговец", GameSystem.GenesysCore,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        // Стартовые карманные деньги — бросок 1d100 (ROT-CRE-03), поэтому для проверок расчёта
        // кошелёк выставляется явно: иначе тест зависел бы от случайного броска.
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, null, null, null, Money: 1000), Json.Options);

        var item = reference.Items.First(i => i.Price is > 0 and <= 50);
        return (client, id, item);
    }

    private static async Task<Guid> BuyAsync(HttpClient client, Guid characterId, Guid itemDefId, int quantity = 1)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, quantity, ItemState.Carried), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    [Fact]
    public async Task PurchaseChargesTheListedPrice_NotAnAmountChosenByTheClient()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var before = await SheetAsync(client, id);

        await BuyAsync(client, id, item.Id, quantity: 2);

        var after = await SheetAsync(client, id);
        Assert.Equal(before.Money - item.Price * 2, after.Money);
    }

    [Fact]
    public async Task PriceOverrideWithoutAReason_IsRejected()
    {
        var (client, id, item) = await CreateBuyerAsync();

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(item.Id, 1, ItemState.Carried, PriceOverride: 1), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("trade.override_reason_required",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task FreeGrant_DoesNotTouchTheWallet()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var before = await SheetAsync(client, id);

        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(item.Id, 1, ItemState.Carried, Free: true), Json.Options);

        Assert.Equal(before.Money, (await SheetAsync(client, id)).Money);
    }

    [Theory]
    [InlineData(1, 25)]
    [InlineData(2, 50)]
    [InlineData(3, 75)]
    [InlineData(5, 75)]
    public async Task SaleProceeds_FollowTheCheckResult(int successes, int percent)
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);
        var afterBuy = await SheetAsync(client, id);

        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, NetSuccesses: successes), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, sell.StatusCode);

        var expected = item.Price * percent / 100;
        Assert.Equal(afterBuy.Money + expected, (await SheetAsync(client, id)).Money);
    }

    [Fact]
    public async Task FailedSaleCheck_SellsNothing()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);
        var afterBuy = await SheetAsync(client, id);

        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, NetSuccesses: 0), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, sell.StatusCode);
        Assert.Equal("trade.sale_failed",
            (await sell.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // Ни денег, ни предмета не тронуто.
        var after = await SheetAsync(client, id);
        Assert.Equal(afterBuy.Money, after.Money);
        Assert.Contains(after.Items, i => i.Id == itemId);
    }

    [Fact]
    public async Task DirectSaleWithoutAnyCheck_PaysTheListedPrice()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);
        var afterBuy = await SheetAsync(client, id);

        // Ни проверки, ни доли — просто продать предмет.
        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, sell.StatusCode);

        Assert.Equal(afterBuy.Money + item.Price, (await SheetAsync(client, id)).Money);
    }

    [Theory]
    [InlineData(25)]
    [InlineData(50)]
    [InlineData(100)]
    public async Task DirectSale_UsesTheChosenFractionOfTheListedPrice(int percent)
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);
        var afterBuy = await SheetAsync(client, id);

        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, Percent: percent), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, sell.StatusCode);

        var expected = item.Price * percent / 100;
        Assert.Equal(afterBuy.Money + expected, (await SheetAsync(client, id)).Money);
    }

    [Fact]
    public async Task DirectSale_StillCannotInventAnAmount()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);

        // Доля выше 100 % отклоняется: сумма всегда привязана к цене каталога.
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, Percent: 5000), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("trade.percent_invalid",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task NegotiatedPrice_PaysExactlyWhatWasAgreed()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id, quantity: 2);
        var afterBuy = await SheetAsync(client, id);

        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(2, PriceOverride: 500, OverrideReason: "сделка с гильдией"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, sell.StatusCode);

        // Договорная цена — это цена за штуку, доля к ней не применяется.
        Assert.Equal(afterBuy.Money + 1000, (await SheetAsync(client, id)).Money);
    }

    [Fact]
    public async Task NegotiatedPriceWithoutAReason_IsRejected()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);
        var afterBuy = await SheetAsync(client, id);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, PriceOverride: 500), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("trade.override_reason_required",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal(afterBuy.Money, (await SheetAsync(client, id)).Money);
    }

    [Fact]
    public async Task NegotiatedPrice_IsRecordedInHistoryWithItsReason()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);

        await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, PriceOverride: 300, OverrideReason: "выкуп у коллекционера"), Json.Options);

        var history = await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options);

        Assert.Contains(history!, e => e.Action == CharacterAuditAction.ItemSold && e.Summary.Contains("300"));
    }

    [Fact]
    public async Task BothSaleModesAtOnce_AreRejected()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, NetSuccesses: 2, Percent: 100), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("trade.sale_mode_ambiguous",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task ConditionMultiplierWithoutAReason_IsRejected()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var itemId = await BuyAsync(client, id, item.Id);

        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1, NetSuccesses: 3, ConditionMultiplier: 0.5), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, sell.StatusCode);
        Assert.Equal("trade.condition_reason_required",
            (await sell.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task InsufficientFunds_AreRejectedAtomically()
    {
        var (client, id, item) = await CreateBuyerAsync();
        var before = await SheetAsync(client, id);
        var unaffordable = before.Money / item.Price!.Value + 2;

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(item.Id, unaffordable, ItemState.Carried), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(before.Money, after.Money);
        Assert.Equal(before.Items.Count, after.Items.Count);
    }
}
