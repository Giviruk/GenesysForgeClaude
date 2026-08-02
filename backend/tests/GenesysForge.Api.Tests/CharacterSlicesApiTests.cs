using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Лист по частям (<c>GET /api/characters/{id}/slices?include=…</c>). Лист играющего персонажа
/// весит около 116 КБ, и две трети из них — инвентарь, который главной вкладке не нужен вовсе.
///
/// <para>
/// Главное, что проверяется здесь: базовый срез считает те же числа, что и полный лист, хотя
/// предметы и таланты в нём не приезжают. Поглощение, защита и порог веса берутся из инвентаря —
/// если срез перестанет его грузить, числа молча поедут, а не сломаются заметно.
/// </para>
/// </summary>
public class CharacterSlicesApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Носильщик", GameSystem.RealmsOfTerrinoth,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, TotalXp: 400, null, null, Money: 50000), Json.Options);
        return (client, id, reference);
    }

    /// <summary>Надевает броню: она меняет и поглощение, и переносимый вес.</summary>
    private static async Task EquipArmorAsync(HttpClient client, Guid id, ReferenceResponse reference)
    {
        var armor = reference.Items.First(i => i.Kind == ItemKind.Armor && i.Purchasable);
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(armor.Id, 1, ItemState.Equipped, Free: true), Json.Options);
    }

    private static Task<SheetSlicesDto?> SlicesAsync(HttpClient client, Guid id, string include) =>
        client.GetFromJsonAsync<SheetSlicesDto>(
            $"/api/characters/{id}/slices?include={include}", Json.Options);

    [Fact]
    public async Task TheBaseSliceCarriesNoHeavyCollections()
    {
        var (client, id, reference) = await CreateAsync();
        await EquipArmorAsync(client, id, reference);

        var slices = (await SlicesAsync(client, id, "base"))!;

        Assert.NotNull(slices.Base);
        Assert.Null(slices.Base.Items);
        Assert.Null(slices.Base.Talents);
        Assert.Null(slices.Base.Mounts);
        Assert.Null(slices.Base.Attachments);
        Assert.Null(slices.Items);
    }

    /// <summary>
    /// Числа базового среза совпадают с полным листом. Инвентарь для этого всё равно читается из
    /// базы — он просто не отдаётся наружу.
    /// </summary>
    [Fact]
    public async Task TheBaseSliceComputesTheSameNumbersAsTheFullSheet()
    {
        var (client, id, reference) = await CreateAsync();
        await EquipArmorAsync(client, id, reference);

        var full = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        var slices = (await SlicesAsync(client, id, "base"))!;

        Assert.Equal(full.Derived.Soak, slices.Base!.Derived.Soak);
        Assert.Equal(full.Derived.EncumbranceLoad, slices.Base.Derived.EncumbranceLoad);
        Assert.Equal(full.Derived.MeleeDefense, slices.Base.Derived.MeleeDefense);
        Assert.Equal(full.Derived.RangedDefense, slices.Base.Derived.RangedDefense);
        // Помехи снаряжения раскладываются по пулам навыков (ROT-ARM-01) — тоже из инвентаря.
        Assert.Equal(
            full.Skills.Sum(s => s.SetbackDice),
            slices.Base.Skills.Sum(s => s.SetbackDice));
    }

    /// <summary>Срез инвентаря совпадает с инвентарём полного листа — до карточек включительно.</summary>
    [Fact]
    public async Task TheItemsSliceMatchesTheFullSheet()
    {
        var (client, id, reference) = await CreateAsync();
        await EquipArmorAsync(client, id, reference);

        var full = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        var slices = (await SlicesAsync(client, id, "items"))!;

        Assert.Null(slices.Base);
        Assert.Equal(full.Items!.Count, slices.Items!.Count);
        Assert.Equal(
            full.Items.Select(i => (i.Id, i.SoakBonus, i.Load, i.Encumbrance)),
            slices.Items.Select(i => (i.Id, i.SoakBonus, i.Load, i.Encumbrance)));
    }

    /// <summary>
    /// Карточка инвентаря получает copyright-safe описание вместе с экземпляром. В PublicSafe
    /// полное описание пусто, поэтому без этого поля существующий текст каталога терялся.
    /// </summary>
    [Fact]
    public async Task TheInventoryItemCarriesSafeDescription()
    {
        var (client, id, reference) = await CreateAsync();
        var definition = reference.Items.First(i =>
            i.Purchasable && !string.IsNullOrWhiteSpace(i.SafeDescription));
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(definition.Id, 1, ItemState.Carried, Free: true), Json.Options);

        var slices = (await SlicesAsync(client, id, "items"))!;
        var item = Assert.Single(slices.Items!);

        Assert.Equal(definition.SafeDescription, item.SafeDescription);
    }

    /// <summary>
    /// Груз транспорта в своём срезе считается теми же поправками, что и позиция за спиной
    /// (ROT-TRANSPORT-01): для этого срезу транспорта и нужны предметы.
    /// </summary>
    [Fact]
    public async Task TheMountsSliceCarriesItsCargo()
    {
        var (client, id, reference) = await CreateAsync();
        var gear = reference.Items.First(i => i.Kind == ItemKind.Gear && i.Purchasable);
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(gear.Id, 1, ItemState.Backpack, Free: true), Json.Options);
        await client.PostAsJsonAsync($"/api/characters/{id}/mounts",
            new BuyMountRequest(reference.Mounts!.First(m => m.TransportKind == TransportKind.Mount).Id,
                Free: true), Json.Options);
        var itemId = (await SlicesAsync(client, id, "items"))!.Items!.Single().Id;
        var mountId = (await SlicesAsync(client, id, "mounts"))!.Mounts!.Single().Id;
        await client.PatchAsJsonAsync($"/api/characters/{id}/items/{itemId}/location",
            new MoveCargoRequest(mountId), Json.Options);

        var slices = (await SlicesAsync(client, id, "mounts"))!;

        var mount = Assert.Single(slices.Mounts!);
        var cargo = Assert.Single(mount.Cargo);
        Assert.Equal(itemId, cargo.Id);
        Assert.Null(slices.Base);
        Assert.Null(slices.Items);
    }

    /// <summary>Без параметра приезжает всё: так отвечали до разделения.</summary>
    [Fact]
    public async Task WithoutIncludeEverythingComesBack()
    {
        var (client, id, reference) = await CreateAsync();
        await EquipArmorAsync(client, id, reference);

        var slices = (await client.GetFromJsonAsync<SheetSlicesDto>(
            $"/api/characters/{id}/slices", Json.Options))!;

        Assert.NotNull(slices.Base);
        Assert.NotNull(slices.Items);
        Assert.NotNull(slices.Talents);
        Assert.NotNull(slices.Mounts);
        Assert.NotNull(slices.Attachments);
    }

    /// <summary>Непонятное имя среза не роняет запрос и не отдаёт лишнего.</summary>
    [Fact]
    public async Task AnUnknownSliceNameFallsBackToTheWholeSheet()
    {
        var (client, id, _) = await CreateAsync();

        var slices = (await SlicesAsync(client, id, "выдумка"))!;

        Assert.NotNull(slices.Base);
        Assert.NotNull(slices.Items);
    }

    /// <summary>Полный лист по-прежнему отдаёт всё: на нём держатся печать, экспорт и ссылка.</summary>
    [Fact]
    public async Task TheFullSheetRouteStillCarriesEverything()
    {
        var (client, id, reference) = await CreateAsync();
        await EquipArmorAsync(client, id, reference);

        var full = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;

        Assert.NotNull(full.Items);
        Assert.NotNull(full.Talents);
        Assert.NotNull(full.TalentTierCounts);
        Assert.NotNull(full.Mounts);
        Assert.NotNull(full.Attachments);
    }

    /// <summary>
    /// Незапрошенная часть уезжает на провод как <c>"items": null</c>, а не отсутствующим полем:
    /// сериализатор настроен писать <c>null</c>-ы.
    ///
    /// <para>
    /// Это записано тестом, потому что на клиенте от этого зависит логика: там «загружено ли»
    /// проверяется сравнением с <c>null</c>, а не с <c>undefined</c>, и пришедшие <c>null</c>-ы не
    /// накладываются поверх уже загруженного. Ровно на этом один раз уже сломались вкладки —
    /// незагруженное считалось загруженным, запросов не уходило, списки были пустыми.
    /// </para>
    /// </summary>
    [Fact]
    public async Task UnrequestedSlicesGoOverTheWireAsExplicitNulls()
    {
        var (client, id, _) = await CreateAsync();

        var raw = await client.GetStringAsync($"/api/characters/{id}/slices?include=base");

        Assert.Contains("\"items\":null", raw, StringComparison.Ordinal);
        Assert.Contains("\"talents\":null", raw, StringComparison.Ordinal);
        Assert.Contains("\"mounts\":null", raw, StringComparison.Ordinal);
        Assert.Contains("\"attachments\":null", raw, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AnotherOwnersSlicesAreNotServed()
    {
        var (_, id, _) = await CreateAsync();
        var stranger = await factory.CreateAuthorizedClientAsync();

        var response = await stranger.GetAsync($"/api/characters/{id}/slices?include=base");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
