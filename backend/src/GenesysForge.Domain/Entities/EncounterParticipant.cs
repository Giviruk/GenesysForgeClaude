namespace GenesysForge.Domain.Entities;

/// <summary>Участник подготовленной сцены: персонаж кампании, NPC из библиотеки или ручной осложнение.</summary>
public class EncounterParticipant
{
    public Guid Id { get; set; }
    public Guid EncounterId { get; set; }

    /// <summary>Персонаж кампании, если участник — PC.</summary>
    public Guid? CharacterId { get; set; }
    /// <summary>NPC из библиотеки бестиария, если участник создан из неё.</summary>
    public Guid? NpcId { get; set; }

    public required string DisplayName { get; set; }
    public ParticipantType ParticipantType { get; set; }
    /// <summary>Сторона инициативы при отправке в Game Table.</summary>
    public InitiativeSlotType InitiativeSide { get; set; } = InitiativeSlotType.Npc;

    public int Quantity { get; set; } = 1;
    public string Notes { get; set; } = "";

    /// <summary>Скрыт от игроков в начале сцены.</summary>
    public bool StartsHidden { get; set; }
    /// <summary>Уже побеждён/неактивен в начале сцены.</summary>
    public bool StartsDefeated { get; set; }

    /// <summary>Переопределение порога ран (null — взять из источника).</summary>
    public int? StartingWoundsOverride { get; set; }
    /// <summary>Переопределение порога усталости (null — взять из источника).</summary>
    public int? StartingStrainOverride { get; set; }

    public int Order { get; set; }
}
