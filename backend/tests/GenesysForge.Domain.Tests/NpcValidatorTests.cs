using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class NpcValidatorTests
{
    private static Npc Npc(NpcKind kind = NpcKind.Rival) => new()
    {
        Name = "Тест", Kind = kind, Role = NpcRole.Brute,
        Brawn = 3, Agility = 2, Intellect = 2, Cunning = 2, Willpower = 2, Presence = 2,
        WoundThreshold = 12, StrainThreshold = kind == NpcKind.Nemesis ? 10 : null, Soak = 3,
    };

    [Fact]
    public void Minion_WithStrainOrRanks_IsError()
    {
        var minion = Npc(NpcKind.Minion);
        minion.StrainThreshold = 8;
        minion.Skills.Add(new NpcSkill { Name = "Ближний бой", Ranks = 2 });

        var r = NpcValidator.Validate(minion);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("усталост", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(r.Errors, e => e.Contains("групповые", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Nemesis_WithoutStrain_IsError()
    {
        var n = Npc(NpcKind.Nemesis);
        n.StrainThreshold = null;
        Assert.Contains(NpcValidator.Validate(n).Errors, e => e.Contains("усталост", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Rival_WithStrain_IsWarningNotError()
    {
        var r = Npc();
        r.StrainThreshold = 10;
        var res = NpcValidator.Validate(r);
        Assert.True(res.IsValid); // warning не блокирует
        Assert.Contains(res.Warnings, w => w.Contains("Соперник", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Defense_Above4Warns_Above6Errors()
    {
        var warn = Npc(); warn.MeleeDefense = 5;
        var w = NpcValidator.Validate(warn);
        Assert.True(w.IsValid);
        Assert.Contains(w.Warnings, x => x.Contains("Защита"));

        var err = Npc(); err.RangedDefense = 7;
        var e = NpcValidator.Validate(err);
        Assert.False(e.IsValid);
        Assert.Contains(e.Errors, x => x.Contains("Защита"));
    }

    [Fact]
    public void Soak_TooHigh_Warns()
    {
        var n = Npc(); n.Soak = 8;
        Assert.Contains(NpcValidator.Validate(n).Warnings, w => w.Contains("Поглощение"));
    }

    [Fact]
    public void Attack_MissingFields_IsError()
    {
        var n = Npc();
        n.Attacks.Add(new NpcAttack { Name = "Меч", SkillName = "", Damage = "", RangeBand = "", Critical = "x" });
        var r = NpcValidator.Validate(n);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("навык"));
        Assert.Contains(r.Errors, e => e.Contains("урон"));
        Assert.Contains(r.Errors, e => e.Contains("дистанц"));
        Assert.Contains(r.Errors, e => e.Contains("крит"));
    }

    [Fact]
    public void Attack_CustomQuality_Warns()
    {
        var n = Npc();
        n.Attacks.Add(new NpcAttack
        {
            Name = "Меч", SkillName = "Melee (Heavy)", Damage = "+3", RangeBand = "Вплотную",
            Qualities = [new NpcAttackQuality { QualityCode = "homebrew", NameRu = "Самопал", QualityDefId = null }],
        });
        var r = NpcValidator.Validate(n);
        Assert.True(r.IsValid);
        Assert.Contains(r.Warnings, w => w.Contains("не из справочника"));
    }

    [Fact]
    public void LargeSilhouette_LowWounds_Warns()
    {
        var n = Npc(NpcKind.Nemesis);
        n.Silhouette = 3; n.WoundThreshold = 12; // < 30
        Assert.Contains(NpcValidator.Validate(n).Warnings, w => w.Contains("силуэт", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MagicNpc_WithoutSpells_Warns()
    {
        var n = Npc();
        n.Skills.Add(new NpcSkill { Name = "Руны", Ranks = 2 });
        Assert.Contains(NpcValidator.Validate(n).Warnings, w => w.Contains("агическ", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidNpc_HasNoErrors()
    {
        var n = Npc();
        n.Skills.Add(new NpcSkill { Name = "Ближний бой", Ranks = 2 });
        Assert.True(NpcValidator.Validate(n).IsValid);
    }
}
