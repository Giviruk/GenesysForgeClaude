using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

public class HistoryUndoTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public HistoryUndoTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("History Undo Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id,
                career.Id, [career.CareerSkillNames[0]]));
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return (client, reference, body!["id"]);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static async Task<List<CharacterAuditEntryDto>> AuditAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>($"/api/characters/{id}/audit", Json.Options))!;

    private static async Task CompleteAndAwardAsync(HttpClient client, Guid id, int amount = 30)
    {
        var complete = await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        var award = await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards",
            new AwardXpRequest(amount, "История undo"), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, award.StatusCode);
    }

    [Fact]
    public async Task HistoryUndo_SkillPurchaseAfterCreation_ReturnsXp()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var career = reference.Careers[0];
        var before = await SheetAsync(client, id);
        var skill = reference.Skills.First(def => !career.CareerSkillNames.Contains(def.Name)
            && before.Skills.Single(row => row.SkillDefId == def.Id).FreeRanks == 0);

        await CompleteAndAwardAsync(client, id);
        var buy = await client.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        var afterBuy = await SheetAsync(client, id);
        var purchase = Assert.Single(await AuditAsync(client, id),
            entry => entry.Action == CharacterAuditAction.SkillRankBought);
        Assert.True(purchase.CanUndo);

        var undo = await client.PostAsync($"/api/characters/{id}/audit/{purchase.Id}/undo", null);
        Assert.Equal(HttpStatusCode.NoContent, undo.StatusCode);

        var afterUndo = await SheetAsync(client, id);
        Assert.Equal(afterBuy.SpentXp - 10, afterUndo.SpentXp);
        Assert.Equal(0, afterUndo.Skills.Single(row => row.SkillDefId == skill.Id).Ranks);
        var history = await AuditAsync(client, id);
        Assert.Contains(history, entry => entry.Action == CharacterAuditAction.SkillRankRefunded
            && entry.Summary.StartsWith("Откат покупки", StringComparison.Ordinal));
        Assert.False(history.Single(entry => entry.Id == purchase.Id).CanUndo);
    }

    [Fact]
    public async Task HistoryUndo_TalentPurchaseAfterCreation_ReturnsXp()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var talent = reference.Talents.First(def => def.Tier == 1 && !def.IsRanked);

        await CompleteAndAwardAsync(client, id);
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(talent.Id), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        var afterBuy = await SheetAsync(client, id);
        var purchase = Assert.Single(await AuditAsync(client, id),
            entry => entry.Action == CharacterAuditAction.TalentBought);
        Assert.True(purchase.CanUndo);

        var undo = await client.PostAsync($"/api/characters/{id}/audit/{purchase.Id}/undo", null);
        Assert.Equal(HttpStatusCode.NoContent, undo.StatusCode);

        var afterUndo = await SheetAsync(client, id);
        Assert.Equal(afterBuy.SpentXp - 5, afterUndo.SpentXp);
        Assert.DoesNotContain(afterUndo.Talents!, row => row.TalentDefId == talent.Id);
    }

    [Fact]
    public async Task HistoryUndo_TalentPyramidStillAppliesAfterCreation()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var tier1 = reference.Talents.Where(talent => talent.Tier == 1 && !talent.IsRanked).Take(2).ToList();
        var tier2 = reference.Talents.First(talent => talent.Tier == 2 && !talent.IsRanked);

        await CompleteAndAwardAsync(client, id, 50);
        foreach (var talent in tier1)
            Assert.Equal(HttpStatusCode.NoContent,
                (await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                    new BuyTalentRequest(talent.Id), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                new BuyTalentRequest(tier2.Id), Json.Options)).StatusCode);

        var history = await AuditAsync(client, id);
        var firstTierPurchase = history.Last(entry => entry.Action == CharacterAuditAction.TalentBought
            && entry.Summary.Contains(tier1[0].Name, StringComparison.Ordinal));
        var tier2Purchase = history.Single(entry => entry.Action == CharacterAuditAction.TalentBought
            && entry.Summary.Contains(tier2.Name, StringComparison.Ordinal));
        Assert.False(firstTierPurchase.CanUndo);
        Assert.True(tier2Purchase.CanUndo);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/audit/{tier2Purchase.Id}/undo", null)).StatusCode);
        history = await AuditAsync(client, id);
        firstTierPurchase = history.Single(entry => entry.Id == firstTierPurchase.Id);
        Assert.True(firstTierPurchase.CanUndo);
    }

    [Fact]
    public async Task HistoryUndo_OldPurchaseCannotUndoAgainAfterNewRank()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var career = reference.Careers[0];
        var before = await SheetAsync(client, id);
        var skill = reference.Skills.First(def => !career.CareerSkillNames.Contains(def.Name)
            && before.Skills.Single(row => row.SkillDefId == def.Id).FreeRanks == 0);

        await CompleteAndAwardAsync(client, id, 50);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null)).StatusCode);

        var history = await AuditAsync(client, id);
        var purchases = history.Where(entry => entry.Action == CharacterAuditAction.SkillRankBought).ToList();
        Assert.Equal(2, purchases.Count);
        Assert.False(purchases[1].CanUndo); // история отсортирована от новой к старой
        Assert.True(purchases[0].CanUndo);

        var stale = await client.PostAsync($"/api/characters/{id}/audit/{purchases[1].Id}/undo", null);
        Assert.Equal(HttpStatusCode.BadRequest, stale.StatusCode);
    }
}
