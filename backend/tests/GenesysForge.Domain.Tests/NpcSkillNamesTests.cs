using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class NpcSkillNamesTests
{
    [Theory]
    [InlineData("Божественное", "Divine", "Вера")]
    [InlineData("Вера", "Divine", "Вера")]
    [InlineData("Рукопашный бой", "Brawl", "Грубая сила")]
    [InlineData("Внимательность", "Perception", "Внимание")]
    public void Resolve_LegacyOrCurrentLabel_ReturnsStableBuiltIn(string saved, string name, string ru)
    {
        var skill = new SkillDef { Name = name, NameRu = ru };
        Assert.Same(skill, NpcSkillNames.Resolve([skill], saved));
    }

    [Fact]
    public void Resolve_CustomExactNameWins_AndAliasesNeverRedirectToCustomSkills()
    {
        var builtin = new SkillDef { Name = "Brawl", NameRu = "Грубая сила" };
        var custom = new SkillDef { Name = "Рукопашный бой", NameRu = "Своя техника", OwnerUserId = Guid.NewGuid() };
        Assert.Same(custom, NpcSkillNames.Resolve([builtin, custom], "Рукопашный бой"));
        Assert.Null(NpcSkillNames.Resolve([custom], "Неизвестный"));
        builtin.OwnerUserId = Guid.NewGuid();
        Assert.Null(NpcSkillNames.Resolve([builtin], "Рукопашный бой"));
    }
}
