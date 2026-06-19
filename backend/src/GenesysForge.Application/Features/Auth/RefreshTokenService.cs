using System.Security.Cryptography;
using System.Text;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Auth;

public class RefreshTokenService(IAppDbContext db, ITokenService tokens) : IRefreshTokenService
{
    /// <summary>Время жизни refresh-токена (сдвигается при каждой ротации).</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromDays(30);

    public async Task<(string RawToken, DateTime ExpiresAt)> IssueAsync(
        Guid userId, RequestMeta meta, CancellationToken ct = default)
    {
        var (raw, expires, _) = Create(userId, Guid.NewGuid(), meta);
        await db.SaveChangesAsync(ct);
        return (raw, expires);
    }

    public async Task<RefreshRotation> RotateAsync(string rawRefresh, RequestMeta meta, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefresh))
            throw new UnauthorizedException("Сессия недействительна. Войдите снова.");

        var hash = Hash(rawRefresh);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedException("Сессия недействительна. Войдите снова.");

        var now = DateTime.UtcNow;

        // Повтор уже отозванного токена → компрометация: гасим всё семейство и заставляем перелогиниться.
        if (token.RevokedAt is not null)
        {
            await RevokeFamily(token.FamilyId, now, ct);
            await db.SaveChangesAsync(ct);
            throw new UnauthorizedException("Сессия скомпрометирована. Войдите снова.");
        }
        if (now >= token.ExpiresAt)
            throw new UnauthorizedException("Сессия истекла. Войдите снова.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == token.UserId, ct)
            ?? throw new UnauthorizedException("Пользователь не найден.");

        // Ротация в том же семействе: старый помечаем отозванным и связываем с новым.
        var (newRaw, newExpires, newId) = Create(user.Id, token.FamilyId, meta);
        token.RevokedAt = now;
        token.ReplacedByTokenId = newId;
        await db.SaveChangesAsync(ct);

        return new RefreshRotation(tokens.CreateToken(user), newRaw, newExpires, user.Id, user.Email, user.DisplayName);
    }

    public async Task RevokeFamilyAsync(string rawRefresh, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawRefresh)) return;
        var hash = Hash(rawRefresh);
        var token = await db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (token is null) return;
        await RevokeFamily(token.FamilyId, DateTime.UtcNow, ct);
        await db.SaveChangesAsync(ct);
    }

    private async Task RevokeFamily(Guid familyId, DateTime now, CancellationToken ct)
    {
        var active = await db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active) t.RevokedAt = now;
    }

    private (string Raw, DateTime Expires, Guid Id) Create(Guid userId, Guid familyId, RequestMeta meta)
    {
        var raw = NewRawToken();
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var expires = now.Add(Lifetime);
        db.RefreshTokens.Add(new RefreshToken
        {
            Id = id,
            UserId = userId,
            TokenHash = Hash(raw),
            FamilyId = familyId,
            ExpiresAt = expires,
            CreatedAt = now,
            UserAgent = Truncate(meta.UserAgent, 400),
            CreatedByIp = Truncate(meta.Ip, 64),
        });
        return (raw, expires, id);
    }

    private static string NewRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    private static string? Truncate(string? value, int max) =>
        value is null || value.Length <= max ? value : value[..max];
}
