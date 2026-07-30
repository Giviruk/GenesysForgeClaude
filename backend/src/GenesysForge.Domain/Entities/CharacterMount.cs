namespace GenesysForge.Domain.Entities;

/// <summary>
/// Скакун, принадлежащий персонажу (ROT-MOUNT-ITEM-01). Собственного веса у скакуна нет: он не
/// предмет в рюкзаке и в Encumbrance владельца не входит. Груз скакуна живёт в
/// <see cref="CarriedLoad"/> и тоже не прибавляется к переносимому весу персонажа; раскладка груза
/// по позициям, повозки и установка попоны/седельных сумок — это будущий раздел «Транспорт»
/// (ROT-TRANSPORT-01).
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

    /// <summary>Полученные раны. Достигнув порога профиля, скакун выведен из строя.</summary>
    public int WoundsCurrent { get; set; }

    /// <summary>Текущая загрузка скакуна. Числом, потому что позиций груза до ROT-TRANSPORT-01 нет.</summary>
    public int CarriedLoad { get; set; }

    /// <summary>
    /// Скакун под седлом прямо сейчас. Активность не даёт механических эффектов сама по себе —
    /// это состояние для стола и будущего транспортного раздела.
    /// </summary>
    public bool IsActive { get; set; }

    public string Notes { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
