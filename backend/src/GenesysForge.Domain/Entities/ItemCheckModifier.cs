namespace GenesysForge.Domain.Entities;

/// <summary>
/// Влияние предмета на проверки навыков (ROT-ARM-01): «+1 помеха к Скрытности» у кольчуги и
/// подобные эффекты. Навык хранится английским именем из каталога, а не ссылкой на строку
/// справочника: у кастомных систем своей строки может не быть, а правило всё равно книжное.
/// </summary>
public class ItemCheckModifier
{
    public Guid Id { get; set; }
    public Guid ItemDefId { get; set; }

    public CheckModifierKind Kind { get; set; }

    /// <summary>
    /// Английское имя навыка («Stealth»). Пусто — модификатор относится ко всем проверкам,
    /// отобранным по <see cref="Characteristic"/>.
    /// </summary>
    public string SkillName { get; set; } = "";

    /// <summary>
    /// Характеристика, к проверкам которой относится модификатор. Задаётся вместо
    /// <see cref="SkillName"/>, когда правило говорит «ко всем проверкам Мощи».
    /// </summary>
    public CharacteristicType? Characteristic { get; set; }

    /// <summary>Сколько костей. Всегда положительное число; направление задаёт <see cref="Kind"/>.</summary>
    public int Value { get; set; }

    /// <summary>
    /// Модификатор действует, только когда предмет надет. Для брони это дополнительно означает
    /// «выбрана активной» (ROT-CMB-02): вторая надетая броня даёт только вес.
    /// </summary>
    public bool RequiresWorn { get; set; } = true;

    /// <summary>
    /// Условие из книги, при котором правило вообще применимо («холодная погода»). Пусто —
    /// применяется всегда. Приложение условие не проверяет: оно показывается игроку и ведущему.
    /// </summary>
    public string Condition { get; set; } = "";
}
