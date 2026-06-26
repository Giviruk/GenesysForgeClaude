using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;

namespace GenesysForge.Api.Tests;

public class RuleReferenceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public RuleReferenceTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Rules_Endpoint_ReturnsAllKinds()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var rules = (await client.GetFromJsonAsync<RulesResponse>("/api/reference/rules", Json.Options))!;

        Assert.NotEmpty(rules.Entries);
        foreach (var kind in Enum.GetValues<RuleTableKind>())
            Assert.Contains(rules.Entries, e => e.Kind == kind);

        // d100 крит-таблица несёт диапазон броска.
        Assert.Contains(rules.Entries, e => e.Kind == RuleTableKind.CriticalInjury && e.RollRange.Length > 0);
    }

    [Fact]
    public async Task Rules_Endpoint_FiltersBySubstring()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var filtered = (await client.GetFromJsonAsync<RulesResponse>(
            "/api/reference/rules?q=усталость", Json.Options))!;

        Assert.NotEmpty(filtered.Entries);
        Assert.All(filtered.Entries, e =>
            Assert.Contains("усталост", (e.NameRu + e.Body + e.Notes).ToLowerInvariant()));
    }

    [Fact]
    public void RuleCatalog_HasNoDuplicateCodes_AndFitsLimits()
    {
        var all = RuleCatalog.Load().ToList();
        Assert.NotEmpty(all);
        Assert.Equal(all.Count, all.Select(r => r.Code).Distinct().Count());
        Assert.All(all, r =>
        {
            Assert.True(r.Body.Length <= 2000, $"Body too long for {r.Code}");
            Assert.True(r.SymbolCost.Length <= 200, $"SymbolCost too long for {r.Code}");
            Assert.False(string.IsNullOrWhiteSpace(r.SearchText), $"empty SearchText for {r.Code}");
        });
    }

    [Fact]
    public async Task Rules_RangeBand_SplitIntoSubgroups()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var rules = (await client.GetFromJsonAsync<RulesResponse>("/api/reference/rules", Json.Options))!;

        var range = rules.Entries.Where(e => e.Kind == RuleTableKind.RangeBand).ToList();
        Assert.NotEmpty(range);
        // Под-раздел кладётся в GroupRu; допустимы ровно две группы.
        Assert.Equal(["Общая информация", "Перемещение"], range.Select(e => e.GroupRu).Distinct().Order());
        // Переходы перемещения несут стоимость в манёврах; описания дистанций — нет.
        Assert.All(range.Where(e => e.GroupRu == "Перемещение"), e => Assert.Contains("манёвр", e.SymbolCost));
        Assert.All(range.Where(e => e.GroupRu == "Общая информация"), e => Assert.Equal("", e.SymbolCost));
    }

    [Fact]
    public async Task Search_FindsRule_AndContent()
    {
        var client = await _factory.CreateAuthorizedClientAsync();

        // «усталость» встречается и в таблицах правил (траты/криты).
        var byRule = (await client.GetFromJsonAsync<SearchResponse>(
            "/api/search?system=RealmsOfTerrinoth&q=усталость", Json.Options))!;
        Assert.Contains(byRule.Hits, h => h.Type == "rule");

        // Любой контент системы должен находиться по своему имени — проверяем непустой ответ
        // на распространённую подстроку и наличие маршрута.
        Assert.All(byRule.Hits, h => Assert.False(string.IsNullOrEmpty(h.Route)));
    }

    [Fact]
    public async Task Search_ShortQuery_ReturnsEmpty()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var res = (await client.GetFromJsonAsync<SearchResponse>(
            "/api/search?system=RealmsOfTerrinoth&q=a", Json.Options))!;
        Assert.Empty(res.Hits);
    }
}
