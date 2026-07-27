namespace GenesysForge.Domain.Entities;

/// <summary>
/// Улучшение Power героической способности (Improved/Supreme). Ability points начисляются
/// по одному за каждые 50 XP сверх стартового XP вида. Supreme требует купленного Improved.
/// </summary>
public class HeroicAbilityUpgradeDef
{
    public Guid Id { get; set; }
    public Guid HeroicAbilityDefId { get; set; }
    /// <summary>Уровень улучшения. Совпадает с рангом (Improved=1, Supreme=2).</summary>
    public HeroicUpgradeLevel Level { get; set; }
    /// <summary>Стоимость в очках улучшения (Improved=1, Supreme=2).</summary>
    public int Cost { get; set; }
    /// <summary>Полный (private) парафраз эффекта. Очищается в режиме PublicSafe.</summary>
    public string Description { get; set; } = "";
    /// <summary>Короткая copyright-safe сводка, остающаяся видимой в PublicSafe.</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    /// <summary>Особые условия/заметки улучшения.</summary>
    public string Notes { get; set; } = "";
}
