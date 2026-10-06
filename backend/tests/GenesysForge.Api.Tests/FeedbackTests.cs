using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Feedback;
using GenesysForge.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GenesysForge.Api.Tests;

public class FeedbackTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private (HttpClient Client, CapturingEmailSender Sender) Create(WebApplicationFactory<Program>? source = null)
    {
        var sender = new CapturingEmailSender();
        var client = (source ?? factory).WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IEmailSender>();
            s.AddSingleton<IEmailSender>(sender);
        })).CreateClient();
        return (client, sender);
    }

    [Fact]
    public async Task Anonymous_feedback_is_forwarded_with_reply_address()
    {
        var (client, sender) = Create();
        var resp = await client.PostAsJsonAsync("/api/feedback",
            new SendFeedbackRequest("  Не работает печать листа  ", "player@test.local", "/characters/1"));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal(new FeedbackMessage("Не работает печать листа", "player@test.local", null, "/characters/1"),
            sender.LastFeedback);
    }

    [Fact]
    public async Task Signed_in_user_without_email_gets_account_address_as_reply_to()
    {
        var (client, sender) = Create();
        var register = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("feedback-user@test.local", "password123", "Feedback User"));
        var auth = (await register.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);

        var resp = await client.PostAsJsonAsync("/api/feedback", new SendFeedbackRequest("Спасибо за проект!"));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal("feedback-user@test.local", sender.LastFeedback!.AccountEmail);
        Assert.Equal("feedback-user@test.local", sender.LastFeedback.ReplyTo);
    }

    [Theory]
    [InlineData("hi", null)]
    [InlineData("Нормальное сообщение", "not-an-email")]
    public async Task Invalid_feedback_is_rejected_without_sending(string message, string? email)
    {
        var (client, sender) = Create();
        var resp = await client.PostAsJsonAsync("/api/feedback", new SendFeedbackRequest(message, email));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal(0, sender.Sent);
    }

    [Fact]
    public async Task Honeypot_filled_by_a_bot_is_accepted_silently_and_dropped()
    {
        var (client, sender) = Create();
        var resp = await client.PostAsJsonAsync("/api/feedback",
            new SendFeedbackRequest("Buy cheap stuff", Website: "http://spam.example"));

        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);
        Assert.Equal(0, sender.Sent);
    }

    [Fact]
    public async Task Feedback_is_rate_limited()
    {
        using var limited = new RateLimitedApiFactory();
        var (client, _) = Create(limited);
        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++)
            statuses.Add((await client.PostAsJsonAsync("/api/feedback", new SendFeedbackRequest("Сообщение номер " + i))).StatusCode);

        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.TooManyRequests], statuses);
    }

    [Fact]
    public void Mail_body_carries_the_message_and_where_it_came_from()
    {
        var text = FeedbackText.Format(new FeedbackMessage("Текст", null, "user@test.local", "/shop"), "https://genesys-forge.com");
        Assert.StartsWith("Текст\n", text);
        Assert.Contains("Сайт: https://genesys-forge.com", text);
        Assert.Contains("Страница: /shop", text);
        Assert.Contains("Аккаунт: user@test.local", text);
        Assert.Contains("Ответить на: адрес не указан", text);
    }

    [Fact]
    public void Support_address_is_the_default_recipient() =>
        Assert.Equal("genesys-forge.support@genesys-forge.com", new EmailOptions().FeedbackTo);
}
