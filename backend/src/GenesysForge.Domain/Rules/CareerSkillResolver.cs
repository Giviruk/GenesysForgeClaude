namespace GenesysForge.Domain.Rules;

/// <summary>Откуда навык получил карьерный статус.</summary>
public enum CareerSkillGrantSource
{
    /// <summary>Базовый список навыков карьеры.</summary>
    Career = 0,
    /// <summary>Стартовый навык вида с признаком <c>GrantsCareerSkill</c>.</summary>
    Species = 1,
    /// <summary>Талант, выдающий карьерный навык.</summary>
    Talent = 2,
}

/// <summary>Одна выдача карьерного статуса: навык, тип источника и отображаемое имя источника.</summary>
/// <param name="SkillName">Каноническое (английское) имя навыка — совпадает с <c>SkillDef.Name</c>.</param>
/// <param name="Source">Тип источника.</param>
/// <param name="SourceName">Имя карьеры, вида или таланта — для объяснения в UI.</param>
public sealed record CareerSkillGrant(string SkillName, CareerSkillGrantSource Source, string SourceName);

/// <summary>
/// Результат резолвинга карьерных навыков: множество <c>SkillDefId</c> и все выдачи по каждому из них.
/// Дедупликация выполняется по стабильному <c>SkillDefId</c>, а не по отображаемому имени.
/// </summary>
public sealed class CareerSkillResolution
{
    private static readonly IReadOnlyList<CareerSkillGrant> NoGrants = [];

    private readonly Dictionary<Guid, List<CareerSkillGrant>> _grants;

    internal CareerSkillResolution(
        Dictionary<Guid, List<CareerSkillGrant>> grants,
        IReadOnlyList<string> unresolvedSkillNames)
    {
        _grants = grants;
        UnresolvedSkillNames = unresolvedSkillNames;
    }

    /// <summary>Все навыки, являющиеся карьерными для персонажа.</summary>
    public IReadOnlyCollection<Guid> SkillDefIds => _grants.Keys;

    /// <summary>
    /// Имена выдач, для которых не нашлось <c>SkillDef</c> в системе персонажа.
    /// Такие выдачи не дают карьерный статус и не должны молча теряться.
    /// </summary>
    public IReadOnlyList<string> UnresolvedSkillNames { get; }

    public bool IsCareer(Guid skillDefId) => _grants.ContainsKey(skillDefId);

    public IReadOnlyList<CareerSkillGrant> GrantsFor(Guid skillDefId) =>
        _grants.TryGetValue(skillDefId, out var list) ? list : NoGrants;
}

/// <summary>
/// Единый резолвер карьерных навыков: базовые навыки карьеры ∪ выдачи вида ∪ выдачи талантов.
/// Это единственный источник истины о карьерном статусе; хранимый флаг <c>CharacterSkill.IsCareer</c>
/// остаётся только кэшем и не может противоречить резолверу.
/// </summary>
public static class CareerSkillResolver
{
    /// <param name="grants">Все выдачи карьерного статуса из всех источников; порядок сохраняется.</param>
    /// <param name="resolveSkillId">
    /// Отображение канонического имени навыка в стабильный <c>SkillDefId</c>; <c>null</c>, если навыка
    /// нет в системе персонажа.
    /// </param>
    public static CareerSkillResolution Resolve(
        IEnumerable<CareerSkillGrant> grants,
        Func<string, Guid?> resolveSkillId)
    {
        ArgumentNullException.ThrowIfNull(grants);
        ArgumentNullException.ThrowIfNull(resolveSkillId);

        var byId = new Dictionary<Guid, List<CareerSkillGrant>>();
        var unresolved = new List<string>();

        foreach (var grant in grants)
        {
            if (string.IsNullOrWhiteSpace(grant.SkillName)) continue;

            var id = resolveSkillId(grant.SkillName);
            if (id is null)
            {
                if (!unresolved.Contains(grant.SkillName, StringComparer.Ordinal))
                    unresolved.Add(grant.SkillName);
                continue;
            }

            if (!byId.TryGetValue(id.Value, out var list))
                byId[id.Value] = list = [];
            // Дубль одного и того же источника не добавляется повторно: совпадение вида и карьеры
            // не должно выглядеть как две разные выдачи.
            if (!list.Any(g => g.Source == grant.Source && g.SourceName == grant.SourceName))
                list.Add(grant);
        }

        return new CareerSkillResolution(byId, unresolved);
    }
}
