namespace GenesysForge.Domain;

/// <summary>
/// Откуда позиция инвентаря появилась у персонажа (ROT-CRE-03). Нужна, чтобы duplicate, audit и
/// legacy-ремонт не считали выданное при создании обычной покупкой.
/// </summary>
public enum ItemProvenance
{
    /// <summary>Куплен за деньги персонажа или добавлен вручную владельцем.</summary>
    Purchased = 0,
    /// <summary>Выдан карьерным комплектом при создании (режим <c>CareerPackage</c>).</summary>
    CareerPackage = 1,
    /// <summary>Куплен при создании за бюджет 500 silver (режим <c>StandardMoney</c>).</summary>
    StartingBudget = 2,
    /// <summary>Перенесён импортом файла персонажа.</summary>
    Imported = 3,
}
