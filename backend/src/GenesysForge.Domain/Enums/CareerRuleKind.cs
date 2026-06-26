namespace GenesysForge.Domain.Entities;

/// <summary>Вид карьерного правила/заметки (для группировки и будущей автоматизации).</summary>
public enum CareerRuleKind
{
    /// <summary>Совет/примечание по карьере (показывается игроку, не автоматизируется).</summary>
    Advisory,
    /// <summary>Замена навыка (например обобщённый Melee → Melee (Light) в системах с раздельными навыками).</summary>
    SkillSubstitution,
}
