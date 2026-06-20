using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenesysForge.Api.Tests;

/// <summary>Перехватывает «письмо» со ссылкой сброса, чтобы тест узнал одноразовый токен.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }
    public string? LastToken { get; private set; }
    public int Sent { get; private set; }

    public Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default)
    {
        LastEmail = email;
        LastToken = rawToken;
        Sent++;
        return Task.CompletedTask;
    }

    public Task SendEmailConfirmationAsync(string email, string rawToken, CancellationToken ct = default)
        => Task.CompletedTask; // не интересует в тестах сброса пароля
}

public class PasswordResetTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _baseFactory;
    public PasswordResetTests(ApiFactory factory) => _baseFactory = factory;

    private WebApplicationFactory<Program> WithSender(CapturingEmailSender sender) =>
        _baseFactory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(sender);
        }));

    private static async Task RegisterAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, "Reset User"));
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task FullFlow_resets_password_and_old_password_stops_working()
    {
        var sender = new CapturingEmailSender();
        var client = WithSender(sender).CreateClient();
        const string email = "reset-flow@test.local";
        await RegisterAsync(client, email, "oldpassword");

        var req = await client.PostAsJsonAsync("/api/auth/password-reset/request",
            new PasswordResetRequestRequest(email));
        Assert.Equal(HttpStatusCode.NoContent, req.StatusCode);
        Assert.Equal(email, sender.LastEmail);
        Assert.False(string.IsNullOrEmpty(sender.LastToken));

        var confirm = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(sender.LastToken!, "newpassword"));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        var withNew = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "newpassword"));
        Assert.Equal(HttpStatusCode.OK, withNew.StatusCode);

        var withOld = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "oldpassword"));
        Assert.Equal(HttpStatusCode.Unauthorized, withOld.StatusCode);
    }

    [Fact]
    public async Task Request_for_unknown_email_returns_204_and_sends_nothing()
    {
        var sender = new CapturingEmailSender();
        var client = WithSender(sender).CreateClient();

        var req = await client.PostAsJsonAsync("/api/auth/password-reset/request",
            new PasswordResetRequestRequest("nobody@test.local"));

        Assert.Equal(HttpStatusCode.NoContent, req.StatusCode);
        Assert.Equal(0, sender.Sent); // не раскрываем отсутствие аккаунта и не шлём письмо
    }

    [Fact]
    public async Task Confirm_with_invalid_token_is_rejected()
    {
        var client = WithSender(new CapturingEmailSender()).CreateClient();

        var confirm = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest("not-a-real-token", "newpassword"));

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }

    [Fact]
    public async Task Token_is_single_use()
    {
        var sender = new CapturingEmailSender();
        var client = WithSender(sender).CreateClient();
        const string email = "single-use@test.local";
        await RegisterAsync(client, email, "oldpassword");

        await client.PostAsJsonAsync("/api/auth/password-reset/request", new PasswordResetRequestRequest(email));
        var token = sender.LastToken!;

        var first = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(token, "newpassword1"));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/auth/password-reset/confirm",
            new PasswordResetConfirmRequest(token, "newpassword2"));
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }
}
