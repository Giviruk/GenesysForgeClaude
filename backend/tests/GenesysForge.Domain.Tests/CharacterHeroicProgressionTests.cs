using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Tests;

public class CharacterHeroicProgressionTests
{
    [Theory]
    [InlineData(100, 0)]
    [InlineData(149, 0)]
    [InlineData(150, 1)]
    [InlineData(199, 1)]
    [InlineData(200, 2)]
    public void AbilityPoints_ExcludeSpeciesStartingXp(int totalXp, int expected)
    {
        var character = CharacterWithStartingXp(100);
        character.TotalXp = totalXp;

        Assert.Equal(expected, character.HeroicUpgradePointsTotal);
    }

    [Fact]
    public void SpentPoints_IncludeAllUpgradeCategories()
    {
        var character = CharacterWithStartingXp(100);
        character.HeroicAbility = new HeroicAbilityDef
        {
            Name = "Test",
            Upgrades =
            [
                new HeroicAbilityUpgradeDef { Level = HeroicUpgradeLevel.Improved, Cost = 1 },
                new HeroicAbilityUpgradeDef { Level = HeroicUpgradeLevel.Supreme, Cost = 2 },
            ],
        };
        character.HeroicUpgradeRank = 2;
        character.HeroicDurationRanks = 2;
        character.HeroicFrequencyRanks = 1;
        character.HeroicStoryUpgrade = true;
        character.HeroicSecondaryEffects =
        [
            new CharacterHeroicSecondaryEffect(),
            new CharacterHeroicSecondaryEffect(),
        ];

        Assert.Equal(10, character.HeroicUpgradePointsSpent);
    }

    private static Character CharacterWithStartingXp(int startingXp) => new()
    {
        Name = "Hero",
        Archetype = new ArchetypeDef
        {
            Name = "Species",
            StartingXp = startingXp,
        },
    };
}
