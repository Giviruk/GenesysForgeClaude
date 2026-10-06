using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class TalentChoiceSchemaTests
{
    private static readonly Func<string, SkillKind?> Skills = name => name switch
    {
        "Athletics" => SkillKind.General,
        "Lore" => SkillKind.Knowledge,
        "Charm" => SkillKind.Social,
        "Melee" => SkillKind.Combat,
        "Arcana" => SkillKind.Magic,
        _ => null,
    };

    private static TalentChoiceError? Validate(
        string code, int rankIndex, string[] values, params string[] alreadyChosen)
    {
        var schema = TalentChoiceSchemas.All[code];
        return TalentChoiceSchemas.Validate(
            schema, rankIndex, TalentChoiceSchemas.Normalize(schema, values), alreadyChosen, Skills);
    }

    [Theory]
    [InlineData("willpower", "Willpower")]
    [InlineData(" BRAWN ", "Brawn")]
    [InlineData("42", "42")]
    [InlineData("Luck", "Luck")]
    public void Normalize_Characteristic_UsesEnumName(string value, string expected)
    {
        var schema = TalentChoiceSchemas.All["geroicheskaya-volya"];
        Assert.Equal([expected], TalentChoiceSchemas.Normalize(schema, [value]));
    }

    [Fact]
    public void HeroicWill_RequiresTwoDistinctKnownCharacteristics()
    {
        Assert.Equal(TalentChoiceSchemas.ReasonMissing, Validate("geroicheskaya-volya", 0, [])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonCount, Validate("geroicheskaya-volya", 0, ["Brawn"])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonDuplicate,
            Validate("geroicheskaya-volya", 0, ["brawn", "Brawn"])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonUnknownValue,
            Validate("geroicheskaya-volya", 0, ["Brawn", "42"])?.ReasonCode);
        Assert.Null(Validate("geroicheskaya-volya", 0, ["Brawn", "willpower"]));
    }

    [Theory]
    [InlineData("schastlivoe-popadanie")]
    [InlineData("heroic-recovery")]
    public void SingleCharacteristicTalents_TakeExactlyOne(string code)
    {
        Assert.Equal(TalentChoiceSchemas.ReasonMissing, Validate(code, 0, [])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonCount, Validate(code, 0, ["Brawn", "Agility"])?.ReasonCode);
        Assert.Null(Validate(code, 0, ["Agility"]));
    }

    [Fact]
    public void KnackForIt_OneThenTwoNewNonCombatNonMagicSkills()
    {
        Assert.Null(Validate("kvalifikatsiya", 0, ["Athletics"]));
        Assert.Equal(TalentChoiceSchemas.ReasonForbiddenSkillKind,
            Validate("kvalifikatsiya", 0, ["Melee"])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonForbiddenSkillKind,
            Validate("kvalifikatsiya", 0, ["Arcana"])?.ReasonCode);

        Assert.Equal(TalentChoiceSchemas.ReasonCount,
            Validate("kvalifikatsiya", 1, ["Lore"], "Athletics")?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonDuplicate,
            Validate("kvalifikatsiya", 1, ["Lore", "Athletics"], "Athletics")?.ReasonCode);
        Assert.Null(Validate("kvalifikatsiya", 1, ["Lore", "Charm"], "Athletics"));
    }

    [Fact]
    public void NaturalAndMaster_AcceptAnySkillKind()
    {
        Assert.Null(Validate("odarennost", 0, ["Melee", "Arcana"]));
        Assert.Equal(TalentChoiceSchemas.ReasonDuplicate, Validate("odarennost", 0, ["Melee", "Melee"])?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonUnknownValue, Validate("odarennost", 0, ["Melee", "Nope"])?.ReasonCode);
        Assert.Null(Validate("master", 0, ["Melee"]));
    }

    [Fact]
    public void Validate_RejectsValuesLongerThanTheColumn()
    {
        var tooLong = "Attack|" + new string('x', TalentChoiceSchemas.MaxValueLength);
        Assert.Equal(TalentChoiceSchemas.ReasonTooLong, Validate("signature-spell", 0, [tooLong])?.ReasonCode);
    }

    [Theory]
    [InlineData("Attack")]
    [InlineData("Attack|")]
    [InlineData("|Fire")]
    public void SignatureSpell_RequiresActionAndAtLeastOneEffect(string value)
    {
        Assert.Null(SignatureSpellConfiguration.Parse(value));
        Assert.Equal(TalentChoiceSchemas.ReasonMissing, Validate("signature-spell", 0, [value])?.ReasonCode);
    }

    [Fact]
    public void SignatureSpell_FormatIgnoresOrderButKeepsMultiplicity()
    {
        var a = SignatureSpellConfiguration.Parse("Attack|Range|Fire|Range")!;
        var b = SignatureSpellConfiguration.Parse("Attack|Fire|Range|Range")!;
        Assert.Equal("Attack|Fire|Range|Range", a.Format());
        Assert.Equal(a.Format(), b.Format());
        Assert.NotEqual(a.Format(), SignatureSpellConfiguration.Parse("Attack|Fire|Range")!.Format());
        Assert.Equal(["Attack|Fire|Range|Range"],
            TalentChoiceSchemas.Normalize(TalentChoiceSchemas.All["signature-spell"], ["Attack|Range|Fire|Range"]));
    }

    private static readonly Dictionary<(string Action, string Code), SignatureSpellEffect> Catalog = new()
    {
        [("Attack", "Fire")] = new("Fire", false, []),
        [("Attack", "Range")] = new("Range", true, []),
        [("Attack", "Non-Lethal")] = new("Non-Lethal", false, ["Deadly"]),
        [("Attack", "Deadly")] = new("Deadly", false, ["Non-Lethal"]),
        [("Heal", "Range")] = new("Range", true, []),
    };

    private static TalentChoiceError? ValidateSpell(string value) => SignatureSpellRules.Validate(
        SignatureSpellConfiguration.Parse(value)!,
        action => action is "Attack" or "Heal",
        (action, code) => Catalog.GetValueOrDefault((action, code)));

    [Fact]
    public void SignatureSpell_ChecksActionEffectsRepeatsAndExclusions()
    {
        Assert.Null(ValidateSpell("Attack|Fire|Range|Range"));
        Assert.Equal(TalentChoiceSchemas.ReasonUnknownValue, ValidateSpell("Curse|Fire")?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonUnknownValue, ValidateSpell("Heal|Fire")?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonDuplicate, ValidateSpell("Attack|Fire|Fire")?.ReasonCode);
        Assert.Equal(TalentChoiceSchemas.ReasonSpellConflict, ValidateSpell("Attack|Deadly|Non-Lethal")?.ReasonCode);
    }
}
