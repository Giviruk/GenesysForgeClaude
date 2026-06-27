namespace GenesysForge.Domain;

/// <summary>
/// Вид автоматизируемого эффекта таланта/героической способности (U-18). Простые механические эффекты
/// применяются кнопкой; сложные нарративные/GM-решения помечаются <see cref="Manual"/> (только подсказка).
/// </summary>
public enum RuleEffectKind
{
    /// <summary>Нет авто-применения — показать как ручную подсказку мастеру/игроку.</summary>
    Manual = 0,
    /// <summary>Лечит раны (уменьшает текущие раны на Amount).</summary>
    HealWounds = 1,
    /// <summary>Лечит усталость (уменьшает текущую усталость на Amount).</summary>
    HealStrain = 2,
    /// <summary>Изменяет поглощение на Amount (на время эффекта).</summary>
    AdjustSoak = 3,
    /// <summary>Изменяет ближнюю защиту на Amount.</summary>
    AdjustMeleeDefense = 4,
    /// <summary>Изменяет дальнюю защиту на Amount.</summary>
    AdjustRangedDefense = 5,
    /// <summary>Изменяет порог ран на Amount.</summary>
    AdjustWoundThreshold = 6,
    /// <summary>Изменяет порог усталости на Amount.</summary>
    AdjustStrainThreshold = 7,
    /// <summary>Добавляет Amount кубов бонуса к следующей проверке.</summary>
    AddBoostNextCheck = 8,
    /// <summary>Добавляет Amount кубов помехи к следующей проверке.</summary>
    AddSetbackNextCheck = 9,
    /// <summary>Тратит очко сюжета (пул сессии — в v1 как подсказка).</summary>
    SpendStoryPoint = 10,
}
