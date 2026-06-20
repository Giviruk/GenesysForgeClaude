using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenesysForge.Api.Tests;

/// <summary>Перехватывает «письмо» подтверждения, чтобы тест узнал одноразовый токен.</summary>
public sealed class CapturingConfirmationEmailSender : IEmailSender
{
    public string? LastEmail { get; private set; }
    public string? LastToken { get; private set; }
    public int Sent { get; private set; }

    public Task SendEmailConfirmationAsync(string email, string rawToken, CancellationToken ct = default)
    {
        LastEmail = email;
        LastToken = rawToken;
        Sent++;
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string rawToken, CancellationToken ct = default)
        => Task.CompletedTask; // не интересует в тестах подтверждения e-mail
}

public class EmailConfirmationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _baseFactory;
    public EmailConfirmationTests(ApiFactory factory) => _baseFactory = factory;

    private WebApplicationFactory<Program> Build(CapturingConfirmationEmailSender sender, bool requireConfirmation = false) =>
        _baseFactory.WithWebHostBuilder(b =>
        {
            if (requireConfirmation) b.UseSetting("Auth:RequireEmailConfirmation", "true");
            b.ConfigureServices(s =>
            {
                s.RemoveAll<IEmailSender>();
                s.AddSingleton<IEmailSender>(sender);
            });
        });

    private static async Task RegisterAsync(HttpClient client, string email, string password)
    {
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, password, "Confirm User"));
        resp.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_sends_a_confirmation_email()
    {
        var sender = new CapturingConfirmationEmailSender();
        var client = Build(sender).CreateClient();

        await RegisterAsync(client, "confirm-send@test.local", "password123");

        Assert.Equal(1, sender.Sent);
        Assert.Equal("confirm-send@test.local", sender.LastEmail);
        Assert.False(string.IsNullOrEmpty(sender.LastToken));
    }

    [Fact]
    public async Task When_required_login_is_blocked_until_confirmed()
    {
        var sender = new CapturingConfirmationEmailSender();
        var client = Build(sender, requireConfirmation: true).CreateClient();
        const string email = "gated@test.local";
        await RegisterAsync(client, email, "password123");

        // До подтверждения вход запрещён.
        var before = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "password123"));
        Assert.Equal(HttpStatusCode.Unauthorized, before.StatusCode);

        var confirm = await client.PostAsJsonAsync("/api/auth/email/confirm",
            new ConfirmEmailRequest(sender.LastToken!));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        // После подтверждения вход разрешён.
        var after = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "password123"));
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
    }

    [Fact]
    public async Task When_not_required_unconfirmed_user_can_log_in()
    {
        var sender = new CapturingConfirmationEmailSender();
        var client = Build(sender).CreateClient(); // requireConfirmation = false (приватный MVP)
        const string email = "ungated@test.local";
        await RegisterAsync(client, email, "password123");

        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "password123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
    }

    [Fact]
    public async Task Resend_sends_again_for_unconfirmed()
    {
        var sender = new CapturingConfirmationEmailSender();
        var client = Build(sender).CreateClient();
        const string email = "resend@test.local";
        await RegisterAsync(client, email, "password123"); // Sent == 1

        var resend = await client.PostAsJsonAsync("/api/auth/email/resend",
            new ResendEmailConfirmationRequest(email));
        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);
        Assert.Equal(2, sender.Sent);
    }

    [Fact]
    public async Task Resend_for_unknown_email_returns_204_and_sends_nothing()
    {
        var sender = new CapturingConfirmationEmailSender();
        var client = Build(sender).CreateClient();

        var resend = await client.PostAsJsonAsync("/api/auth/email/resend",
            new ResendEmailConfirmationRequest("nobody@test.local"));

        Assert.Equal(HttpStatusCode.NoContent, resend.StatusCode);
        Assert.Equal(0, sender.Sent);
    }

    [Fact]
    public async Task Confirm_with_invalid_token_is_rejected()
    {
        var client = Build(new CapturingConfirmationEmailSender()).CreateClient();

        var confirm = await client.PostAsJsonAsync("/api/auth/email/confirm",
            new ConfirmEmailRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
    }
}
