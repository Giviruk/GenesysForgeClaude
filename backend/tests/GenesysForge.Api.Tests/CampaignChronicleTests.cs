using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class CampaignChronicleTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CampaignChronicleTests(ApiFactory factory) => _factory = factory;

    private static async Task<Guid> CreateCharacterAsync(HttpClient client)
    {
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var response = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Летописец", GameSystem.GenesysCore,
                reference.Archetypes[0].Id, reference.Careers[0].Id, null), Json.Options);
        return (await response.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private async Task<(HttpClient Gm, HttpClient Player, HttpClient Stranger, CampaignDetailDto Campaign)> SetupAsync()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaignResponse = await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Хроники Севера", ""), Json.Options);
        var campaign = (await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var player = await _factory.CreateAuthorizedClientAsync();
        var characterId = await CreateCharacterAsync(player);
        await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options);
        return (gm, player, await _factory.CreateAuthorizedClientAsync(), campaign);
    }

    [Fact]
    public async Task Player_CanCreateEditAndRestore_Chapter_WithImmutableHistory()
    {
        var (_, player, _, campaign) = await SetupAsync();
        var create = await player.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/chronicle/chapters",
            new SaveCampaignChronicleChapterRequest("Глава I", "# Прибытие\n\nГерои вошли в город."), Json.Options);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var chapter = (await create.Content.ReadFromJsonAsync<CampaignChronicleChapterDto>(Json.Options))!;
        Assert.Equal(1, chapter.CurrentVersion);

        var update = await player.PutAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{chapter.Id}",
            new SaveCampaignChronicleChapterRequest("Глава I", "# Прибытие\n\nНачалась буря.", 1), Json.Options);
        var updated = (await update.Content.ReadFromJsonAsync<CampaignChronicleChapterDto>(Json.Options))!;
        Assert.Equal(2, updated.CurrentVersion);

        var staleUpdate = await player.PutAsJsonAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{chapter.Id}",
            new SaveCampaignChronicleChapterRequest("Устаревшая правка", "Не должна сохраниться", 1), Json.Options);
        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);

        var history = (await player.GetFromJsonAsync<List<CampaignChronicleRevisionDto>>(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{chapter.Id}/history", Json.Options))!;
        Assert.Equal([2, 1], history.Select(x => x.Version));
        var first = history.Single(x => x.Version == 1);

        var restore = await player.PostAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{chapter.Id}/restore/{first.Id}", null);
        var restored = (await restore.Content.ReadFromJsonAsync<CampaignChronicleChapterDto>(Json.Options))!;
        Assert.Equal(3, restored.CurrentVersion);
        Assert.Contains("Герои вошли", restored.Content);

        var after = (await player.GetFromJsonAsync<List<CampaignChronicleRevisionDto>>(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{chapter.Id}/history", Json.Options))!;
        Assert.Equal([3, 2, 1], after.Select(x => x.Version));
    }

    [Fact]
    public async Task GmAndMemberCanRead_ButStrangerCannot()
    {
        var (gm, player, stranger, campaign) = await SetupAsync();
        await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/chronicle/chapters",
            new SaveCampaignChronicleChapterRequest("Пролог", "Текст"), Json.Options);

        Assert.Single((await gm.GetFromJsonAsync<List<CampaignChronicleChapterDto>>(
            $"/api/campaigns/{campaign.Id}/chronicle", Json.Options))!);
        Assert.Single((await player.GetFromJsonAsync<List<CampaignChronicleChapterDto>>(
            $"/api/campaigns/{campaign.Id}/chronicle", Json.Options))!);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await stranger.GetAsync($"/api/campaigns/{campaign.Id}/chronicle")).StatusCode);
    }

    [Fact]
    public async Task GmAndMemberCanDeleteAnyChapter_ButStrangerCannot()
    {
        var (gm, player, stranger, campaign) = await SetupAsync();
        var gmCreatedResponse = await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/chronicle/chapters",
            new SaveCampaignChronicleChapterRequest("Глава мастера", "Текст"), Json.Options);
        var gmCreated = (await gmCreatedResponse.Content.ReadFromJsonAsync<CampaignChronicleChapterDto>(Json.Options))!;
        var playerCreatedResponse = await player.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/chronicle/chapters",
            new SaveCampaignChronicleChapterRequest("Глава игрока", "Текст"), Json.Options);
        var playerCreated = (await playerCreatedResponse.Content.ReadFromJsonAsync<CampaignChronicleChapterDto>(Json.Options))!;

        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.DeleteAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{gmCreated.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await player.DeleteAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{gmCreated.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await gm.DeleteAsync(
            $"/api/campaigns/{campaign.Id}/chronicle/chapters/{playerCreated.Id}")).StatusCode);

        Assert.Empty((await gm.GetFromJsonAsync<List<CampaignChronicleChapterDto>>(
            $"/api/campaigns/{campaign.Id}/chronicle", Json.Options))!);
    }
}
