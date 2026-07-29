using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-MAG-10: лист персонажа сам говорит, откуда берётся рейтинг эффектов заклинания. Список
/// источников считает сервер — открывает второй из них талант, и решать это на клиенте нельзя.
/// </summary>
public class KnowledgeRatingApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateCharacterAsync(
        GameSystem system = GameSystem.RealmsOfTerrinoth)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/{system}", Json.Options))!;
        // Вид берётся любой встроенный: имена наборов у систем разные, а задача не про вид.
        var archetype = reference.Archetypes.First(a => !a.IsCustom);
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Книжник", system, archetype.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    [Fact]
    public async Task Sheet_OffersLoreOnly_UntilTheTalentSaysOtherwise()
    {
        var (client, id, _) = await CreateCharacterAsync();

        var rating = (await SheetAsync(client, id)).KnowledgeRating;

        Assert.NotNull(rating);
        var only = Assert.Single(rating!.Options);
        Assert.Equal(KnowledgeRatingRules.LoreSkill, only.Skill);
        Assert.Equal("default", only.Reason);
        // Русское имя навыка приходит с сервера — клиент не собирает его сам.
        Assert.Equal("Знание (предания)", only.SkillRu);
    }

    [Fact]
    public async Task Sheet_ShowsTheRanksThePlayerActuallyBought()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var lore = reference.Skills.Single(s => s.Name == KnowledgeRatingRules.LoreSkill);

        var before = (await SheetAsync(client, id)).KnowledgeRating!.Options[0];
        Assert.Equal(0, before.Ranks); // ноль рангов — это ноль, а не «хотя бы один»

        var bought = await client.PostAsync(
            $"/api/characters/{id}/skills/{lore.Id}/buy-rank", null);
        Assert.Equal(HttpStatusCode.NoContent, bought.StatusCode);

        Assert.Equal(1, (await SheetAsync(client, id)).KnowledgeRating!.Options[0].Ranks);
    }

    [Fact]
    public async Task DarkInsight_AddsForbiddenAsASecondSource()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var talent = reference.Talents.Single(x => x.Name == KnowledgeRatingRules.DarkInsightTalent);

        var bought = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(talent.Id), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, bought.StatusCode);

        var options = (await SheetAsync(client, id)).KnowledgeRating!.Options;
        Assert.Equal(2, options.Count);
        // Умолчание правил остаётся первым: талант даёт выбор, а не подменяет источник.
        Assert.Equal(KnowledgeRatingRules.LoreSkill, options[0].Skill);
        Assert.Equal(KnowledgeRatingRules.ForbiddenSkill, options[1].Skill);
        Assert.Equal("darkInsight", options[1].Reason);
        Assert.Equal("Знание (запретное)", options[1].SkillRu);
    }

    [Fact]
    public async Task GenesysCore_HasASingleKnowledgeSkill()
    {
        var (client, id, _) = await CreateCharacterAsync(GameSystem.GenesysCore);

        var only = Assert.Single((await SheetAsync(client, id)).KnowledgeRating!.Options);
        Assert.Equal(KnowledgeRatingRules.CoreKnowledgeSkill, only.Skill);
    }

    // ── Справочник магии знает, какие эффекты считаются по Знанию ──

    [Fact]
    public async Task RatedEffects_CarryTheirQualities_AndSunderStaysBoolean()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>(
            "/api/spells/RealmsOfTerrinoth", Json.Options))!;

        SpellDto Effect(string parent, string code) => spells.Single(s =>
            s.Kind == SpellEntryKind.AdditionalEffect && s.ParentEffect == parent && s.NameEn == code);

        var fire = Effect("Attack", "Fire");
        Assert.True(fire.UsesKnowledgeRating);
        var burn = Assert.Single(fire.RatedQualities!);
        Assert.Equal("Burn", burn.Code);
        Assert.Equal("Жжение", burn.NameRu); // имя резолвится по справочнику качеств

        // У Разрушительного рейтинг получает только Проникающее: «Повреждение N» не существует.
        var destructive = Effect("Attack", "Destructive");
        Assert.Equal(["Pierce"], destructive.RatedQualities!.Select(q => q.Code));

        // Числовые эффекты используют рейтинг, но свойства не выдают.
        var poisonous = Effect("Attack", "Poisonous");
        Assert.True(poisonous.UsesKnowledgeRating);
        Assert.Empty(poisonous.RatedQualities!);

        // Дистанция от Знания не зависит вовсе.
        Assert.False(Effect("Attack", "Range").UsesKnowledgeRating);
    }

    [Fact]
    public async Task EveryRatedQuality_ActuallyHasARatingInTheCatalog()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>(
            "/api/spells/RealmsOfTerrinoth", Json.Options))!;
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;

        var rated = spells.SelectMany(s => s.RatedQualities ?? []).Select(q => q.Code).Distinct();
        foreach (var code in rated)
        {
            var quality = reference.Qualities.Single(q => q.NameEn == code);
            Assert.True(quality.HasRating, $"{code} не имеет рейтинга и не может получить его от Знания");
        }
    }
}
