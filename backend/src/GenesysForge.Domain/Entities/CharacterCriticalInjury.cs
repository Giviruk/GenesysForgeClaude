namespace GenesysForge.Domain.Entities;

/// <summary>
/// Критическое ранение персонажа (U-23). Снимок названия/тяжести, чтобы запись пережила
/// изменения справочника; <see cref="RuleCode"/> ссылается на строку таблицы крит-ранений
/// (<c>RuleTableEntry.Code</c>, <c>RuleTableKind.CriticalInjury</c>) из U-11, если выбран из неё.
/// </summary>
public class CharacterCriticalInjury
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }

    /// <summary>Код строки таблицы крит-ранений (U-11), если ранение выбрано из справочника; иначе пусто.</summary>
    public string? RuleCode { get; set; }

    /// <summary>Название ранения (снимок RU).</summary>
    public required string NameRu { get; set; }
    /// <summary>Тяжесть (группа таблицы: Лёгкая/Обычная/Сложная/Пугающая), может быть пустой.</summary>
    public string? Severity { get; set; }
    /// <summary>Результат броска d100, если фиксировался.</summary>
    public int? RollResult { get; set; }
    /// <summary>Заметки мастера/игрока по ранению.</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
