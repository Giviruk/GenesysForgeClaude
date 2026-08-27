using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Entities;

/// <summary>Участник сцены Game Table (персонаж игрока, NPC, группа миньонов или осложнение).</summary>
public class GameParticipant : ICombatTarget
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }

    /// <summary>Персонаж игрока, если это PC.</summary>
    public Guid? CharacterId { get; set; }
    /// <summary>NPC из библиотеки бестиария, если участник создан из неё.</summary>
    public Guid? NpcId { get; set; }

    public required string DisplayName { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public InitiativeSlotType InitiativeSlotType { get; set; }

    /// <summary>Количество (для группы миньонов).</summary>
    public int Count { get; set; } = 1;

    public int WoundsCurrent { get; set; }
    public int WoundsThreshold { get; set; }
    public int StrainCurrent { get; set; }
    public int? StrainThreshold { get; set; }
    public int Soak { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }

    /// <summary>Временные бонусные кубы, назначенные участнику на игровом столе.</summary>
    public int BoostDice { get; set; }
    /// <summary>Временные кубы помехи, назначенные участнику на игровом столе.</summary>
    public int SetbackDice { get; set; }

    /// <summary>Счётчик активных критических ранений участника (U-23) для быстрого учёта за столом.</summary>
    public int CriticalInjuries { get; set; }
    /// <summary>Число активаций героической способности PC в текущей игровой сессии.</summary>
    public int HeroicAbilityUses { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDefeated { get; set; }
    public bool IsHiddenFromPlayers { get; set; }
    public string Notes { get; set; } = "";

    /// <summary>Зона дистанций на поле Game Table; null означает старую запись без сохранённой позиции.</summary>
    public string? RangeZone { get; set; }
    /// <summary>Угол токена на кольце в градусах, если позиция сохранена.</summary>
    public double? RangeAngle { get; set; }

    public int Order { get; set; }
}
