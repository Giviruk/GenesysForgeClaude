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
}
