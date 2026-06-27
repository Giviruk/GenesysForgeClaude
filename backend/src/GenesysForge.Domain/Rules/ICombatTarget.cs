namespace GenesysForge.Domain.Rules;

/// <summary>
/// Изменяемая боевая цель применения эффектов (U-18): участник Game Table или (Stage 2) персонаж.
/// Поля совпадают с <see cref="Entities.GameParticipant"/>, чтобы применять эффекты единообразно.
/// </summary>
public interface ICombatTarget
{
    int WoundsCurrent { get; set; }
    int WoundsThreshold { get; set; }
    int StrainCurrent { get; set; }
    int? StrainThreshold { get; set; }
    int Soak { get; set; }
    int MeleeDefense { get; set; }
    int RangedDefense { get; set; }
}
