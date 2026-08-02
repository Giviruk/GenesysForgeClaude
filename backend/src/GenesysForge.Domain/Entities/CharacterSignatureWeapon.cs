namespace GenesysForge.Domain.Entities;

/// <summary>
/// Именное оружие способности Signature Weapon (ROT-HA-02). Хранится как экземпляр персонажа,
/// а не как глобальный кастомный <see cref="ItemDef"/>: оружие существует только у своего героя.
/// Строка одна на персонажа, поэтому потерянное оружие и его замена не могут быть активны разом.
/// </summary>
public class CharacterSignatureWeapon
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    /// <summary>Профиль из таблицы. Числа по нему строит сервер, в базе они не дублируются.</summary>
    public SignatureWeaponProfile Profile { get; set; }

    /// <summary>Качество изготовления, выбранное вместе с профилем.</summary>
    public WeaponCraftsmanship Craftsmanship { get; set; }

    /// <summary>Описание формы. На цифры не влияет — определяет совместимость улучшений.</summary>
    public string NarrativeForm { get; set; } = "";

    /// <summary>
    /// Подтверждённые GM признаки формы. Совместимость улучшений считается по ним, а не по тексту
    /// <see cref="NarrativeForm"/>: выводить механику разбором названия запрещено.
    /// </summary>
    public WeaponFormTraits FormTraits { get; set; }

    /// <summary>
    /// Базовое улучшение, выбранное вместе с формой (ROT-HA-02). Оно не установлено физически:
    /// стоит 0, занимает 0 слотов и действует только вместе с героической способностью, поэтому
    /// экземпляра <see cref="CharacterAttachment"/> у него нет — хранится сам выбор.
    /// </summary>
    public Guid? BaseAttachmentDefId { get; set; }
    public AttachmentDef? BaseAttachment { get; set; }

    /// <summary>
    /// Оружие потеряно или уничтожено. Пока флаг стоит, профиль не действует; отдельная команда
    /// возвращает прежнее оружие или выдаёт замену — эта же строка, а не вторая.
    /// </summary>
    public bool IsLost { get; set; }
}
