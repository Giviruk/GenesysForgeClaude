using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Фейковый валидатор: токен кодируется как "sub;email;verified;name", чтобы тест задавал
/// внешнюю личность без реального обращения к Google.
/// </summary>
public sealed class FakeExternalIdentityValidator : IExternalIdentityValidator
{
    public bool GoogleConfigured => true;

    public Task<ExternalIdentityInfo> ValidateGoogleAsync(string idToken, CancellationToken ct = default)
    {
        var p = idToken.Split(';');
        return Task.FromResult(new ExternalIdentityInfo(p[0], p[1], bool.Parse(p[2]), p.Length > 3 ? p[3] : null));
    }
}

public class GoogleSignInTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _baseFactory;
    public GoogleSignInTests(ApiFactory factory) => _baseFactory = factory;

    private WebApplicationFactory<Program> WithFakeValidator() =>
        _baseFactory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IExternalIdentityValidator>();
            s.AddSingleton<IExternalIdentityValidator, FakeExternalIdentityValidator>();
        }));

    private static async Task<AuthResponse> GoogleAsync(HttpClient client, string idToken)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/google", new GoogleSignInRequest(idToken));
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    [Fact]
    public async Task New_google_user_gets_an_account()
    {
        var client = WithFakeValidator().CreateClient();

        var auth = await GoogleAsync(client, "sub-new;newg@test.local;true;New G");

        Assert.Equal("newg@test.local", auth.Email);
        Assert.Equal("New G", auth.DisplayName);
        Assert.False(string.IsNullOrEmpty(auth.Token));
    }

    [Fact]
    public async Task Same_google_subject_returns_the_same_user()
    {
        var client = WithFakeValidator().CreateClient();

        var first = await GoogleAsync(client, "sub-stable;person@test.local;true;Person");
        var second = await GoogleAsync(client, "sub-stable;person@test.local;true;Person");

        Assert.Equal(first.UserId, second.UserId);
    }

    [Fact]
    public async Task Verified_email_links_to_an_existing_password_account()
    {
        var factory = WithFakeValidator();
        var client = factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("linkme@test.local", "password123", "Link Me"));
        var registered = (await reg.Content.ReadFromJsonAsync<AuthResponse>())!;

        var google = await GoogleAsync(client, "sub-link;linkme@test.local;true;Link Me");

        Assert.Equal(registered.UserId, google.UserId); // та же учётка, не дубликат
    }

    [Fact]
    public async Task Unverified_email_is_rejected()
    {
        var client = WithFakeValidator().CreateClient();

        var resp = await client.PostAsJsonAsync("/api/auth/google",
            new GoogleSignInRequest("sub-unv;unverified@test.local;false;NoVerify"));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Providers_endpoint_reports_google_disabled_by_default()
    {
        var client = _baseFactory.CreateClient(); // без настроенного Auth:Google:ClientId

        var providers = await client.GetFromJsonAsync<AuthProvidersResponse>("/api/auth/providers", Json.Options);

        Assert.NotNull(providers);
        Assert.Null(providers!.GoogleClientId);
    }
}
