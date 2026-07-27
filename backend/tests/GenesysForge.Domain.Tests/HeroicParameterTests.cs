using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-HA-02: параметры Paragon, Sixth Sense и профили Signature Weapon.</summary>
public class HeroicParameterTests
{
    [Theory]
    [InlineData("rot.heroic.paragon", HeroicParameterKind.ParagonSkill)]
    [InlineData("rot.heroic.sixth-sense", HeroicParameterKind.SixthSenseSubject)]
    [InlineData("rot.heroic.signature-weapon", HeroicParameterKind.SignatureWeapon)]
    [InlineData("rot.heroic.unleash", HeroicParameterKind.None)]
    [InlineData("", HeroicParameterKind.None)]
    [InlineData(null, HeroicParameterKind.None)]
    public void RequiredParameter_ComesFromStableCode(string? code, HeroicParameterKind expected)
    {
        Assert.Equal(expected, HeroicParameterRules.Required(code));
    }

    // ---- точные профили из таблицы ----

    [Theory]
    [InlineData(SignatureWeaponProfile.Brawl, "Brawl", "Brawn + 2", 4, "Engaged", 1, 2)]
    [InlineData(SignatureWeaponProfile.OneHanded, "Melee (Light)", "Brawn + 3", 3, "Engaged", 1, 2)]
    [InlineData(SignatureWeaponProfile.TwoHanded, "Melee (Heavy)", "Brawn + 5", 3, "Engaged", 3, 2)]
    [InlineData(SignatureWeaponProfile.Ranged, "Ranged", "8", 3, "Long", 2, 2)]
    public void Profile_MatchesBookTableExactly(
        SignatureWeaponProfile profile, string skill, string damage, int crit, string range, int enc, int hp)
    {
        var spec = SignatureWeaponProfiles.Get(profile);

        Assert.Equal(skill, spec.SkillName);
        Assert.Equal(damage, spec.Damage);
        Assert.Equal(crit, spec.Crit);
        Assert.Equal(range, spec.RangeBand);
        Assert.Equal(enc, spec.Encumbrance);
        Assert.Equal(hp, spec.HardPoints);
    }

    [Fact]
    public void Profiles_CarryTheirOwnQualities()
    {
        Assert.Equal([("disorient", 3), ("superior", 0)],
            SignatureWeaponProfiles.Get(SignatureWeaponProfile.Brawl).Qualities);
        Assert.Equal([("superior", 0)],
            SignatureWeaponProfiles.Get(SignatureWeaponProfile.OneHanded).Qualities);
        Assert.Equal([("knockdown", 0), ("superior", 0)],
            SignatureWeaponProfiles.Get(SignatureWeaponProfile.TwoHanded).Qualities);
        Assert.Equal([("superior", 0)],
            SignatureWeaponProfiles.Get(SignatureWeaponProfile.Ranged).Qualities);
    }

    // ---- признаки формы ----

    [Fact]
    public void ProfileGroupTrait_IsServerSide_AndForeignGroupsAreDropped()
    {
        // Клиент прислал «дальнобойную» группу для двуручного профиля — она отбрасывается.
        var traits = HeroicParameterRules.ValidateFormTraits(
            SignatureWeaponProfile.TwoHanded, WeaponFormTraits.Ranged | WeaponFormTraits.BluntOrCrushing);

        Assert.True(traits.HasFlag(WeaponFormTraits.TwoHanded));
        Assert.False(traits.HasFlag(WeaponFormTraits.Ranged));
        Assert.True(traits.HasFlag(WeaponFormTraits.BluntOrCrushing));
    }

    [Fact]
    public void Sword_ImpliesBladedAndCuttingEdge()
    {
        var traits = HeroicParameterRules.ValidateFormTraits(
            SignatureWeaponProfile.OneHanded, WeaponFormTraits.Sword);

        Assert.True(traits.HasFlag(WeaponFormTraits.Bladed));
        // Weighted Head требует отсутствия кромки — на мече он не пройдёт.
        Assert.True(traits.HasFlag(WeaponFormTraits.HasCuttingEdge));
    }

    [Theory]
    [InlineData(SignatureWeaponProfile.OneHanded, WeaponFormTraits.Bladed | WeaponFormTraits.BluntOrCrushing)]
    [InlineData(SignatureWeaponProfile.Ranged, WeaponFormTraits.Sword)]
    [InlineData(SignatureWeaponProfile.OneHanded, WeaponFormTraits.BowOrCrossbow)]
    [InlineData(SignatureWeaponProfile.TwoHanded, WeaponFormTraits.WoodenWorkingEdge)]
    public void ImpossibleTraitCombinations_AreRejected(SignatureWeaponProfile profile, WeaponFormTraits traits)
    {
        var ex = Assert.Throws<DomainRuleException>(
            () => HeroicParameterRules.ValidateFormTraits(profile, traits));

        Assert.Equal("heroic.weapon.traits_conflict", ex.ReasonCode);
    }

    [Fact]
    public void BowOnRangedProfile_IsAccepted()
    {
        var traits = HeroicParameterRules.ValidateFormTraits(
            SignatureWeaponProfile.Ranged, WeaponFormTraits.BowOrCrossbow);

        Assert.True(traits.HasFlag(WeaponFormTraits.Ranged));
        Assert.True(traits.HasFlag(WeaponFormTraits.BowOrCrossbow));
    }

    // ---- текстовые параметры ----

    [Fact]
    public void SixthSenseSubject_IsTrimmedAndRequired()
    {
        Assert.Equal("духи предков", HeroicParameterRules.ValidateSixthSenseSubject("  духи предков  "));

        var ex = Assert.Throws<DomainRuleException>(() => HeroicParameterRules.ValidateSixthSenseSubject("  "));
        Assert.Equal("heroic.parameter.subject_required", ex.ReasonCode);
    }

    [Fact]
    public void SixthSenseSubject_TooLong_IsRejected_InsteadOfTruncated()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicParameterRules.ValidateSixthSenseSubject(
            new string('я', HeroicParameterRules.SixthSenseSubjectMaxLength + 1)));

        Assert.Equal("heroic.parameter.subject_too_long", ex.ReasonCode);
    }

    [Fact]
    public void NarrativeForm_IsRequired()
    {
        var ex = Assert.Throws<DomainRuleException>(() => HeroicParameterRules.ValidateNarrativeForm(null));

        Assert.Equal("heroic.weapon.form_required", ex.ReasonCode);
    }
}
