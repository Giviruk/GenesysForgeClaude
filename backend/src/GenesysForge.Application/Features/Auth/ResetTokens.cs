using System.Security.Cryptography;
using System.Text;

namespace GenesysForge.Application.Features.Auth;

/// <summary>Генерация и хеширование одноразовых токенов сброса пароля.</summary>
internal static class ResetTokens
{
    /// <summary>Криптослучайный токен (URL-safe base64, 256 бит энтропии).</summary>
    public static string NewRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>SHA-256 хеш токена (хранится в БД; hex, 64 символа).</summary>
    public static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
