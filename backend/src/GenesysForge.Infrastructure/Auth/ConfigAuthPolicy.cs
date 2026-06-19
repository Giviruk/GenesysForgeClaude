using GenesysForge.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace GenesysForge.Infrastructure.Auth;

/// <summary>Политика аутентификации из конфигурации (env <c>Auth__RequireEmailConfirmation</c>).</summary>
public class ConfigAuthPolicy(IConfiguration config) : IAuthPolicy
{
    public bool RequireEmailConfirmation => config.GetValue<bool>("Auth:RequireEmailConfirmation");
}
