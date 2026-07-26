using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Application.Common;

/// <summary>
/// Сборка входа для <see cref="CareerSkillResolver"/> из доменных сущностей.
/// Единственная точка, где известно, какие источники дают карьерный статус.
/// </summary>
public static class CareerSkills
{
    /// <summary>Все выдачи карьерного статуса: карьера, вид, таланты персонажа.</summary>
    public static IEnumerable<CareerSkillGrant> GrantsFor(
        CareerDef career, ArchetypeDef? archetype, IEnumerable<TalentDef> ownedTalents)
    {
        foreach (var name in career.CareerSkillNames)
            yield return new CareerSkillGrant(name, CareerSkillGrantSource.Career, career.Name);

        foreach (var ss in archetype?.StartingSkills ?? [])
        {
            if (!ss.GrantsCareerSkill || string.IsNullOrWhiteSpace(ss.SkillName)) continue;
            yield return new CareerSkillGrant(ss.SkillName, CareerSkillGrantSource.Species, archetype!.Name);
        }

        foreach (var talent in ownedTalents)
        foreach (var name in talent.CareerSkillNames)
            yield return new CareerSkillGrant(name, CareerSkillGrantSource.Talent, talent.Name);
    }

    /// <summary>
    /// Резолвит карьерные навыки персонажа по загруженным навыкам системы.
    /// Встроенный <c>SkillDef</c> приоритетнее одноимённого пользовательского.
    /// </summary>
    public static CareerSkillResolution Resolve(
        Character character, CareerDef career, IReadOnlyCollection<SkillDef> systemSkills)
    {
        var byName = BuildNameIndex(systemSkills);
        var talents = character.Talents
            .Select(t => t.TalentDef)
            .Where(t => t is not null)
            .Select(t => t!);
        return CareerSkillResolver.Resolve(
            GrantsFor(career, character.Archetype, talents),
            name => byName.TryGetValue(name, out var def) ? def.Id : null);
    }

    /// <summary>Индекс «имя навыка → определение»; встроенный побеждает одноимённый custom.</summary>
    public static Dictionary<string, SkillDef> BuildNameIndex(IEnumerable<SkillDef> systemSkills) =>
        systemSkills
            .OrderBy(s => s.OwnerUserId == null ? 0 : 1)
            .GroupBy(s => s.Name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
}
