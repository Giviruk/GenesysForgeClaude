namespace GenesysForge.Domain.Rules;

/// <summary>
/// Простая изменяемая реализация <see cref="ICombatTarget"/> для применения эффектов к персонажу
/// (U-18 Stage 2): пороги/поглощение берутся из вычисленного листа, current раны/усталость — из персонажа.
/// После применения сохраняются обратно только current-значения (пороги/soak у листа производные).
/// </summary>
public sealed class MutableCombatTarget : ICombatTarget
{
    public int WoundsCurrent { get; set; }
    public int WoundsThreshold { get; set; }
    public int StrainCurrent { get; set; }
    public int? StrainThreshold { get; set; }
    public int Soak { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
}
