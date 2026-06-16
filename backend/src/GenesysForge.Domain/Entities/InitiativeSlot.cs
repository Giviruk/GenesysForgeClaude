namespace GenesysForge.Domain.Entities;

/// <summary>Слот инициативы Genesys: сторона (игроки/NPC) и порядок, опционально назначенный участник.</summary>
public class InitiativeSlot
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public InitiativeSlotType SlotType { get; set; }
    public int Order { get; set; }
    /// <summary>Назначенный участник, необязательно (слот может быть абстрактным).</summary>
    public Guid? AssignedParticipantId { get; set; }
    public string Notes { get; set; } = "";
}
