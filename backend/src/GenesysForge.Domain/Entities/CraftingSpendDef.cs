namespace GenesysForge.Domain.Entities;

/// <summary>
/// Одна строка таблицы трат символов: изготовления (ROT-CRAFT-01) или алхимии (ROT-ALCH-02).
/// Строка таблицы обычно предлагает несколько взаимоисключающих эффектов за одну цену — здесь
/// каждый эффект отдельная запись со своим кодом, а общая цена связывает их через <see cref="RowCode"/>.
/// </summary>
public class CraftingSpendDef : IContentDef
{
    public Guid Id { get; set; }
    /// <summary>Стабильный код эффекта, например <c>craft-enc-minus-1</c>.</summary>
    public string Code { get; set; } = "";
    /// <summary>Код строки таблицы: внутри строки эффекты взаимоисключающие.</summary>
    public string RowCode { get; set; } = "";
    /// <summary>Какой таблице принадлежит строка.</summary>
    public CraftingKind Table { get; set; }

    public string NameRu { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string Source { get; set; } = "";
    public bool Retired { get; set; }

    /// <summary>Сколько преимуществ стоит трата; 0 — преимуществами не оплачивается.</summary>
    public int AdvantageCost { get; set; }
    /// <summary>Сколько угроз стоит трата; 0 — угрозами не оплачивается.</summary>
    public int ThreatCost { get; set; }
    /// <summary>Сколько триумфов стоит трата; 0 — триумфами не оплачивается.</summary>
    public int TriumphCost { get; set; }
    /// <summary>Сколько отчаяний стоит трата; 0 — отчаяниями не оплачивается.</summary>
    public int DespairCost { get; set; }

    /// <summary>Трата ухудшает результат: оплачивается угрозами и отчаяниями.</summary>
    public bool IsNegative { get; set; }
    /// <summary>Трату можно выбрать больше одного раза — только там, где это прямо сказано.</summary>
    public bool Repeatable { get; set; }
    /// <summary>Перед созданием предмета выбор подтверждает ведущий (нарратив и чужое качество).</summary>
    public bool RequiresGmConfirmation { get; set; }
    /// <summary>Трате нужен параметр: код качества, код зелья или формулировка ведущего.</summary>
    public bool RequiresParameter { get; set; }

    public CraftingSpendEffect Effect { get; set; }
    /// <summary>Величина эффекта: дни, вес, слоты, рейтинг, число лишних экземпляров.</summary>
    public int Value { get; set; }
    /// <summary>Код качества для <see cref="CraftingSpendEffect.AddQuality"/>.</summary>
    public string Quality { get; set; } = "";
    /// <summary>Трата доступна только оружию (Неточное из таблицы).</summary>
    public bool WeaponOnly { get; set; }
    /// <summary>Порядок в таблице — чтобы строки показывались как в книге, а не по алфавиту.</summary>
    public int SortOrder { get; set; }
}
