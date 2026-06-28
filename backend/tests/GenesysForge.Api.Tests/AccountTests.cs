using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Api.Tests;

public class AccountTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AccountTests(ApiFactory factory) => _factory = factory;

    private static int _seq;

    /// <summary>Регистрирует пользователя с известными кредами и возвращает (клиент, email, пароль).</summary>
    private async Task<(HttpClient Client, string Email, string Password)> RegisterAsync()
    {
        var client = _factory.CreateClient();
        var n = Interlocked.Increment(ref _seq);
        var email = $"acct{n}@test.local";
        const string password = "password123";
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, $"Acct {n}"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return (client, email, password);
    }

    [Fact]
    public async Task Get_ReturnsCurrentUserProfile()
    {
        var (client, email, _) = await RegisterAsync();
        var account = (await client.GetFromJsonAsync<AccountDto>("/api/account/", Json.Options))!;
        Assert.Equal(email, account.Email);
        Assert.False(string.IsNullOrWhiteSpace(account.DisplayName));
        Assert.Null(account.AvatarUrl);
    }

    [Fact]
    public async Task Unauthenticated_CannotAccessAccount()
    {
        var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/api/account/");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Patch_UpdatesDisplayNameAndAvatar()
    {
        var (client, _, _) = await RegisterAsync();

        var updated = (await (await client.PatchAsJsonAsync("/api/account/",
            new UpdateAccountRequest("Новое имя", "https://cdn.test/a.png"), Json.Options))
            .Content.ReadFromJsonAsync<AccountDto>(Json.Options))!;
        Assert.Equal("Новое имя", updated.DisplayName);
        Assert.Equal("https://cdn.test/a.png", updated.AvatarUrl);

        // Пустой avatarUrl очищает аватар; null displayName не трогает имя.
        var cleared = (await (await client.PatchAsJsonAsync("/api/account/",
            new UpdateAccountRequest(null, ""), Json.Options))
            .Content.ReadFromJsonAsync<AccountDto>(Json.Options))!;
        Assert.Equal("Новое имя", cleared.DisplayName);
        Assert.Null(cleared.AvatarUrl);
    }

    [Fact]
    public async Task Patch_EmptyDisplayName_Rejected()
    {
        var (client, _, _) = await RegisterAsync();
        var resp = await client.PatchAsJsonAsync("/api/account/",
            new UpdateAccountRequest("   ", null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_WrongCurrent_Rejected()
    {
        var (client, _, _) = await RegisterAsync();
        var resp = await client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest("wrong-password", "newpassword1"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_Success_OldFails_NewWorks()
    {
        var (client, email, password) = await RegisterAsync();

        var change = await client.PostAsJsonAsync("/api/account/change-password",
            new ChangePasswordRequest(password, "brandNew123"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, change.StatusCode);

        var anon = _factory.CreateClient();
        var oldLogin = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
        Assert.Equal(HttpStatusCode.Unauthorized, oldLogin.StatusCode); // старый пароль больше не подходит

        var newLogin = await anon.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "brandNew123"));
        Assert.Equal(HttpStatusCode.OK, newLogin.StatusCode);
    }
}
