using System.Security.Cryptography;
using System.Text;

namespace GenesysForge.Application.Features.HomebrewPacks;

internal static class HomebrewPackTokens
{
    public static string NewRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
