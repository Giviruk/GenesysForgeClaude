using GenesysForge.Application.Dtos;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Application.Common;

public static class Mappers
{
    public static ArchetypeDto ToDto(this ArchetypeDef a) => new(a.Id, a.Name, a.NameRu, a.Brawn, a.Agility,
        a.Intellect, a.Cunning, a.Willpower, a.Presence, a.WoundBase, a.StrainBase, a.StartingXp,
        a.Description, a.SafeDescription, a.Source);

    public static CareerDto ToDto(this CareerDef c) =>
        new(c.Id, c.Name, c.NameRu, c.Description, c.SafeDescription, c.Source, c.CareerSkillNames);

    public static SkillDefDto ToDto(this SkillDef s) =>
        new(s.Id, s.Name, s.NameRu, s.Characteristic, s.Kind, s.SafeDescription, s.Source, s.OwnerUserId != null);

    public static TalentDefDto ToDto(this TalentDef t) => new(t.Id, t.Name, t.NameRu, t.Tier, t.IsRanked, t.Setting,
        t.Activation, t.Description, t.SafeDescription, t.Source,
        t.WoundBonus, t.StrainBonus, t.SoakBonus, t.MeleeDefenseBonus, t.RangedDefenseBonus, t.OwnerUserId != null);

    public static ItemDefDto ToDto(this ItemDef i) => new(i.Id, i.Name, i.NameRu, i.Kind, i.Encumbrance, i.SoakBonus,
        i.MeleeDefense, i.RangedDefense, i.EncumbranceThresholdBonus,
        i.Description, i.SafeDescription, i.Source, i.Price, i.Rarity, i.OwnerUserId != null);

    public static HeroicAbilityDto ToDto(this HeroicAbilityDef h) =>
        new(h.Id, h.Name, h.NameRu, h.Description, h.SafeDescription, h.Source, h.OwnerUserId != null);
}
