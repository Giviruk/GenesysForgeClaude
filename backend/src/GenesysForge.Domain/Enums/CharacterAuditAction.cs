namespace GenesysForge.Domain.Entities;

/// <summary>Тип записи в истории персонажа (что произошло с XP/листом).</summary>
public enum CharacterAuditAction
{
    XpAwarded,
    CharacteristicBought,
    CharacteristicRefunded,
    SkillRankBought,
    SkillRankRefunded,
    TalentBought,
    TalentRefunded,
    ItemBought,
    ItemSold,
    ItemRemoved,
    HeroicAbilityChanged,
    CreationCompleted,
    ManualEdit,
    AbilityActivated,
    /// <summary>Персонаж создан: режим стартового снаряжения, формула и фактический бросок денег.</summary>
    CharacterCreated,

    /// <summary>Задано личное название и происхождение героической способности (ROT-HA-01).</summary>
    HeroicIdentitySet,

    /// <summary>Происхождение героической способности брошено по таблице d10: сохранены фактические грани.</summary>
    HeroicOriginRolled,

    /// <summary>Выбран параметр primary effect: навык Paragon, категория Sixth Sense или оружие (ROT-HA-02).</summary>
    HeroicParameterSet,

    /// <summary>Именное оружие потеряно, возвращено или заменено.</summary>
    SignatureWeaponReplaced,

    /// <summary>Выбрана или снята активная броня (ROT-CMB-02).</summary>
    ActiveArmorChanged,

    /// <summary>Метательное оружие брошено или подобрано (ROT-WPN-01).</summary>
    ItemThrownChanged,

    /// <summary>Куплено улучшение предмета (ROT-EQP-ATT-01).</summary>
    AttachmentBought,

    /// <summary>Улучшение установлено на предмет (ROT-EQP-ATT-01).</summary>
    AttachmentInstalled,

    /// <summary>Улучшение снято с предмета: возвращено, уничтожено или испорчено.</summary>
    AttachmentDetached,

    /// <summary>Изменено состояние повреждения предмета или улучшения (GEN-EQP-DMG-01).</summary>
    ItemDamageStateChanged,

    /// <summary>Предмет или улучшение починены: списаны материалы, состояние стало целым.</summary>
    ItemRepaired,

    /// <summary>Ведущий настроил магический инструмент: выбраны бесплатные эффекты (ROT-MAG-IMP-01).</summary>
    ImplementConfigured,

    /// <summary>Ведущий необратимо настроил activation и бесплатный эффект Lesser Rune (ROT-MAG-11).</summary>
    ShardConfigured,

    /// <summary>Куплена или бесплатно выдана услуга; предмет в инвентаре не создаётся.</summary>
    ServiceBought,
}
