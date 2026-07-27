namespace GenesysForge.Domain.Entities;

/// <summary>
/// Параметры primary effect, выбираемые вместе со способностью (ROT-HA-02): навык Paragon и
/// категория Sixth Sense. Строка одна на персонажа; смена способности во время создания удаляет её
/// вместе с созданными экземплярами, чтобы параметр чужого эффекта не пережил замену.
/// </summary>
public class CharacterHeroicConfiguration
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    /// <summary>Выбранный навык Paragon. Все уровни способности работают только с ним.</summary>
    public Guid? ParagonSkillDefId { get; set; }
    public SkillDef? ParagonSkillDef { get; set; }

    /// <summary>
    /// Снимок отображаемого имени навыка на момент выбора. Нужен, если позже кастомный навык
    /// скрыт: сервер показывает предупреждение о ремонте, но не подставляет другой навык молча.
    /// </summary>
    public string ParagonSkillName { get; set; } = "";

    /// <summary>Категория воспринимаемых существ или явлений для Sixth Sense (1–300 символов).</summary>
    public string SixthSenseSubject { get; set; } = "";
}
