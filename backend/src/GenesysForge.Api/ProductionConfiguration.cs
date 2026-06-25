using GenesysForge.Infrastructure.Auth;

namespace GenesysForge.Api;

public static class ProductionConfiguration
{
    public static void Validate(IConfiguration config, IHostEnvironment environment)
    {
        if (!environment.IsProduction()) return;

        var jwtKey = config["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) ||
            jwtKey.Length < 32 ||
            jwtKey == TokenService.DevFallbackKey ||
            jwtKey.Contains("change-me", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production requires Jwt:Key with at least 32 characters and no default/change-me value.");
        }

        var origins = ParseCorsOrigins(config);
        if (origins.Length == 0)
            throw new InvalidOperationException("Production requires at least one Cors:Origins value.");

        foreach (var origin in origins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                uri.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrWhiteSpace(uri.Host) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                (uri.AbsolutePath != string.Empty && uri.AbsolutePath != "/") ||
                !string.IsNullOrEmpty(uri.Query) ||
                !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidOperationException(
                    $"Production CORS origin must be an HTTPS origin without a path: '{origin}'.");
            }
        }
    }

    public static string[] ParseCorsOrigins(IConfiguration config) =>
        (config["Cors:Origins"] ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
