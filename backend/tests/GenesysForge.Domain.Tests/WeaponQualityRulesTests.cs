using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// GEN-EQP-QUAL-01: качества перестали быть надписью. Точное и Неточное меняют кубы, Громоздкое
/// и Сноровка — сложность при нехватке характеристики, Проникающее и Бронебойное — поглощение.
/// </summary>
public class WeaponQualityRulesTests
{
    private static WeaponQualityInput Q(QualityEffectKind kind, int rating = 0, string name = "Качество") =>
        new(name, name, kind, rating);

    [Fact]
    public void AccurateAddsBoost_AndInaccurateAddsSetback()
    {
        var pool = WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.AttackBoost, 1, "Точное"), Q(QualityEffectKind.AttackSetback, 2, "Неточное")],
            brawn: 3, agility: 3);

        Assert.Equal(1, pool.Boost);
        Assert.Equal(2, pool.Setback);
        Assert.Equal(["Точное", "Неточное"], pool.Sources.Select(s => s.NameRu));
    }

    [Fact]
    public void Cumbersome_RaisesDifficultyByTheMissingBrawn_AndOnlyByIt()
    {
        // Громоздкое 4 при Мощи 2 — две ступени; при Мощи 4 и выше правило не срабатывает вовсе.
        Assert.Equal(2, WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.DifficultyPerMissingBrawn, 4)], brawn: 2, agility: 3).DifficultyIncrease);
        Assert.Equal(0, WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.DifficultyPerMissingBrawn, 4)], brawn: 4, agility: 3).DifficultyIncrease);
        Assert.Equal(0, WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.DifficultyPerMissingBrawn, 4)], brawn: 5, agility: 3).DifficultyIncrease);
    }

    [Fact]
    public void Unwieldy_LooksAtAgility_NotBrawn()
    {
        var pool = WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.DifficultyPerMissingAgility, 3)], brawn: 5, agility: 1);

        Assert.Equal(2, pool.DifficultyIncrease);
    }

    [Fact]
    public void CumbersomeAndUnwieldy_StackTheirDifficulty()
    {
        var pool = WeaponQualityRules.PoolFor(
            [
                Q(QualityEffectKind.DifficultyPerMissingBrawn, 3, "Громоздкое"),
                Q(QualityEffectKind.DifficultyPerMissingAgility, 3, "Сноровка"),
            ],
            brawn: 2, agility: 2);

        Assert.Equal(2, pool.DifficultyIncrease);
    }

    [Fact]
    public void SuperiorGivesAdvantage_AndInferiorGivesThreat_NotDice()
    {
        var pool = WeaponQualityRules.PoolFor(
            [Q(QualityEffectKind.AutomaticAdvantage), Q(QualityEffectKind.AutomaticThreat)], 3, 3);

        Assert.Equal(1, pool.AutomaticAdvantage);
        Assert.Equal(1, pool.AutomaticThreat);
        Assert.Equal(0, pool.Boost);
        Assert.Equal(0, pool.Setback);
    }

    [Fact]
    public void DescriptiveQualities_DoNotTouchThePool()
    {
        // Взрыв, Жжение и прочие эффекты со счётчиками раундов пул не меняют: их исполнения нет.
        var pool = WeaponQualityRules.PoolFor([Q(QualityEffectKind.Descriptive, 3, "Жжение")], 1, 1);

        Assert.Equal(AttackPoolModifiers.None with { Sources = pool.Sources }, pool);
        Assert.Empty(pool.Sources);
    }

    [Fact]
    public void PierceIgnoresSoakByRating_AndBreachByTenTimesIt()
    {
        Assert.Equal(2, WeaponQualityRules.EffectiveSoak(4, [Q(QualityEffectKind.IgnoreSoak, 2)]));
        Assert.Equal(0, WeaponQualityRules.EffectiveSoak(6, [Q(QualityEffectKind.IgnoreSoakTenfold, 1)]));
    }

    [Fact]
    public void SoakNeverGoesBelowZero()
    {
        Assert.Equal(0, WeaponQualityRules.EffectiveSoak(2, [Q(QualityEffectKind.IgnoreSoak, 5)]));
    }

    [Fact]
    public void ReinforcedArmor_IgnoresPierceAndBreachEntirely()
    {
        var qualities = new[] { Q(QualityEffectKind.IgnoreSoak, 3), Q(QualityEffectKind.IgnoreSoakTenfold, 1) };

        Assert.Equal(5, WeaponQualityRules.EffectiveSoak(5, qualities, targetReinforced: true));
    }

    [Fact]
    public void ViciousAddsTenPerRank_ToTheCriticalRoll()
    {
        Assert.Equal(20, WeaponQualityRules.CriticalRollBonus([Q(QualityEffectKind.CriticalBonusTenfold, 2)]));
        Assert.Equal(0, WeaponQualityRules.CriticalRollBonus([Q(QualityEffectKind.AttackBoost, 2)]));
    }
}
