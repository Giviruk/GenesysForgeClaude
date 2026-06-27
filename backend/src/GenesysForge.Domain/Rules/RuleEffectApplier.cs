using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Результат применения эффектов: что применилось автоматически и что требует ручного решения.</summary>
public sealed class RuleEffectResult
{
    /// <summary>Описания авто-применённых эффектов (для лога/UI).</summary>
    public List<string> Applied { get; } = [];
    /// <summary>Подсказки для ручного применения (нарратив/GM-решение, boost к проверке, очки сюжета).</summary>
    public List<string> Manual { get; } = [];

    public bool AnyApplied => Applied.Count > 0;
}

/// <summary>
/// Применяет структурные эффекты способности к боевой цели (U-18). Простые механические эффекты меняют
/// состояние цели; boost/setback/story/manual возвращаются как подсказки (нет pending-модели в Stage 1).
/// Чистая логика без зависимостей — тестируется напрямую.
/// </summary>
public static class RuleEffectApplier
{
    public static RuleEffectResult Apply(IEnumerable<RuleEffectDef> effects, ICombatTarget t)
    {
        var r = new RuleEffectResult();
        foreach (var e in effects)
        {
            var dur = string.IsNullOrWhiteSpace(e.Duration) ? "" : $" ({e.Duration})";
            switch (e.Kind)
            {
                case RuleEffectKind.HealWounds:
                    {
                        var healed = Math.Min(e.Amount, t.WoundsCurrent);
                        t.WoundsCurrent = Math.Max(0, t.WoundsCurrent - e.Amount);
                        r.Applied.Add($"Вылечено ран: {healed}");
                        break;
                    }
                case RuleEffectKind.HealStrain:
                    {
                        var healed = Math.Min(e.Amount, t.StrainCurrent);
                        t.StrainCurrent = Math.Max(0, t.StrainCurrent - e.Amount);
                        r.Applied.Add($"Снято усталости: {healed}");
                        break;
                    }
                case RuleEffectKind.AdjustSoak:
                    t.Soak = Math.Max(0, t.Soak + e.Amount);
                    r.Applied.Add($"Поглощение {Signed(e.Amount)}{dur} → {t.Soak}");
                    break;
                case RuleEffectKind.AdjustMeleeDefense:
                    t.MeleeDefense = Math.Max(0, t.MeleeDefense + e.Amount);
                    r.Applied.Add($"Ближняя защита {Signed(e.Amount)}{dur} → {t.MeleeDefense}");
                    break;
                case RuleEffectKind.AdjustRangedDefense:
                    t.RangedDefense = Math.Max(0, t.RangedDefense + e.Amount);
                    r.Applied.Add($"Дальняя защита {Signed(e.Amount)}{dur} → {t.RangedDefense}");
                    break;
                case RuleEffectKind.AdjustWoundThreshold:
                    t.WoundsThreshold = Math.Max(1, t.WoundsThreshold + e.Amount);
                    r.Applied.Add($"Порог ран {Signed(e.Amount)}{dur} → {t.WoundsThreshold}");
                    break;
                case RuleEffectKind.AdjustStrainThreshold:
                    if (t.StrainThreshold is { } st)
                    {
                        t.StrainThreshold = Math.Max(0, st + e.Amount);
                        r.Applied.Add($"Порог усталости {Signed(e.Amount)}{dur} → {t.StrainThreshold}");
                    }
                    break;
                case RuleEffectKind.AddBoostNextCheck:
                    r.Manual.Add($"Добавьте {e.Amount} куб(а) бонуса к следующей проверке.");
                    break;
                case RuleEffectKind.AddSetbackNextCheck:
                    r.Manual.Add($"Добавьте {e.Amount} куб(а) помехи к следующей проверке.");
                    break;
                case RuleEffectKind.SpendStoryPoint:
                    r.Manual.Add("Потратьте очко сюжета.");
                    break;
                default:
                    if (!string.IsNullOrWhiteSpace(e.Description)) r.Manual.Add(e.Description);
                    break;
            }
        }
        return r;
    }

    private static string Signed(int n) => n >= 0 ? $"+{n}" : n.ToString();
}
