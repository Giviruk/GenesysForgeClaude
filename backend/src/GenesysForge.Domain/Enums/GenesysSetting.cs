namespace GenesysForge.Domain;

/// <summary>
/// Пригодность таланта к сеттингам Genesys (флаги — талант может подходить нескольким сеттингам).
/// <see cref="Any"/> = «во всех игровых мирах». Используется для фильтрации справочника по игровой системе:
/// Genesys Core показывает только <see cref="Any"/>; Realms of Terrinoth — <see cref="Any"/> + <see cref="Fantasy"/>.
/// </summary>
[Flags]
public enum GenesysSetting
{
    None = 0,
    /// <summary>Подходит для любого сеттинга («во всех игровых мирах»).</summary>
    Any = 1 << 0,
    /// <summary>Фэнтези.</summary>
    Fantasy = 1 << 1,
    /// <summary>Стимпанк.</summary>
    Steampunk = 1 << 2,
    /// <summary>Военная мистика / Weird War.</summary>
    WeirdWar = 1 << 3,
    /// <summary>Современность.</summary>
    ModernDay = 1 << 4,
    /// <summary>Научная фантастика.</summary>
    ScienceFiction = 1 << 5,
    /// <summary>Космоопера.</summary>
    SpaceOpera = 1 << 6,
}
