using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using GenesysForge.Infrastructure.Persistence;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Профили скакунов RoT (ROT-MOUNT-ITEM-01). Каждое число таблицы книги закреплено тестом: раньше
/// эти четыре записи были обычным «снаряжением» с Enc 0, поэтому проверять было нечего.
/// </summary>
public class RotMountCatalogTests
{
    private static readonly List<MountDef> Rot =
        [.. MountCatalog.Load().Where(m => m.System == GameSystem.RealmsOfTerrinoth)];

    private static MountDef Def(string bareCode) =>
        Rot.Single(m => m.Code == $"rot.mount.{bareCode}");

    [Fact]
    public void CatalogHasExactlyFourRotProfilesAndNoneInCore()
    {
        Assert.Equal(4, Rot.Count);
        Assert.DoesNotContain(MountCatalog.Load(), m => m.System == GameSystem.GenesysCore);
        Assert.Equal(
            ["beast-of-burden", "flying-mount", "riding-beast", "war-mount"],
            Rot.Select(m => m.Code["rot.mount.".Length..]).OrderBy(c => c, StringComparer.Ordinal));
    }

    [Theory]
    // code, price, rarity, kind, B, A, I, C, W, P, soak, WT, mdef, rdef, capacity, silhouette
    [InlineData("beast-of-burden", 200, 1, NpcKind.Minion, 4, 2, 1, 1, 1, 1, 4, 7, 0, 0, 18, 2)]
    [InlineData("riding-beast", 400, 2, NpcKind.Minion, 4, 3, 1, 1, 1, 1, 4, 5, 0, 0, 12, 2)]
    [InlineData("war-mount", 1500, 6, NpcKind.Rival, 4, 3, 1, 2, 3, 1, 4, 14, 0, 0, 13, 2)]
    [InlineData("flying-mount", 2000, 8, NpcKind.Rival, 3, 4, 1, 2, 2, 2, 3, 12, 1, 2, 12, 2)]
    public void ProfileMatchesBookTable(
        string code, int price, int rarity, NpcKind kind,
        int brawn, int agility, int intellect, int cunning, int willpower, int presence,
        int soak, int woundThreshold, int meleeDefense, int rangedDefense, int capacity, int silhouette)
    {
        var def = Def(code);

        Assert.Equal(price, def.Price);
        Assert.Equal(rarity, def.Rarity);
        Assert.Equal(kind, def.Kind);
        Assert.Equal(brawn, def.Brawn);
        Assert.Equal(agility, def.Agility);
        Assert.Equal(intellect, def.Intellect);
        Assert.Equal(cunning, def.Cunning);
        Assert.Equal(willpower, def.Willpower);
        Assert.Equal(presence, def.Presence);
        Assert.Equal(soak, def.Soak);
        Assert.Equal(woundThreshold, def.WoundThreshold);
        Assert.Equal(meleeDefense, def.MeleeDefense);
        Assert.Equal(rangedDefense, def.RangedDefense);
        Assert.Equal(capacity, def.Capacity);
        Assert.Equal(silhouette, def.Silhouette);
        // Ни Minion, ни Rival не имеют порога усталости — это null, а не ноль.
        Assert.Null(def.StrainThreshold);
        Assert.Equal("Realms of Terrinoth, с. 106", def.Source);
    }

    [Fact]
    public void MinionMountsCarryGroupSkillsAndRivalsCarryRanks()
    {
        foreach (var code in new[] { "beast-of-burden", "riding-beast" })
        {
            var def = Def(code);
            Assert.Equal(["Athletics", "Resilience"], def.Skills.Select(s => s.Name).Order());
            Assert.All(def.Skills, s => Assert.True(s.IsGroupSkill));
            Assert.All(def.Skills, s => Assert.Equal(0, s.Ranks));
        }

        var war = Def("war-mount");
        Assert.All(war.Skills, s => Assert.False(s.IsGroupSkill));
        Assert.Equal(3, war.Skills.Single(s => s.Name == "Athletics").Ranks);
        Assert.Equal(1, war.Skills.Single(s => s.Name == "Brawl").Ranks);
        Assert.Equal(2, war.Skills.Single(s => s.Name == "Discipline").Ranks);
        Assert.Equal(3, war.Skills.Single(s => s.Name == "Resilience").Ranks);
        Assert.Equal(2, war.Skills.Single(s => s.Name == "Survival").Ranks);

        var flying = Def("flying-mount");
        Assert.Equal(3, flying.Skills.Single(s => s.Name == "Athletics").Ranks);
        Assert.Equal(3, flying.Skills.Single(s => s.Name == "Coordination").Ranks);
        Assert.Equal(2, flying.Skills.Single(s => s.Name == "Discipline").Ranks);
        Assert.Equal(2, flying.Skills.Single(s => s.Name == "Resilience").Ranks);
        Assert.Equal(2, flying.Skills.Single(s => s.Name == "Survival").Ranks);
    }

    [Theory]
    [InlineData("war-mount", 6)]
    [InlineData("flying-mount", 5)]
    public void CombatMountsHaveStructuralEngagedAttackWithKnockdown(string code, int damage)
    {
        var attack = Assert.Single(Def(code).Attacks);

        Assert.Equal("Brawl", attack.SkillName);
        Assert.Equal(damage, attack.Damage);
        Assert.Equal(4, attack.Critical);
        Assert.Equal(WeaponRange.Engaged, attack.Range);
        Assert.Equal(["knockdown"], attack.QualityCodes);
    }

    [Fact]
    public void PackAndRidingMountsHaveNoAttackButCarryTheirGear()
    {
        Assert.Empty(Def("beast-of-burden").Attacks);
        Assert.Empty(Def("riding-beast").Attacks);
        Assert.Equal(["harness"], Def("beast-of-burden").IncludedGear);
        Assert.Equal(["riding-tack"], Def("riding-beast").IncludedGear);
        // Проверка Верховой езды в бою/стрессе — правило ездового животного, а не вьючного.
        Assert.True(Def("riding-beast").RequiresRidingCheck);
        Assert.False(Def("beast-of-burden").RequiresRidingCheck);
    }

    [Fact]
    public void FlyingMountKeepsFlyerAbilityAndErrataDefenseWithoutDodge()
    {
        var flying = Def("flying-mount");

        Assert.Equal("Flyer", Assert.Single(flying.Abilities).Name);
        // Официальная errata убрала печатное «Уклонение 2»: у профиля остаётся защита 1/2, и
        // никакого таланта Dodge в записи нет.
        Assert.Equal(1, flying.MeleeDefense);
        Assert.Equal(2, flying.RangedDefense);
        Assert.DoesNotContain("Dodge", flying.Abilities.Select(a => a.Name));
        Assert.DoesNotContain("Dodge", flying.Skills.Select(s => s.Name));
    }

    [Fact]
    public void EveryProfileHasSafeAndEnglishDescription()
    {
        Assert.All(Rot, def =>
        {
            Assert.False(string.IsNullOrWhiteSpace(def.SafeDescription));
            Assert.False(string.IsNullOrWhiteSpace(def.DescriptionEn));
            Assert.False(def.Retired);
        });
    }

    [Fact]
    public void ProfileCapacityWinsOverGenericFivePlusBrawn()
    {
        var pack = Def("beast-of-burden");

        // Общее правило дало бы 5 + 4 = 9, но книга задаёт вьючному животному 18.
        Assert.Equal(9, MountRules.GenericCapacity(pack.Brawn));
        Assert.Equal(18, MountRules.Capacity(pack));
    }
}
