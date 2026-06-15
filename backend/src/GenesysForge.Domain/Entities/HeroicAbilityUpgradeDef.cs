namespace GenesysForge.Domain.Entities;

/// <summary>
/// Улучшение героической способности (Improved/Supreme). Покупается за «очки улучшения»:
/// 1 стартовое + по 1 каждые 50 заработанного XP. Supreme требует предварительно купленного Improved.
/// </summary>
public class HeroicAbilityUpgradeDef
{
    public Guid Id { get; set; }
    public Guid HeroicAbilityDefId { get; set; }
    /// <summary>Уровень улучшения. Совпадает с рангом (Improved=1, Supreme=2).</summary>
    public HeroicUpgradeLevel Level { get; set; }
    /// <summary>Стоимость в очках улучшения (Improved=1, Supreme=2).</summary>
    public int Cost { get; set; }
    /// <summary>Эффект улучшения (RU-переработка).</summary>
    public string Description { get; set; } = "";
    /// <summary>Особые условия/заметки улучшения.</summary>
    public string Notes { get; set; } = "";
}
