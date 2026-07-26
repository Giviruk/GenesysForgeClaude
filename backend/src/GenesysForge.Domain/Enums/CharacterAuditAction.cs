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
}
