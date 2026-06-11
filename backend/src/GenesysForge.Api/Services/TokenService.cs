using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GenesysForge.Api.Data;
using Microsoft.IdentityModel.Tokens;

namespace GenesysForge.Api.Services;

public class TokenService(IConfiguration config)
{
    public const string DevFallbackKey = "genesysforge-dev-signing-key-change-in-production!";

    public static SymmetricSecurityKey GetSigningKey(IConfiguration config) =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? DevFallbackKey));

    public string CreateToken(User user)
    {
        var creds = new SigningCredentials(GetSigningKey(config), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "GenesysForge",
            audience: "GenesysForge",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("name", user.DisplayName),
            ],
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
