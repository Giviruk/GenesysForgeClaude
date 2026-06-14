namespace GenesysForge.Domain;

/// <summary>
/// Режим наполнения справочного контента. Определяет, какой seed-pipeline применяется и
/// какой объём описаний доступен. Полностью отделён от способа аутентификации/доступа (<c>AppMode</c>, см. п3).
/// </summary>
public enum ContentMode
{
    /// <summary>Приватный полный контент: расширенные (full) описания из private-набора.</summary>
    PrivateFull = 0,

    /// <summary>Публичный copyright-safe контент: без full-описаний, только safe-описания и ссылки на источник.</summary>
    PublicSafe = 1,
}
