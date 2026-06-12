using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Auth;

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
    }
}
