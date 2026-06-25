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
