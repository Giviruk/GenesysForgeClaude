using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

public class CharacterAuditTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterAuditTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Audit Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
                [career.CareerSkillNames[0]]));
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return (client, reference, body!["id"]);
    }

    private static async Task<List<CharacterAuditEntryDto>> AuditAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>($"/api/characters/{id}/audit", Json.Options))!;

    [Fact]
    public async Task BuyCharacteristic_WritesAuditWithNegativeDelta()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null);

        var audit = await AuditAsync(client, id);
        var entry = Assert.Single(audit, a => a.Action == CharacterAuditAction.CharacteristicBought);
        Assert.True(entry.XpDelta < 0); // покупка уменьшает доступный XP
        Assert.Contains("Ловкость", entry.Summary);
    }

    [Fact]
    public async Task Refund_WritesAuditWithPositiveDelta_AndIsNewestFirst()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null);
        await client.PostAsync($"/api/characters/{id}/characteristics/agility/refund", null);

        var audit = await AuditAsync(client, id);
        // новые записи первыми → refund сверху
        Assert.Equal(CharacterAuditAction.CharacteristicRefunded, audit[0].Action);
        Assert.True(audit[0].XpDelta > 0);
        Assert.Equal(0, audit[0].SpentXpAfter); // состояние после: всё вернулось
    }

    [Fact]
    public async Task AwardXp_IncreasesTotalXp_AndLogsAward()
    {
        var (client, _, id) = await CreateCharacterAsync();

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards",
            new AwardXpRequest(15, "За сессию"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        var audit = await AuditAsync(client, id);
        var entry = Assert.Single(audit, a => a.Action == CharacterAuditAction.XpAwarded);
        Assert.Equal(15, entry.XpDelta);
        Assert.Equal(sheet.TotalXp, entry.TotalXpAfter);
        Assert.Contains("За сессию", entry.Summary);
    }

    [Fact]
    public async Task AwardXp_CannotDropTotalBelowSpent()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null); // тратит XP

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards",
            new AwardXpRequest(-1000, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task ManualXpEdit_ViaPatch_LogsManualEdit()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, 120, null, null), Json.Options);

        var audit = await AuditAsync(client, id);
        var entry = Assert.Single(audit, a => a.Action == CharacterAuditAction.ManualEdit);
        Assert.Equal(120, entry.TotalXpAfter);
    }

    [Fact]
    public async Task Audit_NotVisibleToOtherUser()
    {
        var (_, _, id) = await CreateCharacterAsync();
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/characters/{id}/audit");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // чужой персонаж не найден
    }

    [Fact]
    public async Task CompleteCreation_LogsOnceAndIsIdempotent()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        await client.PostAsync($"/api/characters/{id}/complete-creation", null); // повтор

        var audit = await AuditAsync(client, id);
        Assert.Single(audit, a => a.Action == CharacterAuditAction.CreationCompleted);
    }
}
