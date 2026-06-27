namespace GenesysForge.Domain.Entities;

public class HeroicAbilityDef : IContentDef
{
    public Guid Id { get; set; }
    /// <summary>Стабильный код встроенного контента. У кастома пусто.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название.</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница. Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";

    /// <summary>Стоимость/требование выбора (например, выбор навыка) или «—».</summary>
    public string Requirement { get; set; } = "";
    /// <summary>Стоимость активации (обычно «2 очка сюжета»).</summary>
    public string ActivationCost { get; set; } = "";
    /// <summary>Тип активации (действие/манёвр/инцидент и т.п.).</summary>
    public string Activation { get; set; } = "";
    /// <summary>Длительность эффекта.</summary>
    public string Duration { get; set; } = "";
    /// <summary>Частота использования.</summary>
    public string Frequency { get; set; } = "";
    /// <summary>Особые условия/заметки базового эффекта.</summary>
    public string Notes { get; set; } = "";

    public Guid? OwnerUserId { get; set; }

    /// <summary>Доступные улучшения способности (Improved/Supreme). У кастома — пусто.</summary>
    public List<HeroicAbilityUpgradeDef> Upgrades { get; set; } = [];

    /// <summary>Структурные эффекты для автоматизации активации (U-18). Пусто → только ручная подсказка.</summary>
    public List<RuleEffectDef> Effects { get; set; } = [];
}
