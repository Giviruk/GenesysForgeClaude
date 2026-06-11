using GenesysForge.Api.Contracts;
using GenesysForge.Api.Data;
using GenesysForge.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuth(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest req, AppDbContext db, TokenService tokens,
            IPasswordHasher<User> hasher) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
                return Results.BadRequest(new ErrorResponse("Укажите корректный e-mail."));
            if ((req.Password?.Length ?? 0) < 6)
                return Results.BadRequest(new ErrorResponse("Пароль должен быть не короче 6 символов."));
            if (string.IsNullOrWhiteSpace(req.DisplayName))
                return Results.BadRequest(new ErrorResponse("Укажите имя пользователя."));

            var email = req.Email.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Email == email))
                return Results.Conflict(new ErrorResponse("Пользователь с таким e-mail уже зарегистрирован."));

            var user = new User { Id = Guid.NewGuid(), Email = email, DisplayName = req.DisplayName.Trim(), PasswordHash = "" };
            user.PasswordHash = hasher.HashPassword(user, req.Password!);
            db.Users.Add(user);
            await db.SaveChangesAsync();

            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName));
        });

        group.MapPost("/login", async (LoginRequest req, AppDbContext db, TokenService tokens,
            IPasswordHasher<User> hasher) =>
        {
            var email = (req.Email ?? "").Trim().ToLowerInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null ||
                hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password ?? "") == PasswordVerificationResult.Failed)
            {
                return Results.Json(new ErrorResponse("Неверный e-mail или пароль."), statusCode: StatusCodes.Status401Unauthorized);
            }
            return Results.Ok(new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName));
        });
    }
}
