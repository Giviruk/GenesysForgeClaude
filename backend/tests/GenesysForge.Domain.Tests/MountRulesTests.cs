using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// Правила скакунов (ROT-MOUNT-ITEM-01): вместимость профиля, перегруз и границы ран.
/// </summary>
public class MountRulesTests
{
    private static MountDef Profile(int brawn = 4, int capacity = 18, int woundThreshold = 7) =>
        new() { Name = "Beast of Burden", Brawn = brawn, Capacity = capacity, WoundThreshold = woundThreshold };

    [Fact]
    public void ProfileCapacityOverridesFivePlusBrawn()
    {
        var def = Profile(brawn: 4, capacity: 18);

        Assert.Equal(18, MountRules.Capacity(def));
        Assert.Equal(9, MountRules.GenericCapacity(def.Brawn));
    }

    [Fact]
    public void ProfileWithoutOwnCapacityFallsBackToGenericRule()
    {
        // Кастомная запись без числа книги считается общим правилом, а не нулём.
        var def = Profile(brawn: 3, capacity: 0);

        Assert.Equal(8, MountRules.Capacity(def));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(18, false)]
    [InlineData(19, true)]
    public void OverloadStartsStrictlyAboveCapacity(int carriedLoad, bool overloaded)
    {
        Assert.Equal(overloaded, MountRules.IsOverloaded(Profile(), carriedLoad));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(6, false)]
    [InlineData(7, true)]
    [InlineData(9, true)]
    public void MountIsIncapacitatedOnlyAtOrAboveWoundThreshold(int wounds, bool incapacitated)
    {
        Assert.Equal(incapacitated, MountRules.IsIncapacitated(Profile(woundThreshold: 7), wounds));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(3, 3)]
    [InlineData(40, 7)]
    public void WoundsAreClampedToProfileRange(int input, int expected)
    {
        Assert.Equal(expected, MountRules.ClampWounds(Profile(woundThreshold: 7), input));
    }
}
