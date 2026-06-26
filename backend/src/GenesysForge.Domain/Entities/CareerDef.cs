namespace GenesysForge.Domain.Entities;

public class CareerDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента.</summary>
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
    public List<string> CareerSkillNames { get; set; } = [];

    /// <summary>Фиксированная часть стартовых денег (серебро).</summary>
    public int StartingMoneyFixed { get; set; }
    /// <summary>Бросок стартовых денег в формате <c>NdM</c> (например «1d100»); пусто — нет броска.</summary>
    public string StartingMoneyDice { get; set; } = "";
    /// <summary>Стартовое снаряжение карьеры. См. <see cref="CareerStartingGear"/>.</summary>
    public List<CareerStartingGear> StartingGear { get; set; } = [];
    /// <summary>Правила/заметки карьеры. См. <see cref="CareerRule"/>.</summary>
    public List<CareerRule> Rules { get; set; } = [];
}
