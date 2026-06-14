namespace GenesysForge.Domain.Entities;

/// <summary>
/// Запись справочника магии Genesys: базовый эффект заклинания (направление) или
/// дополнительный эффект-модификатор. Полные тексты книг не хранятся — только
/// структура, числовые параметры и краткие парафраз-описания.
/// </summary>
public class SpellDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Магический навык: Arcana, Divine, Primal, Runes, Verse.</summary>
    public required string MagicSkill { get; set; }
    public SpellEntryKind Kind { get; set; }
    /// <summary>
    /// Для дополнительного эффекта (Kind=AdditionalEffect) — стабильный код (NameEn) базового
    /// эффекта, к которому он относится. У базовых эффектов пусто.
    /// </summary>
    public string ParentEffect { get; set; } = "";
    public required string NameRu { get; set; }
    public required string NameEn { get; set; }
    /// <summary>Отображаемая сложность: для эффектов — базовая, для модификаторов — «+N».</summary>
    public string Difficulty { get; set; } = "";
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode=PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел (без копирования текста). Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    public int SortOrder { get; set; }
    public Guid? OwnerUserId { get; set; }
}
