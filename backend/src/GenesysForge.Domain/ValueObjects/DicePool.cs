namespace GenesysForge.Domain;

/// <summary>Дайс-пул проверки навыка: зелёные (Ability) и жёлтые (Proficiency) кубы.</summary>
public readonly record struct DicePool(int Ability, int Proficiency)
{
    public override string ToString() => $"{Proficiency}P {Ability}A";
}
