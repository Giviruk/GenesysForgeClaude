namespace GenesysForge.Domain.Entities;

/// <summary>Определение навыка (встроенное или кастомное — у кастомного задан OwnerUserId).</summary>
public class SkillDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public CharacteristicType Characteristic { get; set; }
    public SkillKind Kind { get; set; }
    public Guid? OwnerUserId { get; set; }
}
