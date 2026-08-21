namespace GenesysForge.Domain;

/// <summary>
/// Откуда позиция инвентаря появилась у персонажа. Нужна, чтобы duplicate, audit и legacy-ремонт
/// не считали выданное при создании обычной покупкой.
/// </summary>
public enum ItemProvenance
{
    /// <summary>Куплен за деньги персонажа или добавлен вручную владельцем.</summary>
    Purchased = 0,
    /// <summary>Выдан карьерным комплектом при создании (режим <c>CareerPackage</c>).</summary>
    CareerPackage = 1,
    /// <summary>Устаревшее значение старых файлов; новые записи его не используют.</summary>
    [Obsolete("Starting-budget provenance is no longer produced.")]
    StartingBudget = 2,
    /// <summary>Перенесён импортом файла персонажа.</summary>
    Imported = 3,
    /// <summary>Изготовлен самим персонажем (ROT-CRAFT-01): предмет, зелье или зачарование.</summary>
    Crafted = 4,
    /// <summary>
    /// Сделан грубо, Выживанием вместо Механики: ведущий может сломать такую вещь на отчаянии
    /// любой последующей проверки с ней.
    /// </summary>
    RoughSurvival = 5,
}
