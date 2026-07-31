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

    private static HttpRequestMessage Patch(Guid id, object body, bool askForSheet)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, $"/api/characters/{id}")
        {
            Content = JsonContent.Create(body, options: Json.Options),
        };
        if (askForSheet) request.Headers.Add("X-Return-Sheet", "1");
        return request;
    }

    /// <summary>Без заголовка всё как раньше — на это опираются и старые клиенты, и тесты статусов.</summary>
    [Fact]
    public async Task WithoutTheHeaderTheResponseIsStillNoContent()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), false));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(0, response.Content.Headers.ContentLength ?? 0);
    }

    [Fact]
    public async Task WithTheHeaderTheUpdatedSheetComesBackWithTheEdit()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 500), true));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sheet = (await response.Content.ReadFromJsonAsync<CharacterSheetDto>(Json.Options))!;
        Assert.Equal(id, sheet.Id);
        // Именно обновлённый лист, а не тот, что был до правки.
        Assert.Equal(500, sheet.Money);
    }

    /// <summary>Возвращённый лист обязан совпадать с тем, что отдаёт обычный GET.</summary>
    [Fact]
    public async Task TheReturnedSheetMatchesAPlainGet()
    {
        var (client, id) = await CreateCharacterAsync();

        var response = await client.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 777), true));
        var fromPatch = (await response.Content.ReadFromJsonAsync<CharacterSheetDto>(Json.Options))!;
        var fromGet = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;

        Assert.Equal(fromGet.Money, fromPatch.Money);
        Assert.Equal(fromGet.Derived.EncumbranceLoad, fromPatch.Derived.EncumbranceLoad);
        Assert.Equal(fromGet.Items.Count, fromPatch.Items.Count);
        Assert.Equal(fromGet.Skills.Count, fromPatch.Skills.Count);
    }

    /// <summary>
    /// Маршруты со своим телом ответа заголовок не трогает: подменять `201 Created` листом нельзя.
    /// </summary>
    [Fact]
    public async Task ResponsesThatAlreadyHaveABodyAreLeftAlone()
    {
        var (client, id) = await CreateCharacterAsync();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/characters/{id}/duplicate");
        request.Headers.Add("X-Return-Sheet", "1");
        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        Assert.True(body!.ContainsKey("id"));
    }

    /// <summary>Провалившаяся правка остаётся ошибкой, а не превращается в лист.</summary>
    [Fact]
    public async Task AFailedEditStaysAnError()
    {
        var (client, id) = await CreateCharacterAsync();

        var request = new HttpRequestMessage(
            HttpMethod.Delete, $"/api/characters/{id}/mounts/{Guid.NewGuid()}");
        request.Headers.Add("X-Return-Sheet", "1");
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
        request.Headers.Add("X-Return-Sheet", "1");
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

        var response = await stranger.SendAsync(Patch(id, new UpdateCharacterRequest(null, null, null, null, Money: 1), true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
