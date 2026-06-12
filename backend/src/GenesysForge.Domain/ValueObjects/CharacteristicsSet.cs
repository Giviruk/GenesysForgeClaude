namespace GenesysForge.Domain;

/// <summary>Значения шести характеристик.</summary>
public record CharacteristicsSet(int Brawn, int Agility, int Intellect, int Cunning, int Willpower, int Presence)
{
    public int Get(CharacteristicType type) => type switch
    {
        CharacteristicType.Brawn => Brawn,
        CharacteristicType.Agility => Agility,
        CharacteristicType.Intellect => Intellect,
        CharacteristicType.Cunning => Cunning,
        CharacteristicType.Willpower => Willpower,
        CharacteristicType.Presence => Presence,
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };
}
