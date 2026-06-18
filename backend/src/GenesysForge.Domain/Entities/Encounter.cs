namespace GenesysForge.Domain.Entities;

/// <summary>Подготовленная сцена/столкновение кампании (см. спецификацию Encounter Builder).</summary>
public class Encounter
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }

    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public EncounterType Type { get; set; }
    public ThreatLevel ThreatLevel { get; set; } = ThreatLevel.Standard;

    /// <summary>Приватное описание для мастера.</summary>
    public string GmDescription { get; set; } = "";
    /// <summary>Описание, видимое игрокам.</summary>
    public string PlayerDescription { get; set; } = "";
    /// <summary>Цели игроков (свободный текст).</summary>
    public string PlayerGoals { get; set; } = "";
    /// <summary>Цели NPC (приватно).</summary>
    public string NpcGoals { get; set; } = "";

    public string Location { get; set; } = "";
    public string Environment { get; set; } = "";
    /// <summary>Осложнения — приватные заметки мастера.</summary>
    public string Complications { get; set; } = "";
    public string Rewards { get; set; } = "";

    /// <summary>Открыта ли публичная часть сцены игрокам.</summary>
    public bool IsVisibleToPlayers { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Теги для фильтрации (например «лес», «засада», «босс»).</summary>
    public List<string> Tags { get; set; } = [];
    public List<EncounterParticipant> Participants { get; set; } = [];
}
