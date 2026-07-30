using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Покупка, выдача и продажа скакунов (ROT-MOUNT-ITEM-01). Проверяется главное: скакун — существо
/// со статблоком, а не позиция инвентаря, и в переносимый вес владельца он не входит.
/// </summary>
public class RotMountApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, Guid CharacterId, ReferenceResponse Reference)> CreateRiderAsync(
        int money = 5000)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Всадник", GameSystem.RealmsOfTerrinoth,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, null, null, null, Money: money), Json.Options);
        return (client, id, reference);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static MountDefDto Mount(ReferenceResponse reference, string bareCode) =>
        reference.Mounts!.Single(m => m.Code == $"rot.mount.{bareCode}");

    private static int Funds(CharacterSheetDto sheet) => sheet.Money + sheet.StartingPurchaseBudget;

    [Fact]
    public async Task ReferenceServesFourMountProfilesWithStatblocks()
    {
        var (_, _, reference) = await CreateRiderAsync();

        Assert.Equal(4, reference.Mounts!.Count);
        var flying = Mount(reference, "flying-mount");
        Assert.Equal(2000, flying.Price);
        Assert.Equal(12, flying.WoundThreshold);
        Assert.Equal(12, flying.Capacity);
        Assert.Equal("Flyer", Assert.Single(flying.Abilities).Name);
        Assert.Equal(5, Assert.Single(flying.Attacks).Damage);
    }

    /// <summary>Старые записи скакунов-снаряжения выведены из активной витрины.</summary>
    [Fact]
    public async Task MountsAreNoLongerOfferedAsInventoryGear()
    {
        var (_, _, reference) = await CreateRiderAsync();

        Assert.DoesNotContain(
            reference.Items,
            i => i.Code.EndsWith("war-mount", StringComparison.Ordinal)
                || i.Code.EndsWith("flying-mount", StringComparison.Ordinal)
                || i.Code.EndsWith("beast-of-burden", StringComparison.Ordinal)
                || i.Code.EndsWith("riding-beast", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BuyingMountChargesCatalogPriceAndCreatesCreatureNotItem()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var def = Mount(reference, "war-mount");
        var before = await SheetAsync(client, id);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id), Json.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(Funds(before) - 1500, Funds(after));
        Assert.Equal(before.Items.Count, after.Items.Count);

        var mount = Assert.Single(after.Mounts!);
        Assert.Equal("War Mount", mount.Definition.Name);
        Assert.Equal(13, mount.Capacity);
        Assert.Equal(0, mount.WoundsCurrent);
        Assert.False(mount.IsIncapacitated);
        // Скакун не предмет: переносимый вес владельца от покупки не меняется.
        Assert.Equal(before.Derived.EncumbranceLoad, after.Derived.EncumbranceLoad);
    }

    [Fact]
    public async Task FreeGrantDoesNotTouchMoney()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var def = Mount(reference, "flying-mount");
        var before = await SheetAsync(client, id);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id, Free: true, Name: "Ветер"), Json.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(Funds(before), Funds(after));
        var mount = Assert.Single(after.Mounts!);
        Assert.Equal("Ветер", mount.Name);
        Assert.Equal("Ветер", mount.DisplayName);
        Assert.Equal(ItemProvenance.Imported, mount.Provenance);
    }

    [Fact]
    public async Task HagglePercentAndOwnPriceUseTheSameRulesAsItems()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var def = Mount(reference, "riding-beast");
        var start = await SheetAsync(client, id);

        var haggled = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id, PricePercent: 75), Json.Options);
        Assert.Equal(HttpStatusCode.Created, haggled.StatusCode);
        var afterHaggle = await SheetAsync(client, id);
        Assert.Equal(Funds(start) - 300, Funds(afterHaggle));

        var own = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id, PriceOverride: 50, OverrideReason: "подарок конюха"), Json.Options);
        Assert.Equal(HttpStatusCode.Created, own.StatusCode);
        Assert.Equal(Funds(afterHaggle) - 50, Funds(await SheetAsync(client, id)));
    }

    [Fact]
    public async Task OwnPriceWithoutReasonIsRejectedBeforeAnyCharge()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var def = Mount(reference, "riding-beast");
        var before = await SheetAsync(client, id);

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id, PriceOverride: 10), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("trade.override_reason_required",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(Funds(before), Funds(after));
        Assert.Empty(after.Mounts!);
    }

    [Fact]
    public async Task PurchaseIsAtomicWhenFundsAreShort()
    {
        var (client, id, reference) = await CreateRiderAsync(money: 100);
        var def = Mount(reference, "flying-mount");

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("character.funds.insufficient",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        var after = await SheetAsync(client, id);
        Assert.Empty(after.Mounts!);
        Assert.Equal(100, after.Money);
    }

    [Fact]
    public async Task SellingByCheckPaysTheBookFraction()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var def = Mount(reference, "war-mount");
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(def.Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var before = await SheetAsync(client, id);

        // Два нетто-успеха — 50 % цены каталога.
        var response = await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{owned.Id}/sell",
            new SellMountRequest(NetSuccesses: 2), Json.Options);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(Funds(before) + 750, Funds(after));
        Assert.Empty(after.Mounts!);
    }

    [Fact]
    public async Task FailedSaleCheckKeepsTheMount()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);

        var response = await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{owned.Id}/sell",
            new SellMountRequest(NetSuccesses: 0), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("trade.sale_failed",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Single((await SheetAsync(client, id)).Mounts!);
    }

    [Fact]
    public async Task LoadedMountCannotBeSoldUntilItIsUnloaded()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);
        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{owned.Id}",
            new UpdateMountRequest(CarriedLoad: 6), Json.Options);

        var blocked = await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{owned.Id}/sell", new SellMountRequest(), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("mount.load_not_empty",
            (await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{owned.Id}",
            new UpdateMountRequest(CarriedLoad: 0), Json.Options);
        var sold = await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{owned.Id}/sell", new SellMountRequest(), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, sold.StatusCode);
    }

    [Fact]
    public async Task StateUpdatesStayInsideProfileLimits()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);

        // Порог ран профиля 7: 40 ран не хранится, а приводится к порогу.
        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{owned.Id}",
            new UpdateMountRequest(WoundsCurrent: 40, CarriedLoad: 20, IsActive: true), Json.Options);

        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.Equal(7, mount.WoundsCurrent);
        Assert.True(mount.IsIncapacitated);
        // Груз выше вместимости 18 сохраняется, но помечается перегрузом.
        Assert.Equal(20, mount.CarriedLoad);
        Assert.True(mount.IsOverloaded);
        Assert.True(mount.IsActive);

        var negative = await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{owned.Id}",
            new UpdateMountRequest(CarriedLoad: -1), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, negative.StatusCode);
    }

    [Fact]
    public async Task RemovingMountGivesNoProceeds()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var before = await SheetAsync(client, id);

        var response = await client.DeleteAsync($"/api/characters/{id}/mounts/{owned.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Empty(after.Mounts!);
        Assert.Equal(Funds(before), Funds(after));
    }

    [Fact]
    public async Task AnotherOwnerCannotTouchTheMount()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true), Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);

        var stranger = await factory.CreateAuthorizedClientAsync();
        var response = await stranger.DeleteAsync($"/api/characters/{id}/mounts/{owned.Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Single((await SheetAsync(client, id)).Mounts!);
    }

    [Fact]
    public async Task ExportAndImportKeepMountWithItsState()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "flying-mount").Id, Free: true, Name: "Гроза"),
            Json.Options);
        var owned = Assert.Single((await SheetAsync(client, id)).Mounts!);
        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{owned.Id}",
            new UpdateMountRequest(WoundsCurrent: 4, CarriedLoad: 3, IsActive: true, Notes: "хромает"),
            Json.Options);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(CharacterExportDto.CurrentFormat, export.Format);
        var exported = Assert.Single(export.Character.Mounts!);
        Assert.Equal("rot.mount.flying-mount", exported.Code);
        Assert.Equal("Гроза", exported.CustomName);
        Assert.Equal(4, exported.WoundsCurrent);

        var import = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        Assert.Equal(HttpStatusCode.Created, import.StatusCode);
        var imported = (await import.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        var importedSheet = await SheetAsync(client, imported.CharacterId);

        var mount = Assert.Single(importedSheet.Mounts!);
        Assert.Equal("Гроза", mount.Name);
        Assert.Equal("Flying Mount", mount.Definition.Name);
        Assert.Equal(4, mount.WoundsCurrent);
        Assert.Equal(3, mount.CarriedLoad);
        Assert.True(mount.IsActive);
        Assert.Equal("хромает", mount.Notes);
    }

    [Fact]
    public async Task DuplicateCarriesMountsOverToTheClone()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true, Name: "Уголь"),
            Json.Options);

        var duplicate = await client.PostAsync($"/api/characters/{id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, duplicate.StatusCode);
        var cloneId = (await duplicate.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var clone = Assert.Single((await SheetAsync(client, cloneId)).Mounts!);
        Assert.Equal("Уголь", clone.Name);
        Assert.Equal("War Mount", clone.Definition.Name);
    }
}
