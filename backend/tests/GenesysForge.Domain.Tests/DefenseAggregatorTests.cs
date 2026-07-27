using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CMB-03: провайдеры не складываются, надбавки складываются, предел защиты — 4.</summary>
public class DefenseAggregatorTests
{
    private static DefenseContribution Provides(int value, DefenseScope scope = DefenseScope.General, string name = "источник") =>
        new("Test", name, scope, DefenseMode.Provides, value);

    private static DefenseContribution Increases(int value, DefenseScope scope = DefenseScope.General, string name = "надбавка") =>
        new("Test", name, scope, DefenseMode.Increases, value);

    [Fact]
    public void ProvidersDoNotStack_TheBestOneWins()
    {
        var contributions = new[] { Provides(1, name: "Кожаная"), Provides(2, name: "Латы") };

        var melee = DefenseAggregator.Melee(contributions);

        Assert.Equal(2, melee.Effective);
        Assert.Equal("Латы", melee.Provider!.SourceName);
        Assert.Equal(["Кожаная"], melee.IgnoredProviders.Select(x => x.SourceName));
    }

    [Fact]
    public void ScopedProviders_ApplyOnlyToTheirChannel()
    {
        // Броня даёт General 1, укрытие — Ranged 2: ближняя 1, дальняя 2.
        var contributions = new[]
        {
            Provides(1, DefenseScope.General, "Броня"),
            Provides(2, DefenseScope.Ranged, "Укрытие"),
        };

        Assert.Equal(1, DefenseAggregator.Melee(contributions).Effective);
        Assert.Equal(2, DefenseAggregator.Ranged(contributions).Effective);
    }

    [Fact]
    public void IncreasesStackWithEachOtherAndWithTheBestProvider()
    {
        var contributions = new[] { Provides(2), Increases(1, name: "Defensive"), Increases(1, name: "Deflection") };

        Assert.Equal(4, DefenseAggregator.Melee(contributions).Effective);
    }

    [Fact]
    public void GeneralIncrease_AppliesToBothChannels()
    {
        var contributions = new[] { Increases(1, DefenseScope.General) };

        Assert.Equal(1, DefenseAggregator.Melee(contributions).Effective);
        Assert.Equal(1, DefenseAggregator.Ranged(contributions).Effective);
    }

    [Fact]
    public void RawAboveTheCap_IsReportedButEffectiveStaysAtFour()
    {
        var contributions = new[] { Provides(3), Increases(2) };

        var melee = DefenseAggregator.Melee(contributions);

        Assert.Equal(5, melee.Raw);
        Assert.Equal(4, melee.Effective);
        Assert.True(melee.Capped);
    }

    [Fact]
    public void RemovingTheBestProvider_UncoversTheNextOne()
    {
        var all = new[] { Provides(1, name: "Кожаная"), Provides(3, name: "Латы") };
        Assert.Equal(3, DefenseAggregator.Melee(all).Effective);

        // Латы потеряны — пересчёт обязан дать защиту следующего провайдера, а не ноль.
        var remaining = all.Where(c => c.SourceName != "Латы");
        Assert.Equal(1, DefenseAggregator.Melee(remaining).Effective);
    }

    [Fact]
    public void MeleeProviderDoesNotLeakIntoTheRangedChannel()
    {
        var contributions = new[] { Provides(3, DefenseScope.Melee, "Щит") };

        Assert.Equal(3, DefenseAggregator.Melee(contributions).Effective);
        Assert.Equal(0, DefenseAggregator.Ranged(contributions).Effective);
        Assert.Null(DefenseAggregator.Ranged(contributions).Provider);
    }

    [Fact]
    public void NoContributions_MeanZeroDefense()
    {
        var melee = DefenseAggregator.Melee([]);

        Assert.Equal(0, melee.Effective);
        Assert.Null(melee.Provider);
        Assert.Empty(melee.Increases);
    }

    [Fact]
    public void NegativeIncrease_CannotPushDefenseBelowZero()
    {
        var contributions = new[] { Provides(1), Increases(-3) };

        Assert.Equal(0, DefenseAggregator.Melee(contributions).Effective);
    }

    // ---- связка с расчётом листа ----

    [Fact]
    public void SheetCalculator_CapsDefenseAtFour_AndExplainsTheResult()
    {
        var ch = new CharacteristicsSet(2, 2, 2, 2, 2, 2);
        var armor = new ItemInput("Латы", ItemKind.Armor, ItemState.Equipped, 5,
            MeleeDefense: 3, RangedDefense: 3, IsActiveArmor: true);
        var defensive = new TalentInput("Defensive", 3, 2, MeleeDefenseBonusPerRank: 1, RangedDefenseBonusPerRank: 1);

        var d = SheetCalculator.ComputeDerived(ch, 10, 10, [defensive], [armor]);

        Assert.Equal(4, d.MeleeDefense);
        Assert.Equal(5, d.MeleeDefenseBreakdown!.Raw);
        Assert.True(d.MeleeDefenseBreakdown.Capped);
        Assert.Equal("Латы", d.MeleeDefenseBreakdown.Provider!.SourceName);
        Assert.Equal("Defensive", Assert.Single(d.MeleeDefenseBreakdown.Increases).SourceName);
    }

    [Fact]
    public void SheetCalculator_SpeciesNimbleIsAProvider_NotABonus()
    {
        var ch = new CharacteristicsSet(2, 3, 2, 2, 2, 2);
        var armor = new ItemInput("Кожаная", ItemKind.Armor, ItemState.Equipped, 2,
            MeleeDefense: 1, RangedDefense: 1, IsActiveArmor: true);

        var d = SheetCalculator.ComputeDerived(ch, 9, 10, [], [armor], baseDefense: 1);

        // Nimble 1 и броня 1 — два провайдера, а не 1 + 1.
        Assert.Equal(1, d.MeleeDefense);
        Assert.Single(d.MeleeDefenseBreakdown!.IgnoredProviders);
    }
}
