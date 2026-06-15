using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class RefundTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RefundTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid CharacterId)> CreateCharacterAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Undo Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
                [career.CareerSkillNames[0]]));
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return (client, reference, body!["id"]);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    [Fact]
    public async Task Characteristic_BuyThenRefund_RestoresXpAndValue()
    {
        var (client, _, id) = await CreateCharacterAsync();
        var before = await SheetAsync(client, id);

        await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null);
        var refund = await client.PostAsync($"/api/characters/{id}/characteristics/agility/refund", null);
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode);

        var after = await SheetAsync(client, id);
        Assert.Equal(before.Characteristics["agility"], after.Characteristics["agility"]);
        Assert.Equal(before.SpentXp, after.SpentXp);
    }

    [Fact]
    public async Task Characteristic_RefundBelowArchetypeBase_Rejected()
    {
        var (client, _, id) = await CreateCharacterAsync();
        var refund = await client.PostAsync($"/api/characters/{id}/characteristics/agility/refund", null);
        Assert.Equal(HttpStatusCode.BadRequest, refund.StatusCode);
    }

    [Fact]
    public async Task SkillRank_BuyThenRefund_RestoresXp_ButFreeRankProtected()
    {
        var (client, _, id) = await CreateCharacterAsync();
        var sheet = await SheetAsync(client, id);
        var freeSkill = sheet.Skills.First(s => s.Ranks == 1 && s.FreeRanks == 1);

        // купленный поверх бесплатного ранг возвращается
        await client.PostAsync($"/api/characters/{id}/skills/{freeSkill.SkillDefId}/buy-rank", null);
        var refund = await client.PostAsync($"/api/characters/{id}/skills/{freeSkill.SkillDefId}/refund-rank", null);
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(0, after.SpentXp);
        Assert.Equal(1, after.Skills.First(s => s.SkillDefId == freeSkill.SkillDefId).Ranks);

        // бесплатный стартовый ранг — нет
        var freeRefund = await client.PostAsync($"/api/characters/{id}/skills/{freeSkill.SkillDefId}/refund-rank", null);
        Assert.Equal(HttpStatusCode.BadRequest, freeRefund.StatusCode);
    }

    [Fact]
    public async Task Talent_RefundBlockedByPyramid_ThenAllowedTopDown()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var tier1 = reference.Talents.Where(t => t.Tier == 1 && !t.IsRanked).Take(2).ToList();
        var tier2 = reference.Talents.First(t => t.Tier == 2);

        foreach (var t in tier1)
            await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(t.Id));
        await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(tier2.Id));

        // 2×T1 + 1×T2: возврат T1 ломает пирамиду — отказ
        var blocked = await client.PostAsJsonAsync($"/api/characters/{id}/talents/refund", new BuyTalentRequest(tier1[0].Id));
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);

        // сверху вниз: сначала T2, затем T1 — ок, XP полностью возвращается
        var refundT2 = await client.PostAsJsonAsync($"/api/characters/{id}/talents/refund", new BuyTalentRequest(tier2.Id));
        Assert.Equal(HttpStatusCode.NoContent, refundT2.StatusCode);
        var refundT1 = await client.PostAsJsonAsync($"/api/characters/{id}/talents/refund", new BuyTalentRequest(tier1[0].Id));
        Assert.Equal(HttpStatusCode.NoContent, refundT1.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(5, sheet.SpentXp); // остался один T1
        Assert.Single(sheet.Talents);
    }

    [Fact]
    public async Task RankedTalent_RefundReturnsLastRankCost()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        var grit = reference.Talents.First(t => t.Name == "Упорство");
        var filler = reference.Talents.First(t => t.Tier == 1 && !t.IsRanked);

        await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(grit.Id));   // T1, 5
        await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(filler.Id)); // T1, 5
        await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(grit.Id));   // ранг 2 = T2, 10

        var refund = await client.PostAsJsonAsync($"/api/characters/{id}/talents/refund", new BuyTalentRequest(grit.Id));
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode);
        var sheet = await SheetAsync(client, id);
        Assert.Equal(10, sheet.SpentXp); // вернулись 10 за второй ранг (T2)
        Assert.Equal(1, sheet.Talents.First(t => t.Name == "Упорство").Ranks);
    }

    [Fact]
    public async Task Dedication_BuyGrantsChosenCharacteristic_RefundReverts()
    {
        var (client, reference, id) = await CreateCharacterAsync();
        // Достаточно XP, чтобы выстроить пирамиду до тира 5.
        await client.PatchAsJsonAsync($"/api/characters/{id}", new UpdateCharacterRequest(null, 400, null, null));

        // Пирамида 5/4/3/2 из неранговых талантов открывает покупку тира 5.
        int[] need = [0, 5, 4, 3, 2];
        foreach (var tier in new[] { 1, 2, 3, 4 })
            foreach (var talent in reference.Talents.Where(x => x.Tier == tier && !x.IsRanked).Take(need[tier]))
            {
                var r = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(talent.Id));
                Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
            }

        var dedication = reference.Talents.First(t => t.Name == "Повышение");
        Assert.True(dedication.GrantsCharacteristic);

        var before = await SheetAsync(client, id);
        var baseBrawn = before.Characteristics["brawn"];

        // Без выбора характеристики покупка отклоняется.
        var noPick = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(dedication.Id));
        Assert.Equal(HttpStatusCode.BadRequest, noPick.StatusCode);

        // С выбором — характеристика увеличивается на 1.
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(dedication.Id, CharacteristicType.Brawn));
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(baseBrawn + 1, after.Characteristics["brawn"]);
        Assert.Equal([CharacteristicType.Brawn], after.Talents.First(t => t.Name == "Повышение").GrantedCharacteristics);

        // Повторно ту же характеристику этим талантом нельзя.
        var dup = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(dedication.Id, CharacteristicType.Brawn));
        Assert.Equal(HttpStatusCode.BadRequest, dup.StatusCode);

        // Возврат таланта откатывает увеличение характеристики.
        var refund = await client.PostAsJsonAsync($"/api/characters/{id}/talents/refund", new BuyTalentRequest(dedication.Id));
        Assert.Equal(HttpStatusCode.NoContent, refund.StatusCode);
        var reverted = await SheetAsync(client, id);
        Assert.Equal(baseBrawn, reverted.Characteristics["brawn"]);
    }

    [Fact]
    public async Task Refund_AfterCreationComplete_Rejected()
    {
        var (client, _, id) = await CreateCharacterAsync();
        await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null);
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var refund = await client.PostAsync($"/api/characters/{id}/characteristics/agility/refund", null);
        Assert.Equal(HttpStatusCode.BadRequest, refund.StatusCode);
    }
}
