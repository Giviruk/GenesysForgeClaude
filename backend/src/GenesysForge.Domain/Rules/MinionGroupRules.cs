namespace GenesysForge.Domain.Rules;

public sealed record MinionGroupState(
    int InitialCount,
    int RemainingCount,
    int DefeatedCount,
    int PerMemberWoundThreshold);

/// <summary>Авторитетный расчёт потерь группы миньонов по общему tracker ран.</summary>
public static class MinionGroupRules
{
    public static MinionGroupState? Calculate(
        ParticipantType participantType,
        int count,
        int woundsCurrent,
        int groupWoundThreshold,
        bool isDefeated)
    {
        if (participantType != ParticipantType.MinionGroup || count < 1) return null;
        if (groupWoundThreshold < 1 || groupWoundThreshold % count != 0) return null;

        var perMember = groupWoundThreshold / count;
        var defeatedByWounds = Math.Min(
            count,
            Math.Max(0, woundsCurrent - 1) / perMember);
        var defeated = isDefeated ? count : defeatedByWounds;

        return new MinionGroupState(count, count - defeated, defeated, perMember);
    }
}
