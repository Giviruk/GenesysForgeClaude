using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-SPECIES-01: типизированные видовые правила, Nimble, silhouette и обязательный выбор.</summary>
public class SpeciesAbilityRulesTests
{
    private static ArchetypeAbilityDef Ability(
        string code, SpeciesAbilityRuleKind kind, int value = 0, string parameters = "") =>
        new() { Code = code, RuleKind = kind, RuleValue = value, RuleParameters = parameters };

    private static ArchetypeDef Species(int silhouette, params ArchetypeAbilityDef[] abilities) =>
        new() { Name = "Test", Silhouette = silhouette, Abilities = [.. abilities] };

    // ---- Nimble ----

    [Fact]
    public void Nimble_SetsBaseDefenseToOne()
    {
        var abilities = new[] { Ability("nimble", SpeciesAbilityRuleKind.SetBaseDefense, 1) };

        Assert.Equal(1, SpeciesAbilityRules.BaseDefense(abilities));
    }

    [Fact]
    public void WithoutNimble_ThereIsNoBaseDefense()
    {
        Assert.Null(SpeciesAbilityRules.BaseDefense([Ability("small", SpeciesAbilityRuleKind.SetSilhouette)]));
    }

    [Fact]
    public void Nimble_IsAProviderNotABonus_SoArmourDoesNotStackWithIt()
    {
        var ch = new CharacteristicsSet(2, 3, 2, 2, 1, 2);
        var armour = new ItemInput("Кожаный доспех", ItemKind.Armor, ItemState.Equipped,
            Encumbrance: 2, SoakBonus: 1, MeleeDefense: 1, RangedDefense: 1);

        var withArmour = SheetCalculator.ComputeDerived(ch, 9, 10, [], [armour], baseDefense: 1);
        var withoutArmour = SheetCalculator.ComputeDerived(ch, 9, 10, [], [], baseDefense: 1);

        Assert.Equal(1, withArmour.MeleeDefense);  // 1, а не 2
        Assert.Equal(1, withArmour.RangedDefense);
        Assert.Equal(1, withoutArmour.MeleeDefense);
    }

    [Fact]
    public void Nimble_DoesNotSuppressStrongerArmour()
    {
        var ch = new CharacteristicsSet(2, 3, 2, 2, 1, 2);
        var shield = new ItemInput("Большой щит", ItemKind.Armor, ItemState.Equipped,
            Encumbrance: 2, SoakBonus: 0, MeleeDefense: 2, RangedDefense: 0);

        var d = SheetCalculator.ComputeDerived(ch, 9, 10, [], [shield], baseDefense: 1);

        Assert.Equal(2, d.MeleeDefense);
        Assert.Equal(1, d.RangedDefense);
    }

    [Fact]
    public void Nimble_StillAllowsTalentsToAddOnTop()
    {
        var ch = new CharacteristicsSet(2, 3, 2, 2, 1, 2);
        var talent = new TalentInput("Dodge", 1, 1, 0, 0, 0, MeleeDefenseBonusPerRank: 1, 0);

        var d = SheetCalculator.ComputeDerived(ch, 9, 10, [talent], [], baseDefense: 1);

        Assert.Equal(2, d.MeleeDefense);
    }

    // ---- Silhouette ----

    [Fact]
    public void Small_OverridesSpeciesSilhouetteToZero()
    {
        var gnome = Species(1, Ability("small", SpeciesAbilityRuleKind.SetSilhouette, 0));

        Assert.Equal(0, SpeciesAbilityRules.Silhouette(gnome));
    }

    [Fact]
    public void WithoutSmall_SpeciesSilhouetteIsKept()
    {
        Assert.Equal(1, SpeciesAbilityRules.Silhouette(Species(1)));
    }

    // ---- Обязательный выбор ----

    private static ArchetypeDef HalfCatfolk() => Species(1,
        Ability("half.choice", SpeciesAbilityRuleKind.ChooseOneAbility,
            parameters: "options=cat.claws,cat.fleet"));

    private static Dictionary<string, ArchetypeAbilityDef> CatfolkOptions() => new(StringComparer.Ordinal)
    {
        ["cat.claws"] = Ability("cat.claws", SpeciesAbilityRuleKind.NaturalWeapon, 1),
        ["cat.fleet"] = Ability("cat.fleet", SpeciesAbilityRuleKind.FreeSecondMoveManeuver, 2),
    };

    [Fact]
    public void ChoiceOptions_AreParsedInDeclaredOrder()
    {
        var choice = HalfCatfolk().Abilities[0];

        Assert.Equal(["cat.claws", "cat.fleet"], SpeciesAbilityRules.ChoiceOptions(choice));
    }

    [Fact]
    public void UnmadeChoice_YieldsNoAbility_AndIsReportedIncomplete()
    {
        var species = HalfCatfolk();

        Assert.True(SpeciesAbilityRules.ChoiceIncomplete(species, ""));
        Assert.Empty(SpeciesAbilityRules.EffectiveAbilities(species, "", CatfolkOptions()));
    }

    [Fact]
    public void MadeChoice_YieldsExactlyThatOneAbility()
    {
        var species = HalfCatfolk();

        var effective = SpeciesAbilityRules.EffectiveAbilities(species, "cat.claws", CatfolkOptions()).ToList();

        Assert.False(SpeciesAbilityRules.ChoiceIncomplete(species, "cat.claws"));
        var only = Assert.Single(effective);
        Assert.Equal(SpeciesAbilityRuleKind.NaturalWeapon, only.RuleKind);
    }

    [Fact]
    public void ChoiceOutsideTheAllowedOptions_IsIgnored()
    {
        var species = HalfCatfolk();

        Assert.Empty(SpeciesAbilityRules.EffectiveAbilities(species, "cat.something-else", CatfolkOptions()));
    }

    [Fact]
    public void SpeciesWithoutChoice_IsNeverIncomplete_AndKeepsAllAbilities()
    {
        var catfolk = Species(1,
            Ability("cat.claws", SpeciesAbilityRuleKind.NaturalWeapon, 1),
            Ability("cat.fleet", SpeciesAbilityRuleKind.FreeSecondMoveManeuver, 2));

        Assert.False(SpeciesAbilityRules.ChoiceIncomplete(catfolk, ""));
        Assert.Equal(2, SpeciesAbilityRules.EffectiveAbilities(catfolk, "", new Dictionary<string, ArchetypeAbilityDef>()).Count());
    }

    // ---- Параметры правил ----

    [Theory]
    [InlineData("source=darkness", "source", "darkness")]
    [InlineData("enc=1;rarity=4", "rarity", "4")]
    [InlineData("enc=1;rarity=4", "enc", "1")]
    [InlineData("enc=1", "rarity", "")]
    [InlineData("", "source", "")]
    public void RuleParameter_ReadsNamedValues(string parameters, string key, string expected)
    {
        var ability = Ability("x", SpeciesAbilityRuleKind.Manual, parameters: parameters);

        Assert.Equal(expected, SpeciesAbilityRules.RuleParameter(ability, key));
    }

    [Fact]
    public void RuleParameter_DoesNotMatchPartialKeyNames()
    {
        var ability = Ability("x", SpeciesAbilityRuleKind.Manual, parameters: "rarity=4");

        Assert.Equal("", SpeciesAbilityRules.RuleParameter(ability, "rar"));
    }
}
