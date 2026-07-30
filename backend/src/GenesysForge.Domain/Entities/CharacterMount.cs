namespace GenesysForge.Domain.Entities;

/// <summary>
/// Транспорт, принадлежащий персонажу: скакун или повозка (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01).
/// Собственного веса у транспорта нет: он не предмет в рюкзаке и в Encumbrance владельца не входит.
/// Груз — это обычные позиции инвентаря с <c>CarriedByMountId</c>, и они тоже не прибавляются к
/// переносимому весу персонажа.
/// </summary>
public class CharacterMount
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid MountDefId { get; set; }
    public MountDef? MountDef { get; set; }

    /// <summary>Личная кличка. Пусто — показывается название профиля.</summary>
    public string Name { get; set; } = "";

    /// <summary>Откуда скакун появился: покупка, выдача ведущим, импорт.</summary>
    public ItemProvenance Provenance { get; set; } = ItemProvenance.Purchased;

    /// <summary>Полученные раны. Достигнув порога профиля, транспорт выведен из строя.</summary>
    public int WoundsCurrent { get; set; }

    /// <summary>
    /// Тягловое животное, которое везёт эту повозку (ROT-TRANSPORT-01). <c>null</c> — тяги нет:
    /// повозка стоит на месте, но не исчезает и груз владельцу не переходит. У скакунов всегда
    /// <c>null</c>: скакун — это тяга, а не то, что тянут.
    /// </summary>
    public Guid? DrawnByMountId { get; set; }
    public CharacterMount? DrawnBy { get; set; }

    /// <summary>
    /// Груз и установленное снаряжение этого транспорта. Позиция инвентаря со ссылкой сюда не
    /// входит в переносимый вес владельца. Отдельным Include не грузится: это те же строки
    /// <c>Character.Items</c>, и лист раскладывает их по <c>CarriedByMountId</c>.
    /// </summary>
    public List<CharacterItem> Cargo { get; set; } = [];

    /// <summary>
    /// Скакун под седлом прямо сейчас. Активность не даёт механических эффектов сама по себе —
    /// это состояние для стола и выбранного за столом транспорта.
    /// </summary>
    public bool IsActive { get; set; }

    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
