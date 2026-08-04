namespace GenesysForge.Domain;

/// <summary>
/// Что именно изготавливается. Процесс у всех трёх один (ROT-CRAFT-01 «Alchemy использует общий
/// project lifecycle»), различаются навык, единица времени и таблица трат символов.
/// </summary>
public enum CraftingKind
{
    /// <summary>Обычное изготовление предмета: Механика, дни, таблица ROT-CRAFT-01.</summary>
    Item = 0,

    /// <summary>Варка расходника: Алхимия, часы, таблица ROT-ALCH-02.</summary>
    Potion = 1,

    /// <summary>
    /// Зачарование готовой основы (ROT-CRAFT-MAGIC-01). Скидок по редкости не даёт: сложность,
    /// время, навык и требования назначаются явно, потому что рецепта у зачарования нет.
    /// </summary>
    Enchantment = 2,
}
