namespace GenesysForge.Domain.Entities;

/// <summary>Состояние активной сцены кампании (пульт мастера / Game Table).</summary>
public class GameSession
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public bool IsActive { get; set; } = true;

    public int PlayerStoryPoints { get; set; }
    public int GmStoryPoints { get; set; }

    public int CurrentRound { get; set; } = 1;
    /// <summary>Индекс текущего слота инициативы (0..N-1).</summary>
    public int CurrentTurnIndex { get; set; }

    /// <summary>Заметки, видимые игрокам.</summary>
    public string PublicNotes { get; set; } = "";
    /// <summary>Приватные заметки мастера.</summary>
    public string GmNotes { get; set; } = "";

    /// <summary>Разрешено ли игрокам менять wounds/strain своего персонажа.</summary>
    public bool AllowPlayerEdits { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<GameParticipant> Participants { get; set; } = [];
    public List<InitiativeSlot> Slots { get; set; } = [];
}
