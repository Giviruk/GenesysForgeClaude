using System.Net;
using System.Net.Http.Json;
using GenesysForge.Api;
using GenesysForge.Application.Dtos;
using GenesysForge.Api.Endpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace GenesysForge.Api.Tests;

public class ProductionConfigurationTests
{
    [Fact]
    public void Production_rejects_default_jwt_key()
    {
        var config = Config(
            ("Jwt:Key", GenesysForge.Infrastructure.Auth.TokenService.DevFallbackKey),
            ("Cors:Origins", "https://example.test"));

        var ex = Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(config, Environment("Production")));

        Assert.Contains("Jwt:Key", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("http://example.test")]
    [InlineData("https://example.test/path")]
    [InlineData("https://example.test#fragment")]
    [InlineData("https://user@example.test")]
    public void Production_rejects_missing_or_unsafe_cors(string origins)
    {
        var config = Config(
            ("Jwt:Key", "a-production-signing-key-that-is-long-enough-123"),
            ("Cors:Origins", origins));

        Assert.Throws<InvalidOperationException>(
            () => ProductionConfiguration.Validate(config, Environment("Production")));
    }

    [Fact]
    public void Development_allows_local_defaults()
    {
        var config = Config();
        ProductionConfiguration.Validate(config, Environment("Development"));
    }

    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v =>
                new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static IWebHostEnvironment Environment(string name) => new TestWebHostEnvironment
    {
        EnvironmentName = name,
    };
}

public class RateLimitingTests : IClassFixture<RateLimitedApiFactory>
{
    private readonly RateLimitedApiFactory _factory;
    public RateLimitingTests(RateLimitedApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Sensitive_auth_endpoints_return_429_after_limit()
    {
        var client = _factory.CreateClient();

        for (var i = 0; i < 2; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/register",
                new RegisterRequest($"limited{i}@test.local", "password123", $"Limited {i}"));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var rejected = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("limited2@test.local", "password123", "Limited 2"));

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        var body = await rejected.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Contains("Слишком много запросов", body!.Message);
    }
}

public class SecureCookieTests
{
    [Theory]
    [InlineData("Production", false, true)]
    [InlineData("Development", true, true)]
    [InlineData("Development", false, false)]
    public void Refresh_cookie_is_secure_in_production_or_https(
        string environmentName, bool isHttps, bool expectedSecure)
    {
        var services = new ServiceCollection()
            .AddSingleton<IWebHostEnvironment>(new TestWebHostEnvironment
            {
                EnvironmentName = environmentName,
            })
            .AddSingleton<IHostEnvironment>(sp => sp.GetRequiredService<IWebHostEnvironment>())
            .BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = isHttps ? "https" : "http";

        RefreshTokenCookie.Set(context, "raw-token", DateTime.UtcNow.AddDays(1));

        var cookie = context.Response.Headers.SetCookie.Single()!;
        Assert.Equal(expectedSecure, cookie.Contains("; secure", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", cookie, StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class TestWebHostEnvironment : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "GenesysForge.Api.Tests";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string WebRootPath { get; set; } = "";
    public string EnvironmentName { get; set; } = "";
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
