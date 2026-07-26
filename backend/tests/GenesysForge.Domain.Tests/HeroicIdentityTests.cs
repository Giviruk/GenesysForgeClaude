using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-HA-01: таблица происхождения и валидация личности героической способности.</summary>
public class HeroicIdentityTests
{
    /// <summary>Кость с заранее заданной последовательностью граней (10 = напечатанный «0»).</summary>
    private static Func<int, int> Dice(params int[] values)
    {
        var index = 0;
        return sides =>
        {
            Assert.Equal(HeroicOriginTable.Sides, sides);
            Assert.True(index < values.Length, "Таблица запросила больше бросков, чем задано в тесте.");
            return values[index++];
        };
    }

    // ---- таблица d10 ----

    [Theory]
    [InlineData(1, HeroicOriginType.Bloodline)]
    [InlineData(2, HeroicOriginType.Destiny)]
    [InlineData(3, HeroicOriginType.Artifact)]
    [InlineData(4, HeroicOriginType.Patron)]
    [InlineData(5, HeroicOriginType.Purpose)]
    [InlineData(6, HeroicOriginType.LifeChangingEvent)]
    [InlineData(7, HeroicOriginType.BlessingOrCurse)]
    [InlineData(8, HeroicOriginType.Training)]
    [InlineData(9, HeroicOriginType.WildMagic)]
    public void EveryOrdinaryFace_MapsToItsOwnCategory(int face, HeroicOriginType expected)
    {
        var roll = HeroicOriginTable.Roll(Dice(face));

        Assert.Equal(HeroicOriginMode.Standard, roll.Mode);
        Assert.Equal(expected, roll.Primary);
        Assert.Null(roll.Secondary);
        Assert.Equal([face], roll.Rolls);
    }

    [Fact]
    public void SpecialFace_ProducesTwoCategories_AndKeepsBothRolls()
    {
        // Последовательность из ТЗ: 0, 0, 4, 7 → происхождения 4 и 7.
        var roll = HeroicOriginTable.Roll(Dice(10, 10, 4, 7));

        Assert.Equal(HeroicOriginMode.DoubleStandard, roll.Mode);
        Assert.Equal(HeroicOriginType.Patron, roll.Primary);
        Assert.Equal(HeroicOriginType.BlessingOrCurse, roll.Secondary);
        // Специальный результат хранится как напечатанный «0» и финальным происхождением не становится.
        Assert.Equal([0, 0, 4, 7], roll.Rolls);
    }

    [Fact]
    public void SpecialFace_RepeatedOrdinaryResults_AreKept()
    {
        var roll = HeroicOriginTable.Roll(Dice(10, 3, 3));

        Assert.Equal(HeroicOriginType.Artifact, roll.Primary);
        Assert.Equal(HeroicOriginType.Artifact, roll.Secondary);
        Assert.Equal([0, 3, 3], roll.Rolls);
    }

    [Fact]
    public void OutOfRangeDie_IsRejected_InsteadOfSilentlyMapped()
    {
        Assert.Throws<InvalidOperationException>(() => HeroicOriginTable.Roll(_ => 11));
    }

    [Fact]
    public void BrokenSource_AlwaysSpecial_StopsInsteadOfLooping()
    {
        Assert.Throws<InvalidOperationException>(() => HeroicOriginTable.Roll(_ => 10));
    }

    // ---- валидация личности ----

    [Fact]
    public void Standard_RequiresName_AndSingleCategory()
    {
        var identity = HeroicIdentityRules.Validate(
            "  Клинок рассвета  ", HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null);

        Assert.Equal("Клинок рассвета", identity.CustomName);
        Assert.Equal(HeroicOriginType.Destiny, identity.OriginPrimary);
        Assert.Null(identity.OriginSecondary);
        Assert.Null(identity.OriginNarrative);
    }

    [Fact]
    public void EmptyName_IsRejected_WithMachineReason()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "   ", HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null));

        Assert.Equal("heroic.identity.name_required", ex.ReasonCode);
    }

    [Fact]
    public void TooLongName_IsRejected_InsteadOfTruncated()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            new string('я', HeroicIdentityRules.NameMaxLength + 1),
            HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null));

        Assert.Equal("heroic.identity.name_too_long", ex.ReasonCode);
    }

    [Fact]
    public void Standard_WithoutCategory_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.Standard, null, null, null));

        Assert.Equal("heroic.identity.origin_required", ex.ReasonCode);
    }

    [Fact]
    public void Standard_WithSecondCategory_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.Standard, HeroicOriginType.Destiny, HeroicOriginType.Patron, null));

        Assert.Equal("heroic.identity.origin_second_not_allowed", ex.ReasonCode);
    }

    [Fact]
    public void DoubleStandard_WithoutSecondCategory_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.DoubleStandard, HeroicOriginType.Destiny, null, null));

        Assert.Equal("heroic.identity.origin_second_required", ex.ReasonCode);
    }

    [Fact]
    public void Custom_RequiresNarrative()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.Custom, null, null, "   "));

        Assert.Equal("heroic.identity.narrative_required", ex.ReasonCode);
    }

    [Fact]
    public void Custom_WithTableCategory_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.Custom, HeroicOriginType.Destiny, null, "Свой текст"));

        Assert.Equal("heroic.identity.origin_not_allowed", ex.ReasonCode);
    }

    [Fact]
    public void UnknownCategory_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicIdentityRules.Validate(
            "Имя", HeroicOriginMode.Standard, (HeroicOriginType)42, null, null));

        Assert.Equal("heroic.identity.origin_unknown", ex.ReasonCode);
    }

    [Theory]
    [InlineData(null, HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null, false)]
    [InlineData("Имя", null, null, null, null, false)]
    [InlineData("Имя", HeroicOriginMode.Standard, null, null, null, false)]
    [InlineData("Имя", HeroicOriginMode.DoubleStandard, HeroicOriginType.Destiny, null, null, false)]
    [InlineData("Имя", HeroicOriginMode.Custom, null, null, null, false)]
    [InlineData("Имя", HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null, true)]
    [InlineData("Имя", HeroicOriginMode.DoubleStandard, HeroicOriginType.Destiny, HeroicOriginType.Patron, null, true)]
    [InlineData("Имя", HeroicOriginMode.Custom, null, null, "Кровь дракона", true)]
    public void Completeness_MatchesMode(
        string? name, HeroicOriginMode? mode, HeroicOriginType? primary,
        HeroicOriginType? secondary, string? narrative, bool expected)
    {
        Assert.Equal(expected, HeroicIdentityRules.IsComplete(name, mode, primary, secondary, narrative));
    }

    [Fact]
    public void Rolls_RoundTripThroughStorage()
    {
        var text = HeroicIdentityRules.FormatRolls([0, 0, 4, 7]);

        Assert.Equal("0,0,4,7", text);
        Assert.Equal([0, 0, 4, 7], HeroicIdentityRules.ParseRolls(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("0,x,4")]
    [InlineData("0,42")]
    public void BrokenRolls_ParseToEmpty_InsteadOfGuessing(string? stored)
    {
        Assert.Empty(HeroicIdentityRules.ParseRolls(stored));
    }
}
