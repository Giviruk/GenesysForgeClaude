using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// GEN-EQP-DMG-01: состояние повреждения экземпляра и ремонт. Проверяется каждая строка таблицы
/// состояний, стоимость материалов со всеми округлениями и ступени Разрушающего.
/// </summary>
public class DamageStateRulesTests
{
    // ── Что состояние делает с проверками ──

    [Theory]
    [InlineData(ItemDamageState.Undamaged, 0, 0)]
    [InlineData(ItemDamageState.Minor, 1, 0)]
    [InlineData(ItemDamageState.Moderate, 0, 1)]
    [InlineData(ItemDamageState.Major, 0, 0)]
    [InlineData(ItemDamageState.Destroyed, 0, 0)]
    public void CheckPenalties_FollowTheTable(ItemDamageState state, int setback, int difficulty)
    {
        Assert.Equal(setback, DamageStateRules.SetbackDice(state));
        Assert.Equal(difficulty, DamageStateRules.DifficultyIncrease(state));
    }

    /// <summary>Незначительное и Умеренное не складываются: предмет в одном состоянии сразу.</summary>
    [Fact]
    public void MinorAndModerate_DoNotStack()
    {
        Assert.Equal(0, DamageStateRules.DifficultyIncrease(ItemDamageState.Minor));
        Assert.Equal(0, DamageStateRules.SetbackDice(ItemDamageState.Moderate));
    }

    [Theory]
    [InlineData(ItemDamageState.Undamaged, true)]
    [InlineData(ItemDamageState.Minor, true)]
    [InlineData(ItemDamageState.Moderate, true)]
    [InlineData(ItemDamageState.Major, false)]
    [InlineData(ItemDamageState.Destroyed, false)]
    public void Usability_StopsAtMajor(ItemDamageState state, bool usable) =>
        Assert.Equal(usable, DamageStateRules.IsUsable(state));

    // ── Пул атаки ──

    [Fact]
    public void MinorDamage_AddsOneSetbackToTheAttackPool()
    {
        var pool = DamageStateRules.ApplyTo(AttackPoolModifiers.None, ItemDamageState.Minor);
        Assert.Equal(1, pool.Setback);
        Assert.Equal(0, pool.DifficultyIncrease);
        // Источник назван: игрок должен видеть, откуда взялся куб.
        Assert.Contains(pool.Sources, s => s.NameEn == "Minor damage" && s.Setback == 1);
    }

    [Fact]
    public void ModerateDamage_RaisesDifficultyOnceAndKeepsQualityContributions()
    {
        var qualities = new AttackPoolModifiers(
            Boost: 2, Setback: 1, DifficultyIncrease: 1, AutomaticAdvantage: 0, AutomaticThreat: 0,
            Sources: [new QualityContribution("Accurate", "Точное", Boost: 2)]);

        var pool = DamageStateRules.ApplyTo(qualities, ItemDamageState.Moderate);

        Assert.Equal(2, pool.Boost);
        Assert.Equal(1, pool.Setback);
        Assert.Equal(2, pool.DifficultyIncrease);
        Assert.Equal(2, pool.Sources.Count);
    }

    [Theory]
    [InlineData(ItemDamageState.Undamaged)]
    [InlineData(ItemDamageState.Major)]
    [InlineData(ItemDamageState.Destroyed)]
    public void StatesThatDoNotChangeChecks_LeaveThePoolAlone(ItemDamageState state) =>
        Assert.Same(AttackPoolModifiers.None,
            DamageStateRules.ApplyTo(AttackPoolModifiers.None, state));

    // ── Ремонт: доступность, сложность, время ──

    [Theory]
    [InlineData(ItemDamageState.Undamaged, false, null)]
    [InlineData(ItemDamageState.Minor, true, 1)]
    [InlineData(ItemDamageState.Moderate, true, 2)]
    [InlineData(ItemDamageState.Major, true, 3)]
    [InlineData(ItemDamageState.Destroyed, false, null)]
    public void Repair_FollowsTheTable(ItemDamageState state, bool canRepair, int? difficulty)
    {
        Assert.Equal(canRepair, DamageStateRules.CanRepair(state));
        Assert.Equal(difficulty, DamageStateRules.RepairDifficulty(state));
    }

    [Theory]
    [InlineData(ItemDamageState.Minor, 1, 2)]
    [InlineData(ItemDamageState.Moderate, 2, 4)]
    [InlineData(ItemDamageState.Major, 3, 6)]
    public void RepairTime_IsOneToTwoHoursPerDifficultyStep(ItemDamageState state, int min, int max)
    {
        var (actualMin, actualMax) = DamageStateRules.RepairHours(state);
        Assert.Equal(min, actualMin);
        Assert.Equal(max, actualMax);
    }

    // ── Материалы ──

    [Theory]
    [InlineData(ItemDamageState.Minor, 25)]
    [InlineData(ItemDamageState.Moderate, 50)]
    [InlineData(ItemDamageState.Major, 100)]
    [InlineData(ItemDamageState.Undamaged, 0)]
    [InlineData(ItemDamageState.Destroyed, 0)]
    public void MaterialPercent_FollowsTheTable(ItemDamageState state, int percent) =>
        Assert.Equal(percent, DamageStateRules.MaterialPercent(state));

    [Theory]
    [InlineData(ItemDamageState.Minor, 100)]
    [InlineData(ItemDamageState.Moderate, 200)]
    [InlineData(ItemDamageState.Major, 400)]
    public void MaterialCost_IsTheShareOfTheInstancePrice(ItemDamageState state, int expected) =>
        Assert.Equal(expected, DamageStateRules.MaterialCost(400, state));

    /// <summary>Дробь округляется вверх до целой монеты — явное продуктовое решение.</summary>
    [Fact]
    public void MaterialCost_RoundsUpToAWholeCoin() =>
        Assert.Equal(26, DamageStateRules.MaterialCost(101, ItemDamageState.Minor));

    [Theory]
    [InlineData(0, 100)]
    [InlineData(1, 90)]
    [InlineData(3, 70)]
    [InlineData(10, 0)]
    [InlineData(25, 0)]
    public void SelfRepair_TakesTenPercentOffPerNetAdvantage(int advantages, int expected) =>
        Assert.Equal(expected, DamageStateRules.MaterialCost(400, ItemDamageState.Minor, advantages));

    /// <summary>Скидка считается от уже округлённой доли — той суммы, что показана в памятке.</summary>
    [Fact]
    public void SelfRepairDiscount_AppliesToTheRoundedShare()
    {
        Assert.Equal(26, DamageStateRules.MaterialCost(101, ItemDamageState.Minor));
        Assert.Equal(24, DamageStateRules.MaterialCost(101, ItemDamageState.Minor, 1));
    }

    [Fact]
    public void MaterialCost_OfAFreeItem_IsZero() =>
        Assert.Equal(0, DamageStateRules.MaterialCost(0, ItemDamageState.Major));

    [Fact]
    public void Estimate_CarriesBothDiscountedAndBaseCost()
    {
        var estimate = DamageStateRules.Estimate(400, ItemDamageState.Moderate, netAdvantages: 2);
        Assert.True(estimate.CanRepair);
        Assert.Equal(2, estimate.Difficulty);
        Assert.Equal(50, estimate.MaterialPercent);
        Assert.Equal(160, estimate.MaterialCost);
        Assert.Equal(200, estimate.BaseMaterialCost);
    }

    // ── Разрушающее: ступень вниз, но не дальше Уничтожено ──

    [Theory]
    [InlineData(ItemDamageState.Undamaged, 1, ItemDamageState.Minor)]
    [InlineData(ItemDamageState.Minor, 1, ItemDamageState.Moderate)]
    [InlineData(ItemDamageState.Moderate, 1, ItemDamageState.Major)]
    [InlineData(ItemDamageState.Major, 1, ItemDamageState.Destroyed)]
    [InlineData(ItemDamageState.Destroyed, 1, ItemDamageState.Destroyed)]
    [InlineData(ItemDamageState.Undamaged, 3, ItemDamageState.Major)]
    [InlineData(ItemDamageState.Undamaged, 9, ItemDamageState.Destroyed)]
    [InlineData(ItemDamageState.Minor, 0, ItemDamageState.Minor)]
    public void Worsen_StepsDownAndStopsAtDestroyed(
        ItemDamageState from, int steps, ItemDamageState expected) =>
        Assert.Equal(expected, DamageStateRules.Worsen(from, steps));

    [Fact]
    public void UnknownState_IsRejectedWithAMachineCode()
    {
        var error = Assert.Throws<DomainRuleException>(
            () => DamageStateRules.EnsureKnown((ItemDamageState)42));
        Assert.Equal("item.damage_state.unknown", error.ReasonCode);
    }
}
