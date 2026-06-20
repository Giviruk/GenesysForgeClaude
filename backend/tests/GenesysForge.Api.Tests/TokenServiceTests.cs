using System.IdentityModel.Tokens.Jwt;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace GenesysForge.Api.Tests;

public class TokenServiceTests
{
    private static User SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "operator@example.com",
        DisplayName = "Operator",
        PasswordHash = "x",
    };

    private static IConfiguration Config(params (string Key, string? Value)[] pairs) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(pairs.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    // ValidFrom в токене не проставляется, поэтому считаем срок от «сейчас».
    // exp хранится в целых секундах + проходит доля секунды на создание/проверку,
    // поэтому допускаем погрешность в 1 минуту.
    private static void AssertLifetimeMinutes(int expected, string jwt)
    {
        var token = new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        var minutes = (token.ValidTo - DateTime.UtcNow).TotalMinutes;
        Assert.InRange(minutes, expected - 1, expected + 0.1);
    }

    [Fact]
    public void Default_lifetime_is_short()
    {
        // С refresh-токенами access-токен короткий (30 минут по умолчанию).
        var jwt = new TokenService(Config()).CreateToken(SampleUser());
        AssertLifetimeMinutes(TokenService.DefaultAccessLifetimeMinutes, jwt);
        Assert.Equal(30, TokenService.DefaultAccessLifetimeMinutes);
    }

    [Fact]
    public void Configured_access_lifetime_is_honored()
    {
        var jwt = new TokenService(Config(("Jwt:AccessLifetimeMinutes", "45"))).CreateToken(SampleUser());
        AssertLifetimeMinutes(45, jwt);
    }

    [Fact]
    public void Legacy_lifetime_key_is_still_read()
    {
        // Старый ключ Jwt:LifetimeMinutes поддерживается для совместимости.
        var jwt = new TokenService(Config(("Jwt:LifetimeMinutes", "120"))).CreateToken(SampleUser());
        AssertLifetimeMinutes(120, jwt);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("not-a-number")]
    public void Invalid_lifetime_falls_back_to_default(string value)
    {
        var jwt = new TokenService(Config(("Jwt:AccessLifetimeMinutes", value))).CreateToken(SampleUser());
        AssertLifetimeMinutes(TokenService.DefaultAccessLifetimeMinutes, jwt);
    }
}
