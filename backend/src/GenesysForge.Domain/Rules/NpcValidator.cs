using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Валидация статблока NPC по правилам Genesys (см. спецификацию §9).</summary>
public static class NpcValidator
{
    public static void Validate(Npc npc)
    {
        if (string.IsNullOrWhiteSpace(npc.Name))
            throw new DomainRuleException("Имя NPC обязательно.");

        foreach (var (label, value) in Characteristics(npc))
            if (value is < 1 or > 6)
                throw new DomainRuleException($"Характеристика «{label}» должна быть от 1 до 6.");

        if (npc.WoundThreshold <= 0)
            throw new DomainRuleException("Порог ран должен быть больше 0.");
        if (npc.Soak < 0)
            throw new DomainRuleException("Поглощение не может быть отрицательным.");
        if (npc.MeleeDefense < 0 || npc.RangedDefense < 0)
            throw new DomainRuleException("Защита не может быть отрицательной.");
        if (npc.StrainThreshold is < 0)
            throw new DomainRuleException("Порог усталости не может быть отрицательным.");

        foreach (var skill in npc.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
                throw new DomainRuleException("Навык без названия недопустим.");
            if (skill.Ranks is < 0 or > 5)
                throw new DomainRuleException($"Ранги навыка «{skill.Name}» должны быть от 0 до 5.");
        }

        // Nemesis обязан иметь порог усталости.
        if (npc.Kind == NpcKind.Nemesis && npc.StrainThreshold is null or <= 0)
            throw new DomainRuleException("Немезида должна иметь порог усталости.");
    }

    private static IEnumerable<(string Label, int Value)> Characteristics(Npc npc) =>
    [
        ("Сила", npc.Brawn),
        ("Ловкость", npc.Agility),
        ("Интеллект", npc.Intellect),
        ("Хитрость", npc.Cunning),
        ("Воля", npc.Willpower),
        ("Харизма", npc.Presence),
    ];
}
