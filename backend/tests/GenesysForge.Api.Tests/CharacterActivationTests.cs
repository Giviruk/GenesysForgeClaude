using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

public class CharacterActivationTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterActivationTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ActivateAbility_AppliesEffect_AndWritesAudit()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;

        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Герой", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        // «Трудно убить» — структурный эффект +4 к поглощению (U-18).
        var heroic = reference.HeroicAbilities.First(h => h.Code == "rot.heroic.hard-to-kill");
        var setResp = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new { heroicAbilityId = heroic.Id }, Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, setResp.StatusCode);

        var actResp = await client.PostAsync($"/api/characters/{id}/activate-ability", null);
        Assert.Equal(HttpStatusCode.OK, actResp.StatusCode);
        var result = (await actResp.Content.ReadFromJsonAsync<ActivateCharacterAbilityResult>(Json.Options))!;

        Assert.Contains("Трудно убить", result.AbilityName);
        Assert.NotEmpty(result.Applied); // +4 к поглощению показано как применённое

        // Активация записана в историю персонажа (audit-log, U-09).
        var audit = (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options))!;
        Assert.Contains(audit, a => a.Action == CharacterAuditAction.AbilityActivated);
    }

    [Fact]
    public async Task ActivateAbility_WithoutHeroic_IsRejected()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Безгеройный", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var resp = await client.PostAsync($"/api/characters/{id}/activate-ability", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
