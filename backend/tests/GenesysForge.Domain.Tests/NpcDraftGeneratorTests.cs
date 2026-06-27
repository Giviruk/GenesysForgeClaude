using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class NpcDraftGeneratorTests
{
    private static Npc Draft(NpcKind kind, NpcRole role = NpcRole.Brute,
        NpcPowerLevel level = NpcPowerLevel.Standard, NpcCombatStyle style = NpcCombatStyle.Melee) =>
        NpcDraftGenerator.Generate(Guid.NewGuid(),
            new NpcDraftRequest(GameSystem.RealmsOfTerrinoth, kind, role, level, null, style, null));

    [Fact]
    public void Rival_Wound_Is8PlusBrawn_AndNoStrain()
    {
        var n = Draft(NpcKind.Rival);
        Assert.Equal(8 + n.Brawn, n.WoundThreshold);
        Assert.Null(n.StrainThreshold);
    }

    [Fact]
    public void Nemesis_Wound12PlusBrawn_Strain10PlusWillpower()
    {
        var n = Draft(NpcKind.Nemesis);
        Assert.Equal(12 + n.Brawn, n.WoundThreshold);
        Assert.Equal(10 + n.Willpower, n.StrainThreshold);
    }

    [Fact]
    public void Minion_NoStrain_AndGroupSkillsHaveNoRanks()
    {
        var n = Draft(NpcKind.Minion);
        Assert.Null(n.StrainThreshold);
        Assert.NotEmpty(n.Skills);
        Assert.All(n.Skills, s => Assert.Equal(0, s.Ranks));
    }

    [Fact]
    public void LargeMonster_GetsSilhouette2_AndScaledWounds()
    {
        var n = Draft(NpcKind.Nemesis, NpcRole.Monster, NpcPowerLevel.Elite);
        Assert.True(n.Silhouette >= 2);
        Assert.True(n.WoundThreshold >= n.Silhouette * 10);
    }

    [Fact]
    public void Generated_Npc_PassesValidation()
    {
        foreach (var kind in new[] { NpcKind.Minion, NpcKind.Rival, NpcKind.Nemesis })
            foreach (var level in new[] { NpcPowerLevel.Weak, NpcPowerLevel.Standard, NpcPowerLevel.Strong, NpcPowerLevel.Elite })
            {
                var n = Draft(kind, NpcRole.Brute, level);
                var r = NpcValidator.Validate(n);
                Assert.True(r.IsValid, $"{kind}/{level}: {string.Join("; ", r.Errors)}");
            }
    }

    private static Npc DraftTemplate(CreatureTemplate template, NpcRole role = NpcRole.Monster,
        NpcPowerLevel level = NpcPowerLevel.Standard) =>
        NpcDraftGenerator.Generate(Guid.NewGuid(),
            new NpcDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Rival, role, level, null,
                NpcCombatStyle.Melee, null, template));

    [Theory]
    [InlineData(CreatureTemplate.Undead, "нежить")]
    [InlineData(CreatureTemplate.Beast, "зверь")]
    [InlineData(CreatureTemplate.Dragon, "дракон")]
    [InlineData(CreatureTemplate.Demon, "демон")]
    [InlineData(CreatureTemplate.Construct, "конструкт")]
    public void Template_AddsTag_NaturalAttack_AndPassesValidation(CreatureTemplate template, string tag)
    {
        var n = DraftTemplate(template);
        Assert.Contains(tag, n.Tags);
        Assert.NotEmpty(n.Attacks); // природная атака сгенерирована
        Assert.All(n.Attacks, a => Assert.False(string.IsNullOrWhiteSpace(a.Damage)));
        Assert.Empty(n.Equipment); // существо не носит оружие гуманоида
        Assert.True(NpcValidator.Validate(n).IsValid, string.Join("; ", NpcValidator.Validate(n).Errors));
    }

    [Theory]
    [InlineData(CreatureTemplate.Undead)]
    [InlineData(CreatureTemplate.Dragon)]
    [InlineData(CreatureTemplate.Demon)]
    public void Template_FrighteningCreatures_HaveTerror(CreatureTemplate template) =>
        Assert.Contains(DraftTemplate(template).Abilities, a => a.Name.Contains("Ужас"));

    [Fact]
    public void Dragon_IsLarge_WithScaledWounds()
    {
        var n = DraftTemplate(CreatureTemplate.Dragon, NpcRole.Monster, NpcPowerLevel.Elite);
        Assert.True(n.Silhouette >= 2);
        Assert.True(n.WoundThreshold >= n.Silhouette * 10);
        Assert.Contains(n.Attacks, a => a.Name.Contains("дыхание", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void MagicSkill_SetsPrimarySkill_AndSpellAbility()
    {
        var n = NpcDraftGenerator.Generate(Guid.NewGuid(),
            new NpcDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Nemesis, NpcRole.Caster,
                NpcPowerLevel.Standard, null, NpcCombatStyle.Magic, null,
                CreatureTemplate.None, MagicSkill: "Руны"));
        Assert.Contains(n.Skills, s => s.Name == "Руны");
        Assert.Contains(n.Abilities, a => a.Name == "Заклинания");
    }

    [Fact]
    public void Environment_AddsTag()
    {
        var n = NpcDraftGenerator.Generate(Guid.NewGuid(),
            new NpcDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Rival, NpcRole.Brute,
                NpcPowerLevel.Standard, null, NpcCombatStyle.Melee, null,
                CreatureTemplate.None, Environment: "подземелье"));
        Assert.Contains("подземелье", n.Tags);
    }
}
