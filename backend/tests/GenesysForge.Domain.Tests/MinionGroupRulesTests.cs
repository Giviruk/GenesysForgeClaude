using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class MinionGroupRulesTests
{
    [Theory]
    [InlineData(4, 3)]
    [InlineData(5, 2)]
    [InlineData(8, 2)]
    [InlineData(9, 1)]
    [InlineData(12, 1)]
    [InlineData(13, 0)]
    public void Calculate_UsesStrictPerMemberWoundBoundary(int wounds, int expectedRemaining)
    {
        var state = MinionGroupRules.Calculate(ParticipantType.MinionGroup, 3, wounds, 12, false);

        Assert.NotNull(state);
        Assert.Equal(4, state.PerMemberWoundThreshold);
        Assert.Equal(expectedRemaining, state.RemainingCount);
    }

    [Fact]
    public void Calculate_DoesNotGuessAmbiguousLegacySnapshot() =>
        Assert.Null(MinionGroupRules.Calculate(ParticipantType.MinionGroup, 3, 5, 10, false));

    [Fact]
    public void Calculate_ExplicitlyDefeatedGroupHasNoRemainingMembers() =>
        Assert.Equal(0, MinionGroupRules.Calculate(ParticipantType.MinionGroup, 3, 0, 12, true)!.RemainingCount);
}
