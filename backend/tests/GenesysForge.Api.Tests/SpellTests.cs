using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class SpellTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public SpellTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthorized_WithoutToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/spells/GenesysCore");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSystem_Returns400()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var response = await client.GetAsync("/api/spells/Nonsense");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenesysCore_ReturnsEffectsAndModifiers_NoTerrinothOnlySkills()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        Assert.NotEmpty(spells);
        Assert.Contains(spells, s => s.Kind == SpellEntryKind.Effect);
        Assert.Contains(spells, s => s.Kind == SpellEntryKind.AdditionalEffect);
        // Genesys Core: только Arcana/Divine/Primal, без Runes/Verse
        Assert.DoesNotContain(spells, s => s.MagicSkill is "Runes" or "Verse");
        Assert.Contains(spells, s => s.MagicSkill == "Arcana");
    }

    [Fact]
    public async Task Terrinoth_AddsRunesAndVerse()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        Assert.Contains(spells, s => s.MagicSkill == "Runes" && s.Kind == SpellEntryKind.Effect);
        Assert.Contains(spells, s => s.MagicSkill == "Verse" && s.Kind == SpellEntryKind.Effect);
    }

    [Fact]
    public async Task BaseEffects_FollowSkillAvailabilityMatrix()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        bool Available(string skill, string effect) =>
            spells.Any(s => s.Kind == SpellEntryKind.Effect && s.MagicSkill == skill && s.NameEn == effect);

        // По матрице: Verse не имеет Attack; Arcana не имеет Augment и Heal; Primal не имеет Barrier.
        Assert.False(Available("Verse", "Attack"));
        Assert.False(Available("Arcana", "Augment"));
        Assert.False(Available("Arcana", "Heal"));
        Assert.False(Available("Primal", "Barrier"));
        Assert.False(Available("Runes", "Conjure"));
        // Доступные комбинации присутствуют
        Assert.True(Available("Arcana", "Attack"));
        Assert.True(Available("Verse", "Heal"));
        Assert.True(Available("Runes", "Augment"));
        // Utility и Curse доступны всем навыкам
        foreach (var skill in new[] { "Arcana", "Divine", "Primal", "Runes", "Verse" })
        {
            Assert.True(Available(skill, "Utility"), $"{skill} should have Utility");
            Assert.True(Available(skill, "Curse"), $"{skill} should have Curse");
        }
    }

    [Fact]
    public async Task AdditionalEffects_BelongToAnExistingBaseEffect()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        var baseEffectCodes = spells.Where(s => s.Kind == SpellEntryKind.Effect)
            .Select(s => s.NameEn).ToHashSet();
        var additional = spells.Where(s => s.Kind == SpellEntryKind.AdditionalEffect).ToList();

        Assert.NotEmpty(additional);
        Assert.All(additional, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.ParentEffect));      // привязан к базовому
            Assert.Contains(m.ParentEffect, baseEffectCodes);            // который существует
        });
        // у Attack есть свои доп. эффекты
        Assert.Contains(additional, m => m.ParentEffect == "Attack");
    }

    [Fact]
    public async Task Spells_HaveRussianNames_SafeDescription_AndSource()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        Assert.All(spells, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.NameRu));   // русское название
            Assert.False(string.IsNullOrWhiteSpace(s.SafeDescription)); // безопасное описание для public
            Assert.False(string.IsNullOrWhiteSpace(s.Source));   // ссылка на источник доступна
        });
    }
}
