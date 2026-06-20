using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>
/// Проверка Google ID-токена по публичным ключам Google (JWKS) и настроенному client id
/// (<c>Auth:Google:ClientId</c>). Пока client id не задан, провайдер считается ненастроенным
/// и эндпоинт входа через Google возвращает понятную ошибку. Google Cloud проект/consent screen
/// настраиваются отдельно (см. docs/mvp-ux-account-readiness.md, пункт 5).
/// </summary>
public class GoogleIdTokenValidator(IConfiguration config) : IExternalIdentityValidator
{
    private const string CertsUrl = "https://www.googleapis.com/oauth2/v3/certs";
    private static readonly string[] ValidIssuers = ["accounts.google.com", "https://accounts.google.com"];
    private static readonly HttpClient Http = new();

    private static JsonWebKeySet? _keys;
    private static DateTime _keysFetchedUtc;

    private string? ClientId =>
        string.IsNullOrWhiteSpace(config["Auth:Google:ClientId"]) ? null : config["Auth:Google:ClientId"];

    public bool GoogleConfigured => ClientId is not null;

    public async Task<ExternalIdentityInfo> ValidateGoogleAsync(string idToken, CancellationToken ct = default)
    {
        var clientId = ClientId ?? throw new DomainRuleException("Вход через Google не настроен.");

        var keys = await GetKeysAsync(ct);
        var parameters = new TokenValidationParameters
        {
            ValidIssuers = ValidIssuers,
            ValidAudience = clientId,
            IssuerSigningKeys = keys.GetSigningKeys(),
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
        };

        var handler = new JwtSecurityTokenHandler();
        handler.InboundClaimTypeMap.Clear(); // сохранить исходные имена claim'ов (sub/email/...)

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(idToken, parameters, out _);
        }
        catch (Exception)
        {
            throw new DomainRuleException("Не удалось проверить токен Google.");
        }

        var sub = principal.FindFirst("sub")?.Value;
        var email = principal.FindFirst("email")?.Value;
        var emailVerified = string.Equals(principal.FindFirst("email_verified")?.Value, "true",
            StringComparison.OrdinalIgnoreCase);
        var name = principal.FindFirst("name")?.Value;

        if (string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email))
            throw new DomainRuleException("Токен Google не содержит обязательных полей.");

        return new ExternalIdentityInfo(sub, email, emailVerified, name);
    }

    private static async Task<JsonWebKeySet> GetKeysAsync(CancellationToken ct)
    {
        // Ключи Google ротируются редко — кэшируем на час.
        if (_keys is not null && DateTime.UtcNow - _keysFetchedUtc < TimeSpan.FromHours(1))
            return _keys;
        var json = await Http.GetStringAsync(CertsUrl, ct);
        _keys = new JsonWebKeySet(json);
        _keysFetchedUtc = DateTime.UtcNow;
        return _keys;
    }
}
