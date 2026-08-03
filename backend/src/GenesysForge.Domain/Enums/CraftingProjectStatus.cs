namespace GenesysForge.Domain;

/// <summary>Стадия проекта изготовления. Разрешать дважды один проект нельзя.</summary>
public enum CraftingProjectStatus
{
    /// <summary>Заготовка: параметры выбраны, бросок ещё не сделан.</summary>
    Draft = 0,

    /// <summary>Бросок разрешён, траты распределены, предмет создан или не создан.</summary>
    Resolved = 1,

    /// <summary>Проект отменён до броска.</summary>
    Cancelled = 2,
}
