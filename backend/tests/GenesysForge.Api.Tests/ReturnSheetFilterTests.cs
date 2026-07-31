using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Возврат листа вместе с ответом на правку (оптимизация круговых обращений). Интерфейс после
/// каждой правки всё равно перечитывает лист, и это стоило отдельного запроса. Главное, что
/// проверяется здесь: без заголовка контракт не изменился ни на одном маршруте.
/// </summary>
public class ReturnSheetFilterTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private async Task<(HttpClient Client, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Ускоренный", GameSystem.RealmsOfTerrinoth,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id);
    }

    private static HttpRequestMessage Patch(Guid id, object body, string? slices)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/characters/{id}")
        {
            Content = JsonContent.Create(body, options: Json.Options),
        };
        if (slices is not null) request.Headers.Add("X-Return-Slices", slices);
        return request;
    }

    /// <summary>Без заголовка всё как раньше — на это опираются и старые клиенты, и тесты статусов.</summary>
    [Fact]
    public async Task WithoutTheHeaderTheResponseIsStillNoContent()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), null));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
    }

    [Fact]
    public async Task WithTheHeaderTheUpdatedSliceComesBackWithTheEdit()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(
            Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), "base"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slices = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;
        Assert.Equal(id, slices.Base!.Id);
        // Именно обновлённый лист, а не тот, что был до правки.
        Assert.Equal(500, slices.Base.Money);
    }

    /// <summary>
    /// Просили один срез — приходит один. Не запрошенное приезжать не должно: ради этого разделение
    /// и делалось, а инвентарь — две трети веса листа.
    /// </summary>
    [Fact]
    public async Task OnlyTheRequestedSlicesComeBack()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(
            Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), "base"));
        var slices = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;

        Assert.NotNull(slices.Base);
        Assert.Null(slices.Items);
        Assert.Null(slices.Talents);
        Assert.Null(slices.Mounts);
        Assert.Null(slices.Attachments);
    }

    /// <summary>Несколько срезов разом: вкладка инвентаря показывает и деньги, и предметы.</summary>
    [Fact]
    public async Task SeveralSlicesComeBackTogether()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(
            Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), "base,items"));
        var slices = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;

        Assert.NotNull(slices.Base);
        Assert.NotNull(slices.Items);
        Assert.Null(slices.Talents);
    }

    /// <summary>Срез после правки обязан совпадать с тем, что отдаёт отдельное чтение.</summary>
    [Fact]
    public async Task TheReturnedSlicesMatchAPlainRead()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(
            Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 777), "base,items"));
        var fromPatch = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;
        var fromGet = (await client.GetFromJsonAsync<SheetSlicesDto>(
            $"/api/characters/{id}/slices?include=base,items", Json.Options))!;

        Assert.Equal(fromGet.Base!.Money, fromPatch.Base!.Money);
        Assert.Equal(fromGet.Base.Derived.EncumbranceLoad, fromPatch.Base.Derived.EncumbranceLoad);
        Assert.Equal(fromGet.Items!.Count, fromPatch.Items!.Count);
        Assert.Equal(fromGet.Base.Skills.Count, fromPatch.Base.Skills.Count);
    }

    /// <summary>
    /// `duplicate` создаёт <b>другого</b> персонажа — его заголовок не трогает. Иначе клиент
    /// получил бы части исходного листа под идентификатором копии.
    /// </summary>
    [Fact]
    public async Task DuplicateIsLeftAloneBecauseItCreatesAnotherCharacter()
    {
        var (client, id) = await CreateCharacterAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/characters/{id}/duplicate");
        request.Headers.Add("X-Return-Slices", "base");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        Assert.True(body!.ContainsKey("id"));
        Assert.NotEqual(id, body["id"]);
    }

    /// <summary>
    /// Покупка предмета создаёт запись внутри персонажа, поэтому части листа приезжают и с ней —
    /// иначе за ними шёл бы второй запрос. Идентификатор созданного при этом не теряется.
    /// </summary>
    [Fact]
    public async Task CreatingSomethingInsideTheCharacterAlsoReturnsSlices()
    {
        var (client, id) = await CreateCharacterAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var item = reference.Items.First(i => i.Purchasable);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/characters/{id}/items")
        {
            Content = JsonContent.Create(
                new AddItemRequest(item.Id, 1, ItemState.Carried, Free: true), options: Json.Options),
        };
        request.Headers.Add("X-Return-Slices", "base,items");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var slices = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;
        var added = Assert.Single(slices.Items!);
        Assert.Equal(item.Id, added.ItemDefId);
        // Идентификатор созданной позиции — из него собирают ссылку и следующие правки.
        Assert.Equal(added.Id, slices.CreatedId);
        Assert.NotNull(slices.Base);
    }

    /// <summary>Без заголовка покупка отвечает ровно как раньше: 201 Created с идентификатором.</summary>
    [Fact]
    public async Task WithoutTheHeaderCreatingStillAnswers201()
    {
        var (client, id) = await CreateCharacterAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;

        var response = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(reference.Items.First(i => i.Purchasable).Id, 1, ItemState.Carried, Free: true),
            Json.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        Assert.True(body!.ContainsKey("id"));
    }

    /// <summary>Обычная правка ничего не создаёт — идентификатору созданного взяться неоткуда.</summary>
    [Fact]
    public async Task APlainEditReportsNothingCreated()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(
            Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), "base"));
        var slices = (await response.Content.ReadFromJsonAsync<SheetSlicesDto>(Json.Options))!;

        Assert.Null(slices.CreatedId);
    }

    /// <summary>Провалившаяся правка остаётся ошибкой, а не превращается в лист.</summary>
    [Fact]
    public async Task AFailedEditStaysAnError()
    {
        var (client, id) = await CreateCharacterAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Delete, $"/api/characters/{id}/mounts/{Guid.NewGuid()}");
        request.Headers.Add("X-Return-Slices", "1");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("mount.not_found",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>
    /// Удаление персонажа — единственный случай, когда листа после правки уже не существует.
    /// Успешная запись не должна из-за этого превратиться в ошибку.
    /// </summary>
    [Fact]
    public async Task DeletingTheCharacterStillSucceedsEvenThoughThereIsNoSheetLeft()
    {
        var (client, id) = await CreateCharacterAsync();

        var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/characters/{id}");
        request.Headers.Add("X-Return-Slices", "1");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await client.GetAsync($"/api/characters/{id}")).StatusCode);
    }

    /// <summary>Чужой персонаж не отдаётся и через этот заголовок.</summary>
    [Fact]
    public async Task TheHeaderDoesNotLeakAnotherOwnersSheet()
    {
        var (_, id) = await CreateCharacterAsync();
        var stranger = await factory.CreateAuthorizedClientAsync();

        var response = await stranger.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 1), "base"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
