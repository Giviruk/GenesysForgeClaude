using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CMB-01: обычный урон только при успешной атаке, поглощение — на каждый удар.</summary>
public class CombatResolverTests
{
    [Fact]
    public void Hit_AddsEachNetSuccessToBaseDamage_ThenSubtractsSoak()
    {
        // Пример приёмки ТЗ: base 7, successes 2, soak 4 → попадание, raw 9, применено 5.
        var result = CombatResolver.Resolve(new CombatAttackInput(NetSuccesses: 2, BaseDamage: 7, TargetSoak: 4));

        Assert.True(result.IsHit);
        Assert.Equal(9, result.RawDamagePerHit);
        Assert.Equal(5, result.TotalApplied);
        Assert.Equal(5, result.Hits.Single().Applied);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void NoNetSuccesses_IsAMiss_WithNoDamageFieldsAtAll(int netSuccesses)
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(netSuccesses, BaseDamage: 7, TargetSoak: 0));

        Assert.False(result.IsHit);
        // Именно null, а не базовый урон: иначе промах показывал бы урон оружия.
        Assert.Null(result.RawDamagePerHit);
        Assert.Empty(result.Hits);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public void LeftoverTriumph_DoesNotTurnAMissIntoAHit()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 0, BaseDamage: 6, TargetSoak: 0, Triumphs: 1));

        Assert.False(result.IsHit);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public void FullyAbsorbedHit_StillCountsAsAHit_ButBlocksTheOrdinaryCritical()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 1, BaseDamage: 3, TargetSoak: 9,
            RequestedSpends: [new CombatSymbolSpend("critical", RequiresDamageThroughSoak: true)]));

        Assert.True(result.IsHit);
        Assert.Equal(0, result.TotalApplied);
        Assert.Equal(["critical"], result.RejectedSymbolSpends);
        Assert.Empty(result.AllowedSymbolSpends);
    }

    [Fact]
    public void MultiHit_AppliesSoakToEachHitSeparately_NotToTheSum()
    {
        // Два удара по 8 против поглощения 5: 3 + 3 = 6, а не 16 − 5 = 11.
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 1, BaseDamage: 7, TargetSoak: 5,
            AdditionalHits: [new CombatHitInput(5, "Linked")]));

        Assert.Equal(8, result.RawDamagePerHit);
        Assert.Equal([3, 3], result.Hits.Select(h => h.Applied));
        Assert.Equal(6, result.TotalApplied);
    }

    [Fact]
    public void MultiHit_UsesEachTargetsOwnSoak()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 2, BaseDamage: 6, TargetSoak: 2,
            AdditionalHits: [new CombatHitInput(8, "Вторая цель")]));

        Assert.Equal([6, 0], result.Hits.Select(h => h.Applied));
        Assert.Equal(6, result.TotalApplied);
    }

    [Fact]
    public void ActiveQuality_IsRejectedOnAMiss_UnlessItsRuleAllowsIt()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 0, BaseDamage: 6, TargetSoak: 0, NetAdvantages: 2,
            RequestedSpends:
            [
                new CombatSymbolSpend("knockdown"),
                new CombatSymbolSpend("blast", MayActivateOnMiss: true),
            ]));

        Assert.False(result.IsHit);
        Assert.Equal(["blast"], result.AllowedSymbolSpends);
        Assert.Equal(["knockdown"], result.RejectedSymbolSpends);
    }

    [Fact]
    public void OnAHitThroughSoak_EveryRequestedSpendIsAllowed()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(
            NetSuccesses: 3, BaseDamage: 5, TargetSoak: 2, NetAdvantages: 2,
            RequestedSpends:
            [
                new CombatSymbolSpend("knockdown"),
                new CombatSymbolSpend("critical", RequiresDamageThroughSoak: true),
            ]));

        Assert.Equal(["knockdown", "critical"], result.AllowedSymbolSpends);
        Assert.Empty(result.RejectedSymbolSpends);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void NegativeProfileValues_AreRejected(int baseDamage, int soak)
    {
        Assert.Throws<DomainRuleException>(() =>
            CombatResolver.Resolve(new CombatAttackInput(1, baseDamage, soak)));
    }

    [Fact]
    public void MissLog_SaysPlainlyThatNoOrdinaryDamageApplies()
    {
        var result = CombatResolver.Resolve(new CombatAttackInput(0, 6, 0));

        Assert.Contains(result.Log, line => line.Contains("Промах"));
    }
}
