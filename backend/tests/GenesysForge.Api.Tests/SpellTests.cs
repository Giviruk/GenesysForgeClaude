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

        Assert.Contains(spells, s => s.MagicSkill == "Runes");
        Assert.Contains(spells, s => s.MagicSkill == "Verse");
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
