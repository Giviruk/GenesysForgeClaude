using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class RegisterUserHandler(
    IAppDbContext db, ITokenService tokens, IPasswordHasherService hasher)
    : ICommandHandler<RegisterUserCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterUserCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            throw new DomainRuleException("Укажите корректный e-mail.");
        if ((req.Password?.Length ?? 0) < 6)
            throw new DomainRuleException("Пароль должен быть не короче 6 символов.");
        if (string.IsNullOrWhiteSpace(req.DisplayName))
            throw new DomainRuleException("Укажите имя пользователя.");

        var email = req.Email.Trim().ToLowerInvariant();
        if (await db.Users.AnyAsync(u => u.Email == email, ct))
            throw new ConflictException("Пользователь с таким e-mail уже зарегистрирован.");

        var user = new User
        {
            Id = Guid.NewGuid(), Email = email, DisplayName = req.DisplayName.Trim(),
            PasswordHash = "",
        };
        user.PasswordHash = hasher.Hash(user, req.Password!);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName);
    }
}
