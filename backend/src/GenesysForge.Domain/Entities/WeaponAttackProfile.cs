namespace GenesysForge.Domain.Entities;

/// <summary>
/// Типизированный профиль атаки оружия (ROT-WPN-01). Один экземпляр инвентаря может иметь
/// несколько профилей: кинжал бьют в ближнем бою и метают, топорик метают и берут в руку.
/// Числа хранятся числами, а не строкой «+3»: раньше каждый экран разбирал её сам.
/// </summary>
public class WeaponAttackProfile
{
    /// <summary>Код профиля по умолчанию — того, что напечатан в основной строке таблицы.</summary>
    public const string DefaultCode = "default";

    public Guid Id { get; set; }
    public Guid ItemDefId { get; set; }

    /// <summary>Стабильный код профиля в пределах предмета: <c>default</c>, <c>thrown</c>, <c>held</c>.</summary>
    public string Code { get; set; } = DefaultCode;

    /// <summary>Подпись профиля («в метании»); у профиля по умолчанию пусто.</summary>
    public string NameRu { get; set; } = "";
    public string NameEn { get; set; } = "";

    /// <summary>Английское имя боевого навыка броска.</summary>
    public string SkillName { get; set; } = "";

    public DamageKind DamageKind { get; set; }

    /// <summary>Прибавка к Мощи или итоговый урон — в зависимости от <see cref="DamageKind"/>.</summary>
    public int DamageValue { get; set; }

    public int Crit { get; set; }
    public WeaponRange Range { get; set; }

    /// <summary>Профиль по умолчанию; ровно один на предмет.</summary>
    public bool IsDefault { get; set; }

    /// <summary>
    /// Оружием нельзя атаковать вплотную (пика). Правило структурное: атака отклоняется, а не
    /// сопровождается припиской в описании.
    /// </summary>
    public bool CannotAttackEngaged { get; set; }

    /// <summary>
    /// Сложность проверки, заданная самим оружием (пика — Average, 2). <c>null</c> — сложность
    /// задаёт ситуация, как обычно.
    /// </summary>
    public int? FixedDifficulty { get; set; }

    /// <summary>
    /// Качества профиля кодами справочника. Пусто у профиля по умолчанию: он пользуется качествами
    /// самого предмета, иначе одни и те же строки жили бы в двух местах и расходились.
    /// Хранение кодами повторяет уже работающий путь именного оружия (ROT-HA-02): справочник
    /// резолвится при чтении, а в базе не дублируется.
    /// </summary>
    public List<WeaponProfileQuality> Qualities { get; set; } = [];
}

/// <summary>Качество профиля атаки: код справочника и рейтинг (0 — качество без рейтинга).</summary>
public class WeaponProfileQuality
{
    public string Code { get; set; } = "";
    public int Rating { get; set; }
}
