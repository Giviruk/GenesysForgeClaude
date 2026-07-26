namespace GenesysForge.Domain.Entities;

/// <summary>Выбранный персонажем стандартный Secondary Effect (не более двух разных).</summary>
public class CharacterHeroicSecondaryEffect
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid HeroicSecondaryEffectDefId { get; set; }
    public HeroicSecondaryEffectDef? HeroicSecondaryEffectDef { get; set; }
}
