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

    /// <summary>Короткое время жизни access-токена при наличии refresh-токенов — 30 минут.</summary>
    public const int DefaultAccessLifetimeMinutes = 30;

    public static SymmetricSecurityKey GetSigningKey(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? DevFallbackKey));

    /// <summary>
    /// Время жизни access-токена в минутах. Основной ключ — <c>Jwt:AccessLifetimeMinutes</c>;
    /// для совместимости читается также <c>Jwt:LifetimeMinutes</c>. По умолчанию 30 минут —
    /// сессия продлевается refresh-токеном. Некорректное/≤0 значение откатывается к умолчанию.
    /// Задокументировано в docs/operator-notes.md.
    /// </summary>
    public static int GetAccessLifetimeMinutes(IConfiguration config) =>
        TryMinutes(config["Jwt:AccessLifetimeMinutes"])
        ?? TryMinutes(config["Jwt:LifetimeMinutes"])
        ?? DefaultAccessLifetimeMinutes;

    private static int? TryMinutes(string? value) =>
        int.TryParse(value, out var minutes) && minutes > 0 ? minutes : null;

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
            expires: DateTime.UtcNow.AddMinutes(GetAccessLifetimeMinutes(config)),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
