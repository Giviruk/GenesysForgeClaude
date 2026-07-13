namespace GenesysForge.Domain.Entities;

/// <summary>Определение навыка (встроенное или кастомное — у кастомного задан OwnerUserId).</summary>
public class SkillDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента (не меняется при правке отображаемых имён). У кастома пусто.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название (исторически — поле Name).</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    public CharacteristicType Characteristic { get; set; }
    public SkillKind Kind { get; set; }
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница (без копирования текста). Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    public Guid? OwnerUserId { get; set; }
    /// <summary>Набор homebrew, из которого импортирован пользовательский контент. Null — одиночный custom content.</summary>
    public Guid? HomebrewPackId { get; set; }
}
