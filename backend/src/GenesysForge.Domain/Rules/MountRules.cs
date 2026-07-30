using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Правила скакунов (ROT-MOUNT-ITEM-01). Скакун не предмет: его вес не входит в Encumbrance
/// владельца, а вместимость берётся из профиля книги, а не выводится из характеристик.
/// </summary>
public static class MountRules
{
    /// <summary>
    /// Вместимость скакуна. Профиль книги задаёт своё число и приоритетнее общего правила
    /// <c>5 + Brawn</c>: у Beast of Burden 18 при Brawn 4, а не 9. Профиль без вместимости
    /// (кастомная запись) падает на общее правило.
    /// </summary>
    public static int Capacity(MountDef def) =>
        def.Capacity > 0 ? def.Capacity : GenericCapacity(def.Brawn);

    /// <summary>Общее правило вместимости живого существа, когда профиль своего числа не даёт.</summary>
    public static int GenericCapacity(int brawn) => 5 + brawn;

    /// <summary>Скакун перегружен: груза больше, чем допускает вместимость.</summary>
    public static bool IsOverloaded(MountDef def, int carriedLoad) => carriedLoad > Capacity(def);

    /// <summary>
    /// Скакун выведен из строя: раны достигли порога профиля. Ниже порога скакун ранен, но
    /// работает — счётчик ран сам по себе ничего не запрещает.
    /// </summary>
    public static bool IsIncapacitated(MountDef def, int woundsCurrent) =>
        woundsCurrent >= def.WoundThreshold;

    /// <summary>
    /// Раны в границах профиля: отрицательных не бывает, выше порога считать бессмысленно.
    /// </summary>
    public static int ClampWounds(MountDef def, int woundsCurrent) =>
        Math.Clamp(woundsCurrent, 0, def.WoundThreshold);
}
