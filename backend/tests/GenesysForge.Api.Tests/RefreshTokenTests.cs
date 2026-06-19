using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using GenesysForge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GenesysForge.Api.Tests;

public class RefreshTokenTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RefreshTokenTests(ApiFactory factory) => _factory = factory;

    // Управляем cookie вручную, чтобы повторно предъявить «старый» refresh-токен.
    private HttpClient Client() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static int _n;

    private static string? RefreshCookie(HttpResponseMessage resp)
    {
        if (!resp.Headers.TryGetValues("Set-Cookie", out var cookies)) return null;
        foreach (var c in cookies)
        {
            var m = Regex.Match(c, "gf_refresh=([^;]*)");
            if (m.Success && m.Groups[1].Value.Length > 0) return m.Groups[1].Value;
        }
        return null;
    }

    private async Task<(HttpClient Client, string Cookie)> RegisterAsync()
    {
        var client = Client();
        var n = Interlocked.Increment(ref _n);
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"refresh{n}@test.local", "password123", $"Refresh {n}"));
        resp.EnsureSuccessStatusCode();
        var cookie = RefreshCookie(resp);
        Assert.False(string.IsNullOrEmpty(cookie));
        return (client, cookie!);
    }

    private static HttpRequestMessage RefreshWith(string cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        req.Headers.Add("Cookie", $"gf_refresh={cookie}");
        return req;
    }

    private static HttpRequestMessage LogoutWith(string cookie)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        req.Headers.Add("Cookie", $"gf_refresh={cookie}");
        return req;
    }

    [Fact]
    public async Task Refresh_rotates_token_and_returns_new_access()
    {
        var (client, c1) = await RegisterAsync();

        var resp = await client.SendAsync(RefreshWith(c1));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrEmpty(auth.Token));

        var c2 = RefreshCookie(resp);
        Assert.False(string.IsNullOrEmpty(c2));
        Assert.NotEqual(c1, c2); // токен ротировался
    }

    [Fact]
    public async Task Reused_refresh_token_revokes_the_family()
    {
        var (client, c1) = await RegisterAsync();

        var first = await client.SendAsync(RefreshWith(c1));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var c2 = RefreshCookie(first)!;

        // Повторное использование уже ротированного c1 → 401 и отзыв всего семейства.
        var reuse = await client.SendAsync(RefreshWith(c1));
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Свежий c2 теперь тоже недействителен (семейство погашено) → принудительный перелогин.
        var afterRevoke = await client.SendAsync(RefreshWith(c2));
        Assert.Equal(HttpStatusCode.Unauthorized, afterRevoke.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var (client, c1) = await RegisterAsync();

        var logout = await client.SendAsync(LogoutWith(c1));
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await client.SendAsync(RefreshWith(c1));
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Invalid_or_missing_refresh_token_is_rejected()
    {
        var client = Client();

        var bogus = await client.SendAsync(RefreshWith("not-a-real-token"));
        Assert.Equal(HttpStatusCode.Unauthorized, bogus.StatusCode);

        var none = await client.PostAsync("/api/auth/refresh", null);
        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);
    }

    [Fact]
    public async Task Access_token_is_short_lived()
    {
        var (client, c1) = await RegisterAsync();
        var resp = await client.SendAsync(RefreshWith(c1));
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;

        var jwt = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().ReadJwtToken(auth.Token);
        var minutes = (jwt.ValidTo - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(minutes, 25, 31); // ~30 минут, не 7 дней
    }
}
