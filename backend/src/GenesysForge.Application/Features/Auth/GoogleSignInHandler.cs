using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class GoogleSignInHandler(
    IAppDbContext db, ITokenService tokens, IPasswordHasherService hasher, IExternalIdentityValidator validator)
    : ICommandHandler<GoogleSignInCommand, AuthResponse>
{
    private const string Provider = "google";

    public async Task<AuthResponse> Handle(GoogleSignInCommand command, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command.Request.IdToken))
            throw new DomainRuleException("Не передан токен Google.");

        var info = await validator.ValidateGoogleAsync(command.Request.IdToken, ct);

        // 1) Уже привязанная личность — просто входим.
        var identity = await db.ExternalAuthIdentities
            .FirstOrDefaultAsync(i => i.Provider == Provider && i.ProviderUserId == info.ProviderUserId, ct);
        if (identity is not null)
        {
            var linked = await db.Users.FirstOrDefaultAsync(u => u.Id == identity.UserId, ct)
                ?? throw new DomainRuleException("Связанный аккаунт не найден.");
            return Auth(linked);
        }

        // Привязка/создание допустимы только при подтверждённом провайдером e-mail.
        if (!info.EmailVerified)
            throw new DomainRuleException("Google не подтвердил e-mail — войдите по паролю.");

        var email = info.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);

        // 2) Есть локальный аккаунт с этим (подтверждённым) e-mail — привязываем к нему.
        // 3) Нет — создаём новый аккаунт под Google-личность.
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                DisplayName = DisplayNameFrom(info, email),
                // Пароля у Google-аккаунта нет: ставим неиспользуемый хеш (вход только через Google).
                PasswordHash = "",
            };
            user.PasswordHash = hasher.Hash(user, Guid.NewGuid().ToString("N"));
            db.Users.Add(user);
        }

        db.ExternalAuthIdentities.Add(new ExternalAuthIdentity
        {
            Id = Guid.NewGuid(),
            Provider = Provider,
            ProviderUserId = info.ProviderUserId,
            UserId = user.Id,
            Email = email,
        });
        await db.SaveChangesAsync(ct);

        return Auth(user);
    }

    private AuthResponse Auth(User user) =>
        new(tokens.CreateToken(user), user.Id, user.Email, user.DisplayName);

    private static string DisplayNameFrom(ExternalIdentityInfo info, string email)
    {
        var name = info.Name?.Trim();
        if (!string.IsNullOrEmpty(name)) return name;
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }
}
