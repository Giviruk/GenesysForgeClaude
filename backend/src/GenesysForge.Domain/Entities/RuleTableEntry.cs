namespace GenesysForge.Domain.Entities;

/// <summary>
/// Строка справочной таблицы правил (сложности, траты символов, дистанции, крит-ранения).
/// Системо-независима — одно определение на правило для обеих систем. Источник — RU-парафразы
/// механики (не текст книг), собранные генератором <c>_books/gen-rules-catalog.mjs</c>.
/// </summary>
public class RuleTableEntry
{
    public Guid Id { get; set; }
    public RuleTableKind Kind { get; set; }

    /// <summary>Стабильный код (slug), уникален в пределах справочника.</summary>
    public string Code { get; set; } = "";
    /// <summary>Русское название строки.</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Английское название (как в Core Rulebook), может быть пустым.</summary>
    public string NameEn { get; set; } = "";

    /// <summary>Группа внутри таблицы: тяжесть (криты), ситуация (траты), иначе пусто.</summary>
    public string GroupRu { get; set; } = "";
    /// <summary>Порядок сортировки внутри Kind.</summary>
    public int SortOrder { get; set; }

    /// <summary>Диапазон броска d100 для крит-ранений («01-05»), иначе пусто.</summary>
    public string RollRange { get; set; } = "";
    /// <summary>Стоимость в символах/кубах: трата (1 Advantage), число кубов сложности и т.п.</summary>
    public string SymbolCost { get; set; } = "";
    /// <summary>Основной парафраз-текст эффекта/описания.</summary>
    public string Body { get; set; } = "";
    /// <summary>Доп. примечания (механика/GM), может быть пустым.</summary>
    public string Notes { get; set; } = "";

    public string Source { get; set; } = "";
    public string SourcePage { get; set; } = "";

    /// <summary>Денормализованная строка для поиска (lowercase: имена + группа + текст + стоимость).</summary>
    public string SearchText { get; set; } = "";
}
