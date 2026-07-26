using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>Автоматизирует безопасную часть стандартных Secondary Effects, остальное отдаёт подсказкой.</summary>
public static class HeroicSecondaryEffectApplier
{
    public static void Apply(
        IEnumerable<HeroicSecondaryEffectDef> effects,
        ICombatTarget target,
        RuleEffectResult result)
    {
        foreach (var effect in effects)
        {
            if (effect.Code == "rot.heroic.secondary.rejuvenation")
            {
                var healed = Math.Min(2, target.StrainCurrent);
                target.StrainCurrent = Math.Max(0, target.StrainCurrent - 2);
                result.Applied.Add($"Снято усталости вторичным эффектом: {healed}");
                result.Manual.Add("В начале каждого следующего хода способности восстановите ещё 2 усталости.");
                continue;
            }

            var description = string.IsNullOrWhiteSpace(effect.Description)
                ? effect.SafeDescription
                : effect.Description;
            result.Manual.Add($"{effect.NameRu}: {description}");
        }
    }
}
