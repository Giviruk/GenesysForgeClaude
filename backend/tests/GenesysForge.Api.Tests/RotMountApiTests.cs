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

    private static MountDefDto Vehicle(ReferenceResponse reference, string bareCode) =>
        reference.Mounts!.Single(m => m.Code == $"rot.vehicle.{bareCode}");

    private static int Funds(CharacterSheetDto sheet) => sheet.Money + sheet.StartingPurchaseBudget;

    /// <summary>Кладёт предмет каталога в инвентарь и возвращает id позиции.</summary>
    private static async Task<Guid> AddItemAsync(
        HttpClient client, Guid id, ReferenceResponse reference, string bareCode, int quantity = 1)
    {
        var def = reference.Items.Single(i => i.Code == $"rot.item.{bareCode}");
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(def.Id, quantity, ItemState.Backpack, Free: true), Json.Options);
        var sheet = await SheetAsync(client, id);
        return sheet.Items!.Last(i => i.ItemDefId == def.Id).Id;
    }

    private static Task<HttpResponseMessage> MoveAsync(
        HttpClient client, Guid id, Guid itemId, MoveCargoRequest request) =>
        client.PatchAsJsonAsync(
            $"/api/characters/{id}/items/{itemId}/location", request, Json.Options);

    [Fact]
    public async Task ReferenceServesFourMountProfilesWithStatblocks()
    {
        var (_, _, reference) = await CreateRiderAsync();

        // Четыре скакуна книги плюс повозка одного каталога транспорта (ROT-TRANSPORT-01).
        Assert.Equal(5, reference.Mounts!.Count);
        Assert.Equal(4, reference.Mounts.Count(m => m.TransportKind == TransportKind.Mount));
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
        Assert.Equal(before.Items!.Count, after.Items!.Count);

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
        var cargo = await AddItemAsync(client, id, reference, "bedroll");
        await MoveAsync(client, id, cargo, new MoveCargoRequest(owned.Id));

        var blocked = await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{owned.Id}/sell", new SellMountRequest(), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("mount.load_not_empty",
            (await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        await MoveAsync(client, id, cargo, new MoveCargoRequest(null));
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
            new UpdateMountRequest(WoundsCurrent: 40, IsActive: true), Json.Options);

        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.Equal(7, mount.WoundsCurrent);
        Assert.True(mount.IsIncapacitated);
        Assert.True(mount.IsActive);
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
            new UpdateMountRequest(WoundsCurrent: 4, IsActive: true, Notes: "хромает"),
            Json.Options);
        var cargo = await AddItemAsync(client, id, reference, "bedroll");
        await MoveAsync(client, id, cargo, new MoveCargoRequest(owned.Id));

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
        Assert.True(mount.IsActive);
        Assert.Equal("хромает", mount.Notes);
        // Груз переехал позицией и остался на скакуне, а не у владельца (ROT-TRANSPORT-01).
        Assert.Equal(1, mount.CarriedLoad);
        Assert.Equal("Bedroll", Assert.Single(mount.Cargo).Name);
        Assert.DoesNotContain(importedSheet.Items!, i => i.Name == "Bedroll");
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

    // ── Раздел «Транспорт» (ROT-TRANSPORT-01) ──

    /// <summary>Повозка стала транспортом со статблоком, а не предметом с фиктивным Enc.</summary>
    [Fact]
    public async Task WagonIsSoldAsTransportRatherThanInventoryGear()
    {
        var (_, _, reference) = await CreateRiderAsync();

        var wagon = Vehicle(reference, "wagon");
        Assert.Equal(TransportKind.Vehicle, wagon.TransportKind);
        Assert.Equal(MovementMode.Wheeled, wagon.MovementMode);
        Assert.True(wagon.RequiresTraction);
        Assert.Equal(200, wagon.Price);
        Assert.DoesNotContain(reference.Items, i => i.Code == "rot.item.wagon");
    }

    [Fact]
    public async Task CargoOnTransportStaysOutOfTheOwnersEncumbrance()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        // Enc 5, поэтому вклад в вес виден и до, и после переноса.
        var item = await AddItemAsync(client, id, reference, "barding");
        var loadedOwner = (await SheetAsync(client, id)).Derived.EncumbranceLoad;

        Assert.Equal(HttpStatusCode.NoContent,
            (await MoveAsync(client, id, item, new MoveCargoRequest(mount.Id))).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(loadedOwner - 5, sheet.Derived.EncumbranceLoad);
        var loaded = Assert.Single(sheet.Mounts!);
        Assert.Equal(5, loaded.CarriedLoad);
        Assert.Equal(18, loaded.Capacity);
        Assert.False(loaded.IsOverloaded);
        // Позиция уехала из инвентаря владельца в карточку транспорта, а не задвоилась.
        Assert.DoesNotContain(sheet.Items!, i => i.Id == item);
        Assert.Equal(item, Assert.Single(loaded.Cargo).Id);
    }

    /// <summary>Попона защищает скакуна, а не всадника, и сумки поднимают вместимость.</summary>
    [Fact]
    public async Task InstalledGearChangesTheMountRatherThanTheRider()
    {
        var (client, id, reference) = await CreateRiderAsync();
        // Боевой скакун: попона положена ему по умолчанию, без решения ведущего.
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var before = await SheetAsync(client, id);

        var barding = await AddItemAsync(client, id, reference, "barding");
        var bags = await AddItemAsync(client, id, reference, "saddlebags");
        Assert.Equal(HttpStatusCode.NoContent,
            (await MoveAsync(client, id, barding, new MoveCargoRequest(mount.Id, Install: true))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await MoveAsync(client, id, bags, new MoveCargoRequest(mount.Id, Install: true))).StatusCode);

        var sheet = await SheetAsync(client, id);
        var loaded = Assert.Single(sheet.Mounts!);
        // Седельные сумки дают +4 к вместимости профиля 13; установленное груз не занимает.
        Assert.Equal(17, loaded.Capacity);
        Assert.Equal(0, loaded.CarriedLoad);
        Assert.Equal(2, loaded.Cargo.Count);
        Assert.All(loaded.Cargo, c => Assert.True(c.IsInstalledOnMount));
        // Попона достаётся скакуну: поглощение профиля 4 + 2, защита с нуля до провайдера 1.
        Assert.Equal(6, loaded.Soak);
        Assert.Equal(1, loaded.MeleeDefense);
        Assert.Equal(1, loaded.RangedDefense);
        // Всаднику установленное не достаётся ни защитой, ни весом, ни порогом нагрузки.
        Assert.Equal(before.Derived.Soak, sheet.Derived.Soak);
        Assert.Equal(before.Derived.MeleeDefense, sheet.Derived.MeleeDefense);
        Assert.Equal(before.Derived.EncumbranceLoad, sheet.Derived.EncumbranceLoad);
        Assert.Equal(before.Derived.EncumbranceThreshold, sheet.Derived.EncumbranceThreshold);
    }

    /// <summary>
    /// Попона на не-боевого скакуна — решение ведущего: без причины отказ, с причиной ставится и
    /// причина попадает в историю (ROT-MOUNT-NPC-01).
    /// </summary>
    [Fact]
    public async Task BardingOnANonWarMountNeedsAnExplicitGmReason()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "riding-beast").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var barding = await AddItemAsync(client, id, reference, "barding");

        var refused = await MoveAsync(client, id, barding, new MoveCargoRequest(mount.Id, Install: true));
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);
        Assert.Equal("cargo.barding_requires_override",
            (await refused.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        // Отказ ничего не менял: позиция осталась у владельца.
        Assert.Empty(Assert.Single((await SheetAsync(client, id)).Mounts!).Cargo);

        Assert.Equal(HttpStatusCode.NoContent, (await MoveAsync(client, id, barding,
            new MoveCargoRequest(mount.Id, Install: true, InstallOverrideReason: "подогнал кузнец")))
            .StatusCode);

        var loaded = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.True(Assert.Single(loaded.Cargo).IsInstalledOnMount);
        // Профиль верхового животного — soak 4, защита 0/0; попона поднимает до 6 и 1/1.
        Assert.Equal(6, loaded.Soak);
        Assert.Equal(1, loaded.MeleeDefense);

        var audit = await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options);
        Assert.Contains(audit!, a => a.Summary.Contains("подогнал кузнец", StringComparison.Ordinal));
    }

    /// <summary>
    /// Снятие снаряжения возвращает исходные числа профиля: установленное считается на чтение и
    /// статблок не переписывает (ROT-MOUNT-NPC-01).
    /// </summary>
    [Fact]
    public async Task RemovingBardingRestoresTheProfileNumbers()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "war-mount").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var barding = await AddItemAsync(client, id, reference, "barding");
        await MoveAsync(client, id, barding, new MoveCargoRequest(mount.Id, Install: true));
        Assert.Equal(6, Assert.Single((await SheetAsync(client, id)).Mounts!).Soak);

        await MoveAsync(client, id, barding, new MoveCargoRequest(null));

        var bare = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.Equal(4, bare.Soak);
        Assert.Equal(0, bare.MeleeDefense);
        Assert.Equal(4, bare.Definition.Soak);
    }

    /// <summary>
    /// Попона «задаёт Defense 1», а не прибавляет: летающему скакуну с напечатанной дальней
    /// защитой 2 она ничего не добавляет (ROT-CMB-03).
    /// </summary>
    [Fact]
    public async Task BardingDoesNotStackWithAProfilesOwnDefense()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "flying-mount").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var barding = await AddItemAsync(client, id, reference, "barding");

        await MoveAsync(client, id, barding,
            new MoveCargoRequest(mount.Id, Install: true, InstallOverrideReason: "сшита по крылу"));

        var loaded = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.Equal(5, loaded.Soak);
        Assert.Equal(1, loaded.MeleeDefense);
        Assert.Equal(2, loaded.RangedDefense);
    }

    [Fact]
    public async Task OrdinaryGearCannotBeInstalledOnATransport()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "riding-beast").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var bedroll = await AddItemAsync(client, id, reference, "bedroll");

        var response = await MoveAsync(client, id, bedroll, new MoveCargoRequest(mount.Id, Install: true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("cargo.not_mount_gear",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task CargoBeyondCapacityIsRefusedAndPartialMoveSplitsTheStack()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "riding-beast").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        // Вместимость профиля 12, попона весит 5: три штуки не помещаются, две помещаются.
        var stack = await AddItemAsync(client, id, reference, "barding", quantity: 3);

        var tooMuch = await MoveAsync(client, id, stack, new MoveCargoRequest(mount.Id));
        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);
        Assert.Equal("cargo.capacity_exceeded",
            (await tooMuch.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await MoveAsync(client, id, stack, new MoveCargoRequest(mount.Id, Quantity: 2))).StatusCode);

        var sheet = await SheetAsync(client, id);
        var loaded = Assert.Single(sheet.Mounts!);
        Assert.Equal(10, loaded.CarriedLoad);
        Assert.Equal(2, Assert.Single(loaded.Cargo).Quantity);
        // Остаток стопки остался у владельца, а не пропал.
        Assert.Equal(1, sheet.Items!.Single(i => i.Id == stack).Quantity);
    }

    [Fact]
    public async Task WagonKeepsItsCargoWhenTheDraftAnimalIsUnhitched()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Vehicle(reference, "wagon").Id, Free: true), Json.Options);
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var sheet = await SheetAsync(client, id);
        var wagon = sheet.Mounts!.Single(m => m.Definition.Code == "rot.vehicle.wagon");
        var beast = sheet.Mounts!.Single(m => m.Definition.Code == "rot.mount.beast-of-burden");
        var cargo = await AddItemAsync(client, id, reference, "bedroll");
        await MoveAsync(client, id, cargo, new MoveCargoRequest(wagon.Id));

        // Без тяги повозка стоит, но существует и держит груз.
        Assert.True(Assert.Single((await SheetAsync(client, id)).Mounts!
            .Where(m => m.Id == wagon.Id)).NeedsTraction);

        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{wagon.Id}",
            new UpdateMountRequest(DrawnByMountId: beast.Id), Json.Options);
        var hitched = (await SheetAsync(client, id)).Mounts!.Single(m => m.Id == wagon.Id);
        Assert.False(hitched.NeedsTraction);
        Assert.Equal(beast.Id, hitched.DrawnByMountId);

        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{wagon.Id}",
            new UpdateMountRequest(ClearDrawnBy: true), Json.Options);

        var after = await SheetAsync(client, id);
        var unhitched = after.Mounts!.Single(m => m.Id == wagon.Id);
        Assert.True(unhitched.NeedsTraction);
        Assert.Null(unhitched.DrawnByMountId);
        // Груз не переехал владельцу и не пропал.
        Assert.Equal(cargo, Assert.Single(unhitched.Cargo).Id);
        Assert.DoesNotContain(after.Items!, i => i.Id == cargo);
    }

    [Fact]
    public async Task AWagonCannotPullAnotherWagon()
    {
        var (client, id, reference) = await CreateRiderAsync();
        var wagonDef = Vehicle(reference, "wagon").Id;
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(wagonDef, Free: true), Json.Options);
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(wagonDef, Free: true), Json.Options);
        var wagons = (await SheetAsync(client, id)).Mounts!;

        var response = await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{wagons[0].Id}",
            new UpdateMountRequest(DrawnByMountId: wagons[1].Id), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("mount.traction_invalid",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Удаление транспорта возвращает груз владельцу, а не оставляет его без хозяина.</summary>
    [Fact]
    public async Task RemovingATransportReturnsItsCargoToTheOwner()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var cargo = await AddItemAsync(client, id, reference, "bedroll");
        var barding = await AddItemAsync(client, id, reference, "barding");
        await MoveAsync(client, id, cargo, new MoveCargoRequest(mount.Id));
        await MoveAsync(client, id, barding, new MoveCargoRequest(mount.Id, Install: true));

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/characters/{id}/mounts/{mount.Id}")).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Empty(sheet.Mounts!);
        Assert.Contains(sheet.Items!, i => i.Id == cargo);
        var returnedBarding = sheet.Items!.Single(i => i.Id == barding);
        Assert.False(returnedBarding.IsInstalledOnMount);
        Assert.Null(returnedBarding.CarriedByMountId);
    }

    [Fact]
    public async Task SellingTheDraftAnimalLeavesTheWagonWithoutABrokenLink()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Vehicle(reference, "wagon").Id, Free: true), Json.Options);
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var sheet = await SheetAsync(client, id);
        var wagon = sheet.Mounts!.Single(m => m.Definition.Code == "rot.vehicle.wagon");
        var beast = sheet.Mounts!.Single(m => m.Definition.Code == "rot.mount.beast-of-burden");
        await client.PatchAsJsonAsync($"/api/characters/{id}/mounts/{wagon.Id}",
            new UpdateMountRequest(DrawnByMountId: beast.Id), Json.Options);

        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/characters/{id}/mounts/{beast.Id}/sell", new SellMountRequest(), Json.Options))
            .StatusCode);

        var after = Assert.Single((await SheetAsync(client, id)).Mounts!);
        Assert.Equal(wagon.Id, after.Id);
        Assert.Null(after.DrawnByMountId);
        Assert.True(after.NeedsTraction);
    }

    [Fact]
    public async Task DuplicateKeepsCargoOnTheClonesOwnTransport()
    {
        var (client, id, reference) = await CreateRiderAsync();
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(Mount(reference, "beast-of-burden").Id, Free: true), Json.Options);
        var mount = Assert.Single((await SheetAsync(client, id)).Mounts!);
        var cargo = await AddItemAsync(client, id, reference, "bedroll");
        await MoveAsync(client, id, cargo, new MoveCargoRequest(mount.Id));

        var duplicate = await client.PostAsync($"/api/characters/{id}/duplicate", null);
        var cloneId = (await duplicate.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var cloneSheet = await SheetAsync(client, cloneId);
        var cloneMount = Assert.Single(cloneSheet.Mounts!);
        var cloneCargo = Assert.Single(cloneMount.Cargo);
        Assert.Equal("Bedroll", cloneCargo.Name);
        // Ссылка ведёт на транспорт клона, а не оригинала.
        Assert.Equal(cloneMount.Id, cloneCargo.CarriedByMountId);
        Assert.NotEqual(mount.Id, cloneMount.Id);
        Assert.NotEqual(cargo, cloneCargo.Id);
    }
}
