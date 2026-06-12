using System.Security.Claims;

namespace GenesysForge.Api.Endpoints;

public static class ClaimsPrincipalExtensions
{
    public static Guid UserId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? user.FindFirstValue("sub")
                   ?? throw new InvalidOperationException("Токен без идентификатора пользователя."));
}
