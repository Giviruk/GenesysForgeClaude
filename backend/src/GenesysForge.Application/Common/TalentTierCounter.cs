using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

public static class TalentTierCounter
{
    /// <summary>Каждый ранг таланта считается отдельным талантом своего эффективного тира.</summary>
    public static Dictionary<int, int> Count(IEnumerable<CharacterTalent> talents)
    {
        var counts = new Dictionary<int, int>();
        foreach (var t in talents)
        {
            for (var rank = 0; rank < t.Ranks; rank++)
            {
                var tier = GenesysRules.RankedTalentEffectiveTier(t.TalentDef!.Tier, rank);
                counts[tier] = counts.GetValueOrDefault(tier) + 1;
            }
        }
        return counts;
    }
}
