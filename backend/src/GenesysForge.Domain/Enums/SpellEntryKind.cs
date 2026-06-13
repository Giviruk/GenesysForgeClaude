namespace GenesysForge.Domain;

/// <summary>Тип записи в справочнике магии.</summary>
public enum SpellEntryKind
{
    /// <summary>Базовый эффект заклинания (направление): Атака, Лечение, Барьер и т. д.</summary>
    Effect = 0,
    /// <summary>Дополнительный эффект-модификатор, повышающий сложность.</summary>
    AdditionalEffect = 1,
}
