namespace GenesysForge.Domain.Entities;

public class CharacterItem
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid ItemDefId { get; set; }
    public ItemDef? ItemDef { get; set; }
    public int Quantity { get; set; } = 1;
    public ItemState State { get; set; } = ItemState.Carried;
    /// <summary>Откуда позиция появилась: покупка, карьерный комплект, стартовый бюджет или импорт.</summary>
    public ItemProvenance Provenance { get; set; } = ItemProvenance.Purchased;
}
