namespace GenesysForge.Domain;

/// <summary>Тип сцены/столкновения Encounter Builder. Влияет на UI-подсказки, но не на механику.</summary>
public enum EncounterType
{
    Combat = 0,
    Social = 1,
    Exploration = 2,
    Chase = 3,
    Investigation = 4,
    Travel = 5,
    Hazard = 6,
    Mixed = 7,
    Custom = 8,
}
