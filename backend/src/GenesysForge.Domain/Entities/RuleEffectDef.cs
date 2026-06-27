namespace GenesysForge.Domain.Entities;

/// <summary>
/// Структурный эффект таланта/героической способности (U-18, GF-012 / Аудит §2.2): что применить при
/// активации. Привязан к встроенной способности по <see cref="HeroicAbilityDefId"/>. Простые эффекты
/// применяются автоматически; <see cref="RuleEffectKind.Manual"/> — только подсказка (нарративные/GM-решения).
/// </summary>
public class RuleEffectDef
{
    public Guid Id { get; set; }

    /// <summary>Героика-источник (Stage 1). Таланты подключатся в Stage 2.</summary>
    public Guid? HeroicAbilityDefId { get; set; }

    public RuleEffectKind Kind { get; set; }
    /// <summary>Величина эффекта (раны/кубы/бонус). Для Manual — 0.</summary>
    public int Amount { get; set; }
    /// <summary>Длительность эффекта (русская подпись): «до конца хода», «раунд» и т. п.</summary>
    public string Duration { get; set; } = "";
    /// <summary>Человекочитаемое описание эффекта (для лога/подсказки).</summary>
    public string Description { get; set; } = "";
}
