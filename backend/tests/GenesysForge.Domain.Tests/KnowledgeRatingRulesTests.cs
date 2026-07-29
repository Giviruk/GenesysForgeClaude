using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-MAG-10: рейтинг эффектов заклинания берётся из рангов Знания. В RoT это именно Предания,
/// а «Тёмное прозрение» добавляет вторым источником Запретное — и выбор между ними делает игрок.
/// </summary>
public class KnowledgeRatingRulesTests
{
    private static Dictionary<string, int> Ranks(int lore = 0, int forbidden = 0, int geography = 0,
        int knowledge = 0) => new(StringComparer.Ordinal)
        {
            [KnowledgeRatingRules.LoreSkill] = lore,
            [KnowledgeRatingRules.ForbiddenSkill] = forbidden,
            ["Knowledge (Geography)"] = geography,
            [KnowledgeRatingRules.CoreKnowledgeSkill] = knowledge,
        };

    [Fact]
    public void Terrinoth_UsesLore_AndNothingElse()
    {
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, Ranks(lore: 2, forbidden: 3, geography: 5), hasDarkInsight: false);

        var only = Assert.Single(options);
        Assert.Equal(KnowledgeRatingRules.LoreSkill, only.Skill);
        Assert.Equal(2, only.Ranks);
        // Ни наибольший знаниевый навык, ни общий Knowledge источником не становятся.
        Assert.DoesNotContain(options, o => o.Skill is "Knowledge (Geography)" or "Knowledge");
    }

    [Fact]
    public void DarkInsight_AddsForbidden_AsASecondOption_NotAsAReplacement()
    {
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, Ranks(lore: 1, forbidden: 4), hasDarkInsight: true);

        Assert.Equal(2, options.Count);
        // Первым остаётся навык из правил системы: талант даёт выбор, а не подменяет умолчание.
        Assert.Equal(KnowledgeRatingRules.LoreSkill, options[0].Skill);
        Assert.Equal(KnowledgeRatingReason.Default, options[0].Reason);
        Assert.Equal(KnowledgeRatingRules.ForbiddenSkill, options[1].Skill);
        Assert.Equal(KnowledgeRatingReason.DarkInsight, options[1].Reason);
        Assert.Equal(4, options[1].Ranks);
    }

    [Fact]
    public void GenesysCore_HasOneKnowledgeSkill_AndNoDarkInsight()
    {
        // Запретного знания в Core нет, поэтому талант не открывает второй источник, даже
        // если персонаж каким-то образом им владеет.
        var options = KnowledgeRatingRules.Options(
            GameSystem.GenesysCore, Ranks(knowledge: 3, forbidden: 5), hasDarkInsight: true);

        var only = Assert.Single(options);
        Assert.Equal(KnowledgeRatingRules.CoreKnowledgeSkill, only.Skill);
        Assert.Equal(3, only.Ranks);
    }

    [Fact]
    public void ZeroRanks_MeanZero_NotAMinimumOfOne()
    {
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, Ranks(), hasDarkInsight: false);

        Assert.Equal(0, options[0].Ranks);
    }

    [Fact]
    public void UnknownSkill_CountsAsZero_RatherThanThrowing()
    {
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, new Dictionary<string, int>(), hasDarkInsight: false);

        Assert.Equal(0, Assert.Single(options).Ranks);
    }

    [Fact]
    public void Resolve_TakesThePlayersChoice()
    {
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, Ranks(lore: 1, forbidden: 4), hasDarkInsight: true);

        Assert.Equal(4, KnowledgeRatingRules.Resolve(options, KnowledgeRatingRules.ForbiddenSkill).Ranks);
        Assert.Equal(1, KnowledgeRatingRules.Resolve(options, KnowledgeRatingRules.LoreSkill).Ranks);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Knowledge (Geography)")]
    public void Resolve_FallsBackToTheRuleDefault_WhenTheChoiceIsNotOffered(string? chosen)
    {
        // Клиент, отставший на один талант, не должен ронять расчёт — он получает умолчание.
        var options = KnowledgeRatingRules.Options(
            GameSystem.RealmsOfTerrinoth, Ranks(lore: 2, forbidden: 4), hasDarkInsight: true);

        Assert.Equal(KnowledgeRatingRules.LoreSkill, KnowledgeRatingRules.Resolve(options, chosen).Skill);
    }

    [Fact]
    public void DarkInsight_IsRecognizedByItsStableEnglishName() =>
        Assert.True(KnowledgeRatingRules.HasDarkInsight(["Grit", "Dark Insight", "Toughened"]));

    [Fact]
    public void WithoutTheTalent_ThereIsNoChoice() =>
        Assert.False(KnowledgeRatingRules.HasDarkInsight(["Grit", "Toughened"]));
}
