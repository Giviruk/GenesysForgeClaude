namespace GenesysForge.Domain;

/// <summary>
/// Как источник влияет на пул проверки. Хранится типизированно, а не текстом в описании:
/// иначе штраф тяжёлой брони игрок видел бы только глазами и в бросок он бы не попадал.
/// </summary>
public enum CheckModifierKind
{
    /// <summary>Добавляет кости помех (чёрные).</summary>
    AddSetback = 0,

    /// <summary>Убирает уже добавленные кости помех (не может увести итог ниже нуля).</summary>
    RemoveSetback = 1,
}
