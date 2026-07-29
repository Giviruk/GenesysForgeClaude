using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class RuneboundShardRulesTests
{
    private static readonly string[] Codes =
    [
        "arcane-bolt-rune", "blasting-rune", "ice-storm-rune", "immolation-rune",
        "lesser-rune", "lightning-strike-rune", "rune-of-collection", "rune-of-fate",
        "rune-of-misery", "soulstone-rune", "stasis-rune", "sunburst-rune",
        "teleportation-rune", "terror-rune", "vision-rune", "wanderers-stone",
        "ynfernael-rune",
    ];

    [Fact]
    public void Manifest_IsExactlyTheSeventeenPublishedShards()
    {
        Assert.Equal(Codes, RuneboundShardRules.All.Select(x => x.Code));
        Assert.All(Codes, code => Assert.True(RuneboundShardRules.IsShard($"rot.item.{code}")));
        Assert.False(RuneboundShardRules.IsShard("staff"));
    }

    [Theory]
    [InlineData(false, 1, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 1, true)]
    [InlineData(true, 3, true)]
    public void ImplementEligibility_RequiresCareerRunesAndOneRank(
        bool career, int ranks, bool expected) =>
        Assert.Equal(expected, RuneboundShardRules.CanUseAsImplement(career, ranks));

    [Theory]
    [InlineData("arcane-bolt-rune", 4)]
    [InlineData("blasting-rune", 5)]
    [InlineData("ice-storm-rune", 4)]
    [InlineData("lesser-rune", 3)]
    [InlineData("lightning-strike-rune", 5)]
    [InlineData("ynfernael-rune", 3)]
    public void AttackDamageBonuses_AreStructural(string code, int bonus) =>
        Assert.Equal(bonus, RuneboundShardRules.For(code)!.AttackDamageBonus);

    [Fact]
    public void CollectionAndMisery_ApplyOnlyTheirExactFlatReductions()
    {
        var collection = RuneboundShardRules.For("rune-of-collection")!;
        Assert.Equal(1, RuneboundShardRules.FlatDifficultyReduction(collection, "Attack"));
        Assert.Equal(1, collection.CastingStrainReduction);

        var misery = RuneboundShardRules.For("rune-of-misery")!;
        Assert.Equal(2, RuneboundShardRules.FlatDifficultyReduction(misery, "Curse"));
        Assert.Equal(0, RuneboundShardRules.FlatDifficultyReduction(misery, "Attack"));
    }

    [Fact]
    public void FateAndSunburst_DeclareTheirSkillMatrixOverrides()
    {
        var fate = RuneboundShardRules.EffectsFor(
            RuneboundShardRules.For("rune-of-fate")!, "Curse");
        Assert.Contains(fate, x => x.EffectCode == "Doom"
            && x.Mode == ShardSpellEffectMode.MandatoryFree
            && x.OverridesSkillRestriction);

        var sunburst = RuneboundShardRules.EffectsFor(
            RuneboundShardRules.For("sunburst-rune")!, "Attack");
        Assert.Contains(sunburst, x => x.EffectCode == "Holy/Unholy"
            && x.Mode == ShardSpellEffectMode.MandatoryFree
            && x.OverridesSkillRestriction);
    }

    [Fact]
    public void Teleportation_MakesExactlyThreeRangeAdditionsFree()
    {
        var effect = Assert.Single(RuneboundShardRules.For("teleportation-rune")!.SpellEffects);
        Assert.Equal("Range", effect.EffectCode);
        Assert.Equal(3, effect.FreeUses);
        Assert.Equal(ShardSpellEffectMode.OptionalFree, effect.Mode);
    }

    [Fact]
    public void LesserRune_IsTheOnlyConfigurableShard()
    {
        Assert.True(RuneboundShardRules.For("lesser-rune")!.NeedsConfiguration);
        Assert.All(RuneboundShardRules.All.Where(x => x.Code != "lesser-rune"),
            x => Assert.False(x.NeedsConfiguration));
    }
}
