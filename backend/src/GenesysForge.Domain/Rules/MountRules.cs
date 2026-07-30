using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Защищённость транспорта после установленного снаряжения (ROT-MOUNT-NPC-01). Числа считаются на
/// чтение и статблок профиля не меняют: снятая попона возвращает исходные значения сама собой.
/// </summary>
public sealed record MountProtection(int Soak, int MeleeDefense, int RangedDefense);

/// <summary>
/// Правила транспорта — скакунов и повозок (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Транспорт не
/// предмет: его вес не входит в Encumbrance владельца, груз считается по своим позициям, а
/// вместимость берётся из профиля книги, а не выводится из характеристик.
/// </summary>
public static class MountRules
{
    /// <summary>
    /// Вместимость транспорта. Профиль книги задаёт своё число и приоритетнее общего правила
    /// <c>5 + Brawn</c>: у Beast of Burden 18 при Brawn 4, а не 9. Профиль без вместимости
    /// (кастомная запись) падает на общее правило. <paramref name="installedBonus"/> — прибавка от
    /// установленного снаряжения, например седельных сумок.
    /// </summary>
    public static int Capacity(MountDef def, int installedBonus = 0) =>
        (def.Capacity > 0 ? def.Capacity : GenericCapacity(def.Brawn)) + Math.Max(0, installedBonus);

    /// <summary>Общее правило вместимости живого существа, когда профиль своего числа не даёт.</summary>
    public static int GenericCapacity(int brawn) => 5 + brawn;

    /// <summary>
    /// Загрузка транспорта по позициям груза: вес позиции на её количество. Правило «десять
    /// предметов с нулевым весом дают единицу» здесь не применяется — оно про то, что персонаж
    /// таскает на себе, и книга не переносит его на вьючный груз.
    /// </summary>
    public static int CargoLoad(IEnumerable<CharacterItem> cargo) =>
        cargo.Where(i => !i.IsInstalledOnMount)
            .Sum(i => Math.Max(0, i.ItemDef?.Encumbrance ?? 0) * Math.Max(1, i.Quantity));

    /// <summary>
    /// Прибавка вместимости от установленного снаряжения: у седельных сумок это их бонус к порогу
    /// нагрузки. Сами сумки вместимость не занимают.
    /// </summary>
    public static int InstalledCapacityBonus(IEnumerable<CharacterItem> cargo) =>
        cargo.Where(i => i.IsInstalledOnMount)
            .Sum(i => Math.Max(0, i.ItemDef?.EncumbranceThresholdBonus ?? 0) * Math.Max(1, i.Quantity));

    /// <summary>
    /// Итоговая защищённость транспорта с учётом установленного снаряжения (ROT-MOUNT-NPC-01).
    /// Всаднику эти числа не достаются — установленная позиция не входит в его надетое снаряжение.
    ///
    /// <para>
    /// Поглощение складывается, а защита — нет: и профиль, и попона «задают Defense N», а такие
    /// источники по ROT-CMB-03 конкурируют за максимум, а не суммируются. Поэтому попона с защитой 1
    /// поднимает вьючное животное с нуля до единицы, но летающему скакуну с напечатанной дальней
    /// защитой 2 ничего не добавляет. Считает общий <see cref="DefenseAggregator"/> — тот же, что и
    /// у персонажа, вместе с общим пределом 4.
    /// </para>
    /// </summary>
    public static MountProtection Protection(MountDef def, IEnumerable<CharacterItem> cargo)
    {
        var installed = cargo.Where(i => i.IsInstalledOnMount && i.ItemDef is not null).ToList();

        var contributions = new List<DefenseContribution>();
        if (def.MeleeDefense > 0)
            contributions.Add(new DefenseContribution(
                "Profile", def.Name, DefenseScope.Melee, DefenseMode.Provides, def.MeleeDefense));
        if (def.RangedDefense > 0)
            contributions.Add(new DefenseContribution(
                "Profile", def.Name, DefenseScope.Ranged, DefenseMode.Provides, def.RangedDefense));
        foreach (var item in installed)
        {
            if (item.ItemDef!.MeleeDefense > 0)
                contributions.Add(new DefenseContribution(
                    "Item", item.ItemDef.Name, DefenseScope.Melee, DefenseMode.Provides,
                    item.ItemDef.MeleeDefense));
            if (item.ItemDef.RangedDefense > 0)
                contributions.Add(new DefenseContribution(
                    "Item", item.ItemDef.Name, DefenseScope.Ranged, DefenseMode.Provides,
                    item.ItemDef.RangedDefense));
        }

        return new MountProtection(
            def.Soak + installed.Sum(i => Math.Max(0, i.ItemDef!.SoakBonus)),
            DefenseAggregator.Melee(contributions).Effective,
            DefenseAggregator.Ranged(contributions).Effective);
    }

    /// <summary>
    /// Попона рассчитана на боевого скакуна: на любого другого её ставит ведущий своим решением,
    /// и это решение обязано попасть в историю персонажа вместе с причиной (ROT-MOUNT-NPC-01).
    /// </summary>
    public static bool RequiresGmApprovalForBarding(MountDef def) =>
        !string.Equals(BareCode(def.Code), WarMountCode, StringComparison.Ordinal);

    /// <summary>Стабильный bare-код боевого скакуна: единственный профиль, которому попона положена.</summary>
    private const string WarMountCode = "war-mount";

    /// <summary>Код без префикса системы и типа: <c>rot.mount.war-mount</c> → <c>war-mount</c>.</summary>
    private static string BareCode(string code) => code[(code.LastIndexOf('.') + 1)..];

    /// <summary>Транспорт перегружен: груза больше, чем допускает вместимость с учётом сумок.</summary>
    public static bool IsOverloaded(MountDef def, int carriedLoad, int installedBonus = 0) =>
        carriedLoad > Capacity(def, installedBonus);

    /// <summary>
    /// Транспорт стоит: ему нужна тяга, а тяглового животного не назначено. Груз при этом остаётся
    /// на транспорте — правило говорит «не движется», а не «исчезает».
    /// </summary>
    public static bool NeedsTraction(MountDef def, Guid? drawnByMountId) =>
        def.RequiresTraction && drawnByMountId is null;

    /// <summary>
    /// Может ли этот профиль тянуть повозку. Тянет живой скакун, которому тяга не нужна самому:
    /// повозку в повозку не запрягают.
    /// </summary>
    public static bool CanDraw(MountDef def) =>
        def.TransportKind == TransportKind.Mount && !def.RequiresTraction;

    /// <summary>
    /// Транспорт выведен из строя: раны достигли порога профиля. Ниже порога он повреждён, но
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
