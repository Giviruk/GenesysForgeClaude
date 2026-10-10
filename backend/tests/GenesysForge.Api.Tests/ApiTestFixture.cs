using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GenesysForge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GenesysForge.Api.Tests;

public class ApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                // Уникальное имя БД на фабрику — изоляция параллельных тест-классов
                ["InMemoryDatabaseName"] = $"genesysforge-tests-{Guid.NewGuid():N}",
                // Большинство integration-тестов проверяют use cases, а не throttling.
                ["RateLimiting:Enabled"] = "false",
            }));
        return base.CreateHost(builder);
    }
}

public class RateLimitedApiFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(config =>
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["InMemoryDatabaseName"] = $"genesysforge-rate-limit-{Guid.NewGuid():N}",
                ["RateLimiting:Enabled"] = "true",
                ["RateLimiting:AuthSensitive:PermitLimit"] = "2",
                ["RateLimiting:AuthSensitive:WindowSeconds"] = "60",
            }));
        return base.CreateHost(builder);
    }
}

public static class Json
{
    /// <summary>Зеркало серверных настроек сериализации (enum'ы — camelCase-строками).</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
}

public static class TestClientExtensions
{
    private static int _userCounter;

    /// <summary>Регистрирует нового пользователя и возвращает авторизованный клиент.</summary>
    public static async Task<HttpClient> CreateAuthorizedClientAsync(this ApiFactory factory)
    {
        var client = factory.CreateClient();
        var n = Interlocked.Increment(ref _userCounter);
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"user{n}@test.local", "password123", $"User {n}"));
        response.EnsureSuccessStatusCode();
        var auth = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }
}

/// <summary>Explicit pack fixture: v2 no longer creates technical personal/campaign packs.</summary>
public static class LibraryTestFixtures
{
    public static async Task<HomebrewPackListItemDto> CreateLibraryPackAsync(this HttpClient client,
        GenesysForge.Domain.GameSystem system = GenesysForge.Domain.GameSystem.GenesysCore, Guid? campaignId = null)
    {
        var response = await client.PostAsJsonAsync("/api/homebrew-packs", new PackDetailsRequest("Test library pack", "Own fixture", system), Json.Options);
        response.EnsureSuccessStatusCode();
        var pack = (await response.Content.ReadFromJsonAsync<HomebrewPackListItemDto>(Json.Options))!;
        var entries = (await client.GetFromJsonAsync<List<LibraryEntryDto>>($"/api/library?system={system}", Json.Options))!;
        (await client.PostAsJsonAsync($"/api/homebrew-packs/{pack.Id}/entries", new ContentEntriesRequest(entries.Select(x => new ContentEntryRef(x.EntryType, x.Id)).ToList()), Json.Options)).EnsureSuccessStatusCode();
        if (campaignId is not null)
        {
            // These fixtures exercise pack gating, rather than the separately tested legacy direct-item path.
            var direct = (await client.GetFromJsonAsync<List<CampaignContentItemDto>>($"/api/campaigns/{campaignId}/content/items", Json.Options))!;
            foreach (var item in direct.Where(x => entries.Any(e => e.Id == x.EntryId)))
                (await client.DeleteAsync($"/api/campaigns/{campaignId}/content/items/{item.Id}")).EnsureSuccessStatusCode();
            (await client.PutAsJsonAsync($"/api/campaigns/{campaignId}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(true), Json.Options)).EnsureSuccessStatusCode();
        }
        return (await client.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs", Json.Options))!.Single(x => x.Id == pack.Id);
    }
}
