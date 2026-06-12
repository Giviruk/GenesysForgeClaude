namespace GenesysForge.Domain.Entities;

public class ItemDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public ItemKind Kind { get; set; }
    public int Encumbrance { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
    public int EncumbranceThresholdBonus { get; set; }
    public string Description { get; set; } = "";
    public int Price { get; set; }
    public int Rarity { get; set; }
    public Guid? OwnerUserId { get; set; }
}
