using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using GenesysForge.Infrastructure.Persistence;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-HA-05 / ROT-HA-08 / ROT-HA-10 / ROT-HA-CONTENT: состав и содержательность каталога
/// героических способностей.
/// </summary>
public class RotHeroicCatalogTests
{
    private static readonly List<HeroicAbilityDef> Abilities = [.. HeroicCatalog.Load()];
    private static readonly List<HeroicSecondaryEffectDef> Effects = HeroicSecondaryEffectCatalog.Load();

    private static HeroicAbilityDef Ability(string code) =>
        Abilities.Single(a => a.Code == $"rot.heroic.{code}");

    private static string Upgrade(string code, HeroicUpgradeLevel level) =>
        Ability(code).Upgrades.Single(u => u.Level == level).Description;

    [Fact]
    public void CatalogHasAllElevenPrimaryEffects_EachWithTwoPowerUpgrades()
    {
        Assert.Equal(11, Abilities.Count);
        foreach (var ability in Abilities)
        {
            Assert.Equal(2, ability.Upgrades.Count);
            Assert.Equal(1, ability.Upgrades.Single(u => u.Level == HeroicUpgradeLevel.Improved).Cost);
            Assert.Equal(2, ability.Upgrades.Single(u => u.Level == HeroicUpgradeLevel.Supreme).Cost);
        }
    }

    [Fact]
    public void CatalogHasAllEightSecondaryEffects()
    {
        Assert.Equal(8, Effects.Count);
        Assert.Equal(
            ["devastating", "diminish", "drain", "empower-allies", "empowered",
             "rejuvenate-allies", "rejuvenation", "renewal"],
            Effects.Select(e => e.Code.Replace("rot.heroic.secondary.", "")).OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void EveryEntryHasFullAndSafeText_WithoutLeakingTheFullRuleIntoPublicSafe()
    {
        var issues = HeroicContentValidator.Validate(Abilities, Effects);

        Assert.Empty(issues.Select(i => $"{i.Code}: {i.Problem} — {i.Message}"));
    }

    // ---- отдельные правила, которые аудит назвал неверными ----

    [Fact]
    public void AllTheFacts_SupremeStoryPoint_DisappearsInsteadOfMovingToTheGm()
    {
        var supreme = Upgrade("all-the-facts", HeroicUpgradeLevel.Supreme);

        Assert.Contains("временное очко сюжета", supreme);
        Assert.Contains("исчезает", supreme);
        Assert.Contains("не переходит", supreme);
    }

    [Fact]
    public void Unbowed_SupremeDelaysDeathButDoesNotCancelIt()
    {
        var supreme = Upgrade("unbowed", HeroicUpgradeLevel.Supreme);

        Assert.Contains("умирает", supreme);
        Assert.Contains("не вылечена", supreme);
    }

    [Fact]
    public void Unbowed_BaseActivationIsAllowedOutOfTurn()
    {
        Assert.Contains("вне своего хода", Ability("unbowed").Description);
    }

    [Fact]
    public void Unleash_BaseCostsAManoeuvre_AndSparesEverythingButMinions()
    {
        var description = Ability("unleash").Description;

        Assert.Contains("манёвр", description);
        Assert.Contains("группу приспешников", description);
        Assert.Contains("Соперники", description);
    }

    [Fact]
    public void Unleash_SupremeKeepsTheBaseEffectAvailableLater()
    {
        Assert.Contains("остаётся доступен", Upgrade("unleash", HeroicUpgradeLevel.Supreme));
    }

    [Fact]
    public void MiraculousRecovery_ImprovedKeepsThePeriodicHeal()
    {
        var improved = Upgrade("miraculous-recovery", HeroicUpgradeLevel.Improved);

        Assert.Contains("все текущие раны", improved);
        Assert.Contains("3 раны", improved);
    }

    [Fact]
    public void MiraculousRecovery_SupremeNeedsNoMedicineCheck()
    {
        var supreme = Upgrade("miraculous-recovery", HeroicUpgradeLevel.Supreme);

        Assert.Contains("выбранную", supreme);
        Assert.Contains("без проверки", supreme);
    }

    [Fact]
    public void Connected_BaseLimitsTheFavour_AndRefundsAFailedActivation()
    {
        var description = Ability("connected").Description;

        Assert.Contains("не обязана", description);
        Assert.Contains("отменяется", description);
    }

    [Fact]
    public void Connected_SupremeIsAnOutOfTurnIncidental()
    {
        Assert.Contains("вне своего хода", Upgrade("connected", HeroicUpgradeLevel.Supreme));
    }

    [Fact]
    public void Foretelling_ImprovedForbidsRerollingTheSameCheckTwice()
    {
        Assert.Contains("повторно", Upgrade("foretelling", HeroicUpgradeLevel.Improved));
    }

    [Fact]
    public void Foretelling_SupremeDecidesAfterBothRolls()
    {
        var supreme = Upgrade("foretelling", HeroicUpgradeLevel.Supreme);

        Assert.Contains("после обоих бросков", supreme);
        Assert.Contains("за активацию", supreme);
    }

    [Fact]
    public void Paragon_SupremeIsHarmlessWhenTheDieIsAbsent()
    {
        Assert.Contains("ничего не делает", Upgrade("paragon", HeroicUpgradeLevel.Supreme));
    }

    [Fact]
    public void SignatureWeapon_PowerChoicesArePermanent()
    {
        Assert.Contains("не меняется", Upgrade("signature-weapon", HeroicUpgradeLevel.Improved));
        Assert.Contains("фиксируется", Upgrade("signature-weapon", HeroicUpgradeLevel.Supreme));
    }

    // ---- вторичные эффекты ----

    [Theory]
    [InlineData("devastating", "одному выбранному попаданию")]
    [InlineData("diminish", "короткой дистанции")]
    [InlineData("drain", "поглощение не применяется")]
    [InlineData("empowered", "кость подмоги")]
    [InlineData("empower-allies", "своим союзником для этого эффекта не считается")]
    [InlineData("rejuvenation", "не ниже нуля")]
    [InlineData("rejuvenate-allies", "лечением ран этот эффект не становится")]
    [InlineData("renewal", "уже отходившие участники")]
    public void SecondaryEffect_CarriesItsDecidingRule(string slug, string expected)
    {
        var effect = Effects.Single(e => e.Code == $"rot.heroic.secondary.{slug}");

        Assert.Contains(expected, effect.Description);
    }
}
