using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GenesysForge.Infrastructure.Auth;

public class TokenService(IConfiguration config) : ITokenService
{
    public const string Issuer = "GenesysForge";
    public const string DevFallbackKey = "genesysforge-dev-signing-key-change-in-production!";

    /// <summary>Время жизни access-токена по умолчанию — 7 дней (для MVP без refresh-токенов).</summary>
    public const int DefaultLifetimeMinutes = 60 * 24 * 7;

    public static SymmetricSecurityKey GetSigningKey(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? DevFallbackKey));

    /// <summary>
    /// Время жизни токена в минутах. Настраивается через <c>Jwt:LifetimeMinutes</c>
    /// (env <c>Jwt__LifetimeMinutes</c>); некорректное/≤0 значение откатывается к 7 дням.
    /// Задокументировано в docs/operator-notes.md.
    /// </summary>
    public static int GetLifetimeMinutes(IConfiguration config) =>
        int.TryParse(config["Jwt:LifetimeMinutes"], out var minutes) && minutes > 0
            ? minutes
            : DefaultLifetimeMinutes;

    public string CreateToken(User user)
    {
        var creds = new SigningCredentials(GetSigningKey(config), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Issuer,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.DisplayName),
            ],
            expires: DateTime.UtcNow.AddMinutes(GetLifetimeMinutes(config)),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
