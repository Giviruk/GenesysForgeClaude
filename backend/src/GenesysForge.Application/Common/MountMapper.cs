using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Проекция скакунов в DTO (ROT-MOUNT-ITEM-01). Вместимость и признаки состояния считаются здесь
/// одним правилом для витрины и листа: клиент не выводит их сам.
/// </summary>
public static class MountMapper
{
    public static MountDefDto DefDto(MountDef def) => new(
        def.Id,
        def.Code,
        def.Name,
        def.NameRu,
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
    /// Скакун персонажа. <c>MountDef</c> обязателен: без профиля у экземпляра нет ни порога ран,
    /// ни вместимости, поэтому запись без него в лист не попадает.
    /// </summary>
    public static CharacterMountDto InstanceDto(CharacterMount mount)
    {
        var def = mount.MountDef
            ?? throw new InvalidOperationException("Скакун загружен без профиля.");
        return new CharacterMountDto(
            mount.Id,
            mount.MountDefId,
            string.IsNullOrWhiteSpace(mount.Name) ? def.NameRu : mount.Name,
            mount.Name,
            DefDto(def),
            mount.WoundsCurrent,
            mount.CarriedLoad,
            MountRules.Capacity(def),
            mount.IsActive,
            MountRules.IsOverloaded(def, mount.CarriedLoad),
            MountRules.IsIncapacitated(def, mount.WoundsCurrent),
            mount.Provenance,
            mount.Notes);
    }
}
