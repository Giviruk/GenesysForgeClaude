using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class LoginHandler(IAppDbContext db, ITokenService tokens, IPasswordHasherService hasher, IAuthPolicy policy)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand command, CancellationToken ct = default)
    {
        var email = (command.Request.Email ?? "").Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is null || !hasher.Verify(user, user.PasswordHash, command.Request.Password ?? ""))
            throw new UnauthorizedException("Неверный e-mail или пароль.");
        if (policy.RequireEmailConfirmation && !user.EmailConfirmed)
            throw new UnauthorizedException("Подтвердите e-mail по ссылке из письма, прежде чем войти.");
        return new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName);
    }
}
