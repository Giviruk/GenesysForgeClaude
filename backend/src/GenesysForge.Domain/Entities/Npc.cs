namespace GenesysForge.Domain.Entities;

/// <summary>Пользовательский противник/NPC из библиотеки бестиария мастера.</summary>
public class Npc
{
    public Guid Id { get; set; }
    /// <summary>Владелец-мастер. <c>null</c> у встроенных существ (см. <see cref="IsBuiltIn"/>).</summary>
    public Guid? OwnerUserId { get; set; }
    /// <summary>Встроенное существо из преднаполненного бестиария: read-only, видно всем, клонируется.</summary>
    public bool IsBuiltIn { get; set; }
    /// <summary>Кампания, к которой привязан NPC (необязательно).</summary>
    public Guid? CampaignId { get; set; }

    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public NpcKind Kind { get; set; }
    public NpcRole Role { get; set; }
    public string Description { get; set; } = "";
    public string Source { get; set; } = "";

    // Характеристики (1..6)
    public int Brawn { get; set; } = 2;
    public int Agility { get; set; } = 2;
    public int Intellect { get; set; } = 2;
    public int Cunning { get; set; } = 2;
    public int Willpower { get; set; } = 2;
    public int Presence { get; set; } = 2;

    // Производные параметры
    public int WoundThreshold { get; set; } = 5;
    /// <summary>Порог усталости. У миньонов обычно отсутствует (null).</summary>
    public int? StrainThreshold { get; set; }
    public int Soak { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
    /// <summary>Силуэт (размер). Обычный гуманоид = 1; крупные монстры ≥ 2 (правило wound ≥ силуэт×10).</summary>
    public int Silhouette { get; set; } = 1;

    /// <summary>Тактика на 1–3 раунда (что NPC делает в бою). Свободный текст.</summary>
    public string Tactics { get; set; } = "";

    public NpcVisibility Visibility { get; set; } = NpcVisibility.Private;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<NpcSkill> Skills { get; set; } = [];
    public List<NpcAbility> Abilities { get; set; } = [];
    /// <summary>Структурные боевые атаки NPC.</summary>
    public List<NpcAttack> Attacks { get; set; } = [];
    /// <summary>Названия талантов NPC.</summary>
    public List<string> Talents { get; set; } = [];
    /// <summary>Небоевое снаряжение в свободной форме (боевое вынесено в <see cref="Attacks"/>).</summary>
    public List<string> Equipment { get; set; } = [];
    /// <summary>Теги для фильтрации (например «нежить», «лес», «командир»).</summary>
    public List<string> Tags { get; set; } = [];
}
