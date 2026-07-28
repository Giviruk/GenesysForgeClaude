using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Что персонаж уже держит и носит — вход для проверки свободных мест.</summary>
/// <param name="Kind">Вид предмета.</param>
/// <param name="Traits">Признаки формы: по ним считается, сколько рук занимает оружие.</param>
/// <param name="IsImplement">
/// Запись — магический инструмент (ROT-MAG-IMP-01). На одну магическую проверку работает ровно
/// один, поэтому и держать в руках больше одного бессмысленно.
/// </param>
public sealed record EquippedItemInput(
    Guid Id, ItemKind Kind, WeaponFormTraits Traits, string Name, bool IsImplement = false);

/// <summary>
/// Ограничения на одновременно используемое снаряжение. Раньше их не было вовсе: персонаж мог
/// надеть три доспеха и держать в руках четыре двуручных меча, а лист считал это нормой.
/// <para>
/// Модель простая и совпадает с тем, как это выглядит за столом: у персонажа две руки. Двуручное
/// оружие занимает обе, одноручное и рукопашное — одну, брони можно носить только одну.
/// </para>
/// </summary>
public static class EquipmentSlotRules
{
    /// <summary>Сколько рук у персонажа.</summary>
    public const int Hands = 2;

    /// <summary>Сколько броней можно носить одновременно.</summary>
    public const int MaxWornArmor = 1;

    /// <summary>
    /// Сколько рук занимает оружие. Двуручное — обе; всё остальное, включая щиты, пращу и
    /// рукопашное, — одну. Стопка считается за один предмет: держат один экземпляр, а не всю пачку.
    /// </summary>
    public static int HandCost(WeaponFormTraits traits) =>
        traits.HasFlag(WeaponFormTraits.TwoHanded) ? Hands : 1;

    /// <summary>Оружие держат двумя руками — с ним не берут ничего второго.</summary>
    public static bool IsTwoHanded(WeaponFormTraits traits) => HandCost(traits) == Hands;

    /// <summary>Занятые руки при текущем наборе используемого оружия.</summary>
    public static int UsedHands(IEnumerable<EquippedItemInput> equipped) =>
        equipped.Where(i => i.Kind == ItemKind.Weapon).Sum(i => HandCost(i.Traits));

    /// <summary>Сколько броней уже надето.</summary>
    public static int WornArmor(IEnumerable<EquippedItemInput> equipped) =>
        equipped.Count(i => i.Kind == ItemKind.Armor);

    /// <summary>
    /// Проверяет, можно ли начать использовать предмет. Уже используемые предметы приходят без
    /// проверяемого — вызывающий исключает его сам, иначе смена количества считалась бы надеванием.
    /// </summary>
    /// <param name="isImplement">
    /// Предмет — магический инструмент: их берут в руки по одному (ROT-MAG-IMP-01), потому что
    /// на одну проверку всё равно работает только один, а лист иначе показывал бы шесть посохов
    /// сразу и делал вид, что все шесть чем-то помогают.
    /// </param>
    public static void EnsureCanEquip(
        ItemKind kind, WeaponFormTraits traits, IReadOnlyList<EquippedItemInput> equipped,
        bool isImplement = false)
    {
        if (isImplement && equipped.Any(e => e.IsImplement))
            throw new DomainRuleException(
                "На одну магическую проверку работает только один инструмент — уберите тот, что в руках.",
                "implement.limit");

        if (kind == ItemKind.Armor)
        {
            if (WornArmor(equipped) >= MaxWornArmor)
                throw new DomainRuleException(
                    "Больше одной брони одновременно не носят — снимите надетую.",
                    "equipment.armor_limit");
            return;
        }

        if (kind != ItemKind.Weapon) return;

        var used = UsedHands(equipped);
        var cost = HandCost(traits);
        if (used + cost <= Hands) return;

        throw new DomainRuleException(
            cost == Hands
                ? "Двуручное оружие занимает обе руки — сначала уберите то, что в руках."
                : "Обе руки заняты — уберите одно из оружий.",
            "equipment.hands_full");
    }

    /// <summary>
    /// Набор используемого снаряжения непротиворечив: не больше одной брони и не больше двух рук.
    /// Нужен для импорта чужого файла, где ограничений могло не быть вовсе.
    /// </summary>
    public static bool IsValid(IReadOnlyList<EquippedItemInput> equipped) =>
        WornArmor(equipped) <= MaxWornArmor && UsedHands(equipped) <= Hands
        && equipped.Count(e => e.IsImplement) <= MaxUsedImplements;

    /// <summary>Больше одного магического инструмента в руках не держат (ROT-MAG-IMP-01).</summary>
    public const int MaxUsedImplements = 1;
}
