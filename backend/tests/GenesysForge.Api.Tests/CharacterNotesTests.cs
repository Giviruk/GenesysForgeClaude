using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class CharacterNotesTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterNotesTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Noted", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id);
    }

    [Fact]
    public async Task Create_List_Update_Delete_Flow()
    {
        var (client, id) = await CreateCharacterAsync();

        // create
        var created = await client.PostAsJsonAsync($"/api/characters/{id}/notes/",
            new SaveCharacterNoteRequest("Сюжет", "Встретили барда в таверне."), Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var note = (await created.Content.ReadFromJsonAsync<CharacterNoteDto>(Json.Options))!;
        Assert.Equal("Сюжет", note.Title);

        // list
        var list = (await client.GetFromJsonAsync<List<CharacterNoteDto>>($"/api/characters/{id}/notes/", Json.Options))!;
        Assert.Single(list);

        // update
        var updated = await client.PutAsJsonAsync($"/api/characters/{id}/notes/{note.Id}",
            new SaveCharacterNoteRequest("Сюжет (обновл.)", "Бард оказался шпионом."), Json.Options);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var updatedNote = (await updated.Content.ReadFromJsonAsync<CharacterNoteDto>(Json.Options))!;
        Assert.Equal("Сюжет (обновл.)", updatedNote.Title);
        Assert.True(updatedNote.UpdatedAt >= updatedNote.CreatedAt);

        // delete
        var del = await client.DeleteAsync($"/api/characters/{id}/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        var after = (await client.GetFromJsonAsync<List<CharacterNoteDto>>($"/api/characters/{id}/notes/", Json.Options))!;
        Assert.Empty(after);
    }

    [Fact]
    public async Task EmptyTitle_Rejected()
    {
        var (client, id) = await CreateCharacterAsync();
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/notes/",
            new SaveCharacterNoteRequest("  ", "body"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Notes_AreIsolatedBetweenUsers()
    {
        var (owner, id) = await CreateCharacterAsync();
        await owner.PostAsJsonAsync($"/api/characters/{id}/notes/",
            new SaveCharacterNoteRequest("Тайна", "секрет"), Json.Options);

        // чужой пользователь не видит персонажа и его заметки
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var list = await stranger.GetAsync($"/api/characters/{id}/notes/");
        Assert.Equal(HttpStatusCode.BadRequest, list.StatusCode); // «персонаж не найден»
    }

    [Fact]
    public async Task ForeignNote_CannotBeEditedOrDeleted()
    {
        var (owner, id) = await CreateCharacterAsync();
        var created = await owner.PostAsJsonAsync($"/api/characters/{id}/notes/",
            new SaveCharacterNoteRequest("Моя", "текст"), Json.Options);
        var note = (await created.Content.ReadFromJsonAsync<CharacterNoteDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var upd = await stranger.PutAsJsonAsync($"/api/characters/{id}/notes/{note.Id}",
            new SaveCharacterNoteRequest("Взлом", "x"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, upd.StatusCode);
        var del = await stranger.DeleteAsync($"/api/characters/{id}/notes/{note.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, del.StatusCode);
    }
}
