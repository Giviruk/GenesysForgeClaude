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

        // Сброс пароля. Запрос всегда отвечает 204 (не раскрывает наличие аккаунта);
        // подтверждение по одноразовому токену из письма.
        group.MapPost("/password-reset/request", async (PasswordResetRequestRequest req,
            ICommandHandler<RequestPasswordResetCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new RequestPasswordResetCommand(req), ct);
            return Results.NoContent();
        });

        group.MapPost("/password-reset/confirm", async (PasswordResetConfirmRequest req,
            ICommandHandler<ConfirmPasswordResetCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ConfirmPasswordResetCommand(req), ct);
            return Results.NoContent();
        });

        // Подтверждение e-mail по токену из письма + повторная отправка (всегда 204, без раскрытия аккаунта).
        group.MapPost("/email/confirm", async (ConfirmEmailRequest req,
            ICommandHandler<ConfirmEmailCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ConfirmEmailCommand(req), ct);
            return Results.NoContent();
        });

        group.MapPost("/email/resend", async (ResendEmailConfirmationRequest req,
            ICommandHandler<ResendEmailConfirmationCommand, Unit> handler, CancellationToken ct) =>
        {
            await handler.Handle(new ResendEmailConfirmationCommand(req), ct);
            return Results.NoContent();
        });
    }
}
