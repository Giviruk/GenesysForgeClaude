namespace GenesysForge.Domain;

/// <summary>
/// Откуда взяты сохранённые пороги ран/стрейна (ROT-CRE-02). Отличает точный snapshot момента
/// завершения создания от восстановленного или оценённого значения legacy-персонажа.
/// </summary>
public enum ThresholdSnapshotProvenance
{
    /// <summary>Snapshot ещё не сделан — персонаж в фазе создания, порог считается динамически.</summary>
    None = 0,
    /// <summary>Значение зафиксировано в момент <c>CompleteCreation</c>.</summary>
    CreationCompleted = 1,
    /// <summary>Значение однозначно восстановлено из audit/event log завершённого персонажа.</summary>
    LegacyAuditReconstructed = 2,
    /// <summary>Значение выведено из видимого итога legacy-персонажа минус явные бонусы порога.</summary>
    LegacyDerivedFromVisibleTotal = 3,
    /// <summary>Восстановить не удалось — использована оценка, требуется ручная проверка правил.</summary>
    LegacyEstimated = 4,
}
