namespace GenesysForge.Domain.Entities;

/// <summary>
/// Покупаемый профиль скакуна (ROT-MOUNT-ITEM-01). Раньше четыре скакуна лежали в каталоге
/// предметов обычным снаряжением с <c>Enc 0</c> и описанием «Снаряжение»: их можно было положить в
/// рюкзак, но у них не было ни характеристик, ни порога ран, ни вместимости. Скакун — существо со
/// статблоком, поэтому у него собственный тип контента, а покупка создаёт
/// <see cref="CharacterMount"/>, а не строку инвентаря.
/// </summary>
public class MountDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }

    /// <summary>Стабильный код встроенного контента, например <c>rot.mount.war-mount</c>.</summary>
    public string Code { get; set; } = "";
    public required string Name { get; set; }
    public string NameRu { get; set; } = "";

    /// <summary>
    /// Скакун или транспортное средство (ROT-TRANSPORT-01). У повозки характеристики, навыки и
    /// атаки пустые, а <see cref="StrainThreshold"/> читается как порог систем.
    /// </summary>
    public TransportKind TransportKind { get; set; } = TransportKind.Mount;

    /// <summary>Режим движения для карточки: по земле, по воздуху или на колёсах.</summary>
    public MovementMode MovementMode { get; set; } = MovementMode.Ground;

    /// <summary>
    /// Сам по себе не движется — нужно тягловое животное. Отсутствие тяги не удаляет транспорт и
    /// не переносит его груз владельцу: это состояние, а не запрет.
    /// </summary>
    public bool RequiresTraction { get; set; }

    /// <summary>Тип существа: у скакунов книги это Minion либо Rival.</summary>
    public NpcKind Kind { get; set; }

    // Характеристики профиля (B/A/I/C/W/P).
    public int Brawn { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Cunning { get; set; }
    public int Willpower { get; set; }
    public int Presence { get; set; }

    public int Soak { get; set; }
    public int WoundThreshold { get; set; }

    /// <summary>Порог усталости. У Minion его нет — <c>null</c>, а не ноль.</summary>
    public int? StrainThreshold { get; set; }

    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
    public int Silhouette { get; set; } = 2;

    /// <summary>
    /// Вместимость груза профиля. Приоритетнее общего <c>5 + Brawn</c>: книга задаёт скакунам свои
    /// числа, и они не выводятся из характеристик (см. <c>MountRules.Capacity</c>).
    /// </summary>
    public int Capacity { get; set; }

    /// <summary>
    /// Цена. <c>null</c> — бесценно: обычная покупка недоступна, скакуна может только выдать
    /// ведущий. Это не то же самое, что цена ноль.
    /// </summary>
    public int? Price { get; set; }

    public int Rarity { get; set; }

    /// <summary>
    /// Снаряжение, которое идёт вместе со скакуном (упряжь, верховая сбруя). Коды, а не подписи:
    /// локализацию делает клиент.
    /// </summary>
    public List<string> IncludedGear { get; set; } = [];

    /// <summary>
    /// Скакуну нужна проверка Верховой езды в бою и под стрессом; сложность задаёт ведущий по
    /// ситуации, поэтому число здесь не хранится.
    /// </summary>
    public bool RequiresRidingCheck { get; set; }

    public List<MountSkill> Skills { get; set; } = [];
    public List<MountAbility> Abilities { get; set; } = [];
    public List<MountAttack> Attacks { get; set; } = [];

    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string Source { get; set; } = "";

    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
    public bool Retired { get; set; }
}

/// <summary>
/// Навык профиля скакуна. У Minion навыки групповые (ранг считается по размеру группы, поэтому
/// <see cref="Ranks"/> у них 0), у Rival — с обычными рангами.
/// </summary>
public class MountSkill
{
    public Guid Id { get; set; }
    public Guid MountDefId { get; set; }

    /// <summary>Английское имя навыка справочника, например <c>Athletics</c>.</summary>
    public required string Name { get; set; }
    public int Ranks { get; set; }

    /// <summary>Групповой навык Minion: ранг даёт группа, а не сама запись.</summary>
    public bool IsGroupSkill { get; set; }
}

/// <summary>Особая способность профиля скакуна (например, полёт).</summary>
public class MountAbility
{
    public Guid Id { get; set; }
    public Guid MountDefId { get; set; }

    public required string Name { get; set; }
    public string NameRu { get; set; } = "";
    public string Description { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
}

/// <summary>Структурная атака профиля скакуна: числа, а не строка снаряжения.</summary>
public class MountAttack
{
    public Guid Id { get; set; }
    public Guid MountDefId { get; set; }

    public required string Name { get; set; }
    public string NameRu { get; set; } = "";

    /// <summary>Английское имя боевого навыка для броска, например <c>Brawl</c>.</summary>
    public string SkillName { get; set; } = "";
    public int Damage { get; set; }
    public int Critical { get; set; }

    /// <summary>Дистанция атаки: у природных атак скакунов это <c>Engaged</c>.</summary>
    public WeaponRange Range { get; set; } = WeaponRange.Engaged;

    /// <summary>
    /// Коды качеств атаки из справочника качеств. У профилей книги рейтинга нет ни у одного
    /// качества, поэтому хранится только код.
    /// </summary>
    public List<string> QualityCodes { get; set; } = [];
}
