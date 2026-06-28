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
}
