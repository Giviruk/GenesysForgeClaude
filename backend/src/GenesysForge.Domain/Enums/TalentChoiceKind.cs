namespace GenesysForge.Domain;

/// <summary>
/// Что именно выбирает игрок при покупке ранга таланта (ROT-TAL-03). Тип выбора определяет
/// валидацию значения; хранить выбор только отображаемым именем запрещено.
/// </summary>
public enum TalentChoiceKind
{
    /// <summary>Талант не требует выбора.</summary>
    None = 0,
    /// <summary>Характеристика (Dedication, Lucky Strike, Heroic Recovery, Heroic Will).</summary>
    Characteristic = 1,
    /// <summary>Навык (Knack for It, Natural, Master).</summary>
    Skill = 2,
    /// <summary>Магическое действие плюс мультимножество дополнительных эффектов (Signature Spell).</summary>
    SpellConfiguration = 3,
    /// <summary>Одобренный ведущим спутник с ограничением по silhouette (Animal Companion).</summary>
    AnimalCompanion = 4,
}
