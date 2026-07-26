using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Api.Tests;

/// <summary>ROT-CMB-01: разрешение атаки авторитетно на сервере, а не в клиентской сводке.</summary>
public class CombatResolveApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<ResolveAttackResponse> ResolveAsync(HttpClient client, ResolveAttackRequest req)
    {
        var resp = await client.PostAsJsonAsync("/api/combat/resolve-attack", req, Json.Options);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<ResolveAttackResponse>(Json.Options))!;
    }

    [Fact]
    public async Task Hit_ReportsRawDamageAndDamageAfterSoak()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, new ResolveAttackRequest(
            NetSuccesses: 2, BaseDamage: 7, TargetSoak: 4));

        Assert.True(result.IsHit);
        Assert.Equal(9, result.RawDamagePerHit);
        Assert.Equal(5, result.TotalApplied);
        Assert.Equal(5, Assert.Single(result.Hits).Applied);
    }

    [Fact]
    public async Task Miss_ReturnsNoDamageFields()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, new ResolveAttackRequest(
            NetSuccesses: 0, BaseDamage: 9, TargetSoak: 0, Triumphs: 1));

        Assert.False(result.IsHit);
        Assert.Null(result.RawDamagePerHit);
        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public async Task MultiHit_PassesEachHitThroughSoakSeparately()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, new ResolveAttackRequest(
            NetSuccesses: 1, BaseDamage: 7, TargetSoak: 5,
            AdditionalHits: [new AttackHitRequest(5, "Linked")]));

        Assert.Equal([3, 3], result.Hits.Select(h => h.Applied));
        Assert.Equal(6, result.TotalApplied);
    }

    [Fact]
    public async Task InvalidSpends_AreReportedInsteadOfSilentlyApplied()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, new ResolveAttackRequest(
            NetSuccesses: 0, BaseDamage: 6, TargetSoak: 0, NetAdvantages: 2,
            Spends:
            [
                new AttackSpendRequest("knockdown"),
                new AttackSpendRequest("blast", MayActivateOnMiss: true),
            ]));

        Assert.Equal(["blast"], result.AllowedSymbolSpends);
        Assert.Equal(["knockdown"], result.RejectedSymbolSpends);
    }

    [Fact]
    public async Task NegativeProfile_IsRejected()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var resp = await client.PostAsJsonAsync("/api/combat/resolve-attack",
            new ResolveAttackRequest(1, -3, 0), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Endpoint_RequiresAuthorization()
    {
        var client = factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/combat/resolve-attack",
            new ResolveAttackRequest(1, 5, 0), Json.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }
}
