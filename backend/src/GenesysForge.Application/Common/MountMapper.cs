using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Проекция транспорта в DTO (ROT-MOUNT-ITEM-01, ROT-TRANSPORT-01). Вместимость, загрузка и
/// признаки состояния считаются здесь одним правилом для витрины и листа: клиент не выводит их сам.
/// </summary>
public static class MountMapper
{
    public static MountDefDto DefDto(MountDef def) => new(
        def.Id,
        def.Code,
        def.Name,
        def.NameRu,
        def.TransportKind,
        def.MovementMode,
        def.RequiresTraction,
        def.Kind,
        new Dictionary<string, int>
        {
            ["brawn"] = def.Brawn,
            ["agility"] = def.Agility,
            ["intellect"] = def.Intellect,
            ["cunning"] = def.Cunning,
            ["willpower"] = def.Willpower,
            ["presence"] = def.Presence,
        },
        def.Soak,
        def.WoundThreshold,
        def.StrainThreshold,
        def.MeleeDefense,
        def.RangedDefense,
        def.Silhouette,
        MountRules.Capacity(def),
        def.Price,
        def.Rarity,
        [.. def.IncludedGear],
        def.RequiresRidingCheck,
        [.. def.Skills
            .OrderByDescending(s => s.Ranks).ThenBy(s => s.Name, StringComparer.Ordinal)
            .Select(s => new MountSkillDto(s.Name, s.Ranks, s.IsGroupSkill))],
        [.. def.Abilities.Select(a => new MountAbilityDto(
            a.Name, a.NameRu, a.Description, a.DescriptionEn))],
        [.. def.Attacks.Select(a => new MountAttackDto(
            a.Name, a.NameRu, a.SkillName, a.Damage, a.Critical, a.Range, [.. a.QualityCodes]))],
        def.Description,
        def.DescriptionEn,
        def.Source);

    /// <summary>
    /// Транспорт персонажа. <c>MountDef</c> обязателен: без профиля у экземпляра нет ни порога ран,
    /// ни вместимости, поэтому запись без него в лист не попадает.
    /// </summary>
    /// <param name="all">Весь транспорт персонажа: нужен, чтобы показать кличку тяглового животного.</param>
    /// <param name="items">
    /// Все позиции инвентаря персонажа. Груз отбирается по <c>CarriedByMountId</c>, а не через
    /// навигацию: это те же строки, и они уже загружены со всеми справочниками.
    /// </param>
    /// <param name="cargoDto">
    /// Проекция позиции груза. Приходит снаружи, потому что груз обязан считаться теми же
    /// поправками экземпляра, что и обычная позиция инвентаря.
    /// </param>
    public static CharacterMountDto InstanceDto(
        CharacterMount mount,
        IEnumerable<CharacterMount> all,
        IEnumerable<CharacterItem> items,
        Func<CharacterItem, CharacterItemDto> cargoDto)
    {
        var def = mount.MountDef
            ?? throw new InvalidOperationException("Транспорт загружен без профиля.");
        var cargo = items
            .Where(i => i.ItemDef is not null && i.CarriedByMountId == mount.Id)
            .ToList();
        var installedBonus = MountRules.InstalledCapacityBonus(cargo);
        var load = MountRules.CargoLoad(cargo);
        var protection = MountRules.InstalledProtection(cargo);

        var drawnBy = mount.DrawnByMountId is { } id ? all.FirstOrDefault(m => m.Id == id) : null;

        return new CharacterMountDto(
            mount.Id,
            mount.MountDefId,
            string.IsNullOrWhiteSpace(mount.Name) ? def.NameRu : mount.Name,
            mount.Name,
            DefDto(def),
            mount.WoundsCurrent,
            load,
            MountRules.Capacity(def, installedBonus),
            mount.IsActive,
            MountRules.IsOverloaded(def, load, installedBonus),
            MountRules.IsIncapacitated(def, mount.WoundsCurrent),
            mount.Provenance,
            mount.Notes,
            mount.DrawnByMountId,
            drawnBy is null ? "" : DisplayName(drawnBy),
            MountRules.NeedsTraction(def, mount.DrawnByMountId),
            def.Soak + protection.Soak,
            def.MeleeDefense + protection.MeleeDefense,
            def.RangedDefense + protection.RangedDefense,
            [.. cargo
                .OrderByDescending(i => i.IsInstalledOnMount)
                .ThenBy(i => i.ItemDef!.NameRu, StringComparer.Ordinal)
                .Select(cargoDto)]);
    }

    /// <summary>Кличка, если задана, иначе название профиля.</summary>
    public static string DisplayName(CharacterMount mount) =>
        string.IsNullOrWhiteSpace(mount.Name) ? mount.MountDef?.NameRu ?? "" : mount.Name;
}
