using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;
using Microsoft.Extensions.Configuration;

namespace GenesysForge.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req,
                ICommandHandler<RegisterUserCommand, AuthResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new RegisterUserCommand(req), ct)));

        group.MapPost("/login", async (LoginRequest req,
                ICommandHandler<LoginCommand, AuthResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new LoginCommand(req), ct)));

        // Вход через Google: фронтенд присылает ID-токен от Google Identity Services.
        group.MapPost("/google", async (GoogleSignInRequest req,
                ICommandHandler<GoogleSignInCommand, AuthResponse> handler, CancellationToken ct) =>
            Results.Ok(await handler.Handle(new GoogleSignInCommand(req), ct)));

        // Какие внешние провайдеры входа доступны (для отрисовки кнопок). Публично.
        group.MapGet("/providers", (IConfiguration config) =>
            Results.Ok(new AuthProvidersResponse(config["Auth:Google:ClientId"])));
    }
}
