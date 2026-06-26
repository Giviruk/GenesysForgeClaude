namespace GenesysForge.Domain.Entities;

/// <summary>
/// Стартовый навык вида. Фиксированные (<see cref="IsChoice"/> = false) применяются автоматически
/// при создании персонажа; выборы (например «1 ранг в двух разных некарьерных навыках») закрываются
/// пикером в форме создания.
/// </summary>
public class ArchetypeStartingSkill
{
    public Guid Id { get; set; }
    public Guid ArchetypeId { get; set; }
    /// <summary>
    /// Каноническое (английское) имя навыка — совпадает с <c>SkillDef.Name</c> (как CareerSkillNames).
    /// Для выбора (<see cref="IsChoice"/>) пусто: конкретный навык выбирает игрок.
    /// </summary>
    public string SkillName { get; set; } = "";
    /// <summary>Русское имя навыка для отображения (для фиксированных).</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Сколько бесплатных рангов даёт (обычно 1, иногда 2).</summary>
    public int FreeRanks { get; set; } = 1;
    /// <summary>Это не фиксированный навык, а выбор игрока при создании.</summary>
    public bool IsChoice { get; set; }
    /// <summary>Группа выбора (например «any-noncareer» — N разных некарьерных навыков).</summary>
    public string ChoiceGroup { get; set; } = "";
    /// <summary>Сколько навыков нужно выбрать в группе (для <see cref="IsChoice"/>).</summary>
    public int ChoiceCount { get; set; }
}
