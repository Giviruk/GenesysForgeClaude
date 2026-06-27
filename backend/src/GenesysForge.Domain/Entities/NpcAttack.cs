namespace GenesysForge.Domain.Entities;

/// <summary>Структурная боевая атака NPC (оружие/природная атака). Заменяет боевые строки в <see cref="Npc.Equipment"/>.</summary>
public class NpcAttack
{
    public Guid Id { get; set; }
    public Guid NpcId { get; set; }

    /// <summary>Название атаки/оружия (русское), например «Длинный меч», «Когти».</summary>
    public string Name { get; set; } = "";
    /// <summary>Английское имя боевого навыка для броска, например «Melee (Heavy)», «Ranged». Может быть пусто.</summary>
    public string SkillName { get; set; } = "";
    /// <summary>Урон: «+3» (прибавка к характеристике в ближнем бою) или абсолютное число.</summary>
    public string Damage { get; set; } = "";
    /// <summary>Критическое значение (строкой: «2», «—»).</summary>
    public string Critical { get; set; } = "";
    /// <summary>Дистанция (русская подпись): «Вплотную», «Средняя» и т. п.</summary>
    public string RangeBand { get; set; } = "";
    /// <summary>Свободные заметки по атаке.</summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// Подпись предмета из снаряжения, из которого атака автосоздана (U-«атаки из снаряжения»).
    /// Пусто — кастомная (ручная) атака. Используется для синхронизации атак с оружием в инвентаре.
    /// </summary>
    public string SourceWeapon { get; set; } = "";

    /// <summary>Структурные качества атаки (свойство + рейтинг), ссылаются на справочник <see cref="QualityDef"/>.</summary>
    public List<NpcAttackQuality> Qualities { get; set; } = [];
}
