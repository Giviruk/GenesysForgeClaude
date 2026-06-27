using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>
/// Валидация статблока NPC по правилам Genesys / создания adversary (см. спецификацию §9 и
/// _books/_npc/genesys_adversary_creation_rules_for_claude.json). Возвращает errors (блокируют) и
/// warnings (рекомендации, не блокируют).
/// </summary>
public static class NpcValidator
{
    /// <summary>Защита adversary считается чрезмерной начиная с warning-порога; выше hard-порога — error.</summary>
    private const int DefenseWarn = 4;
    private const int DefenseError = 6;
    private const int SoakWarn = 7;
    private const int SkillsWarn = 8;

    /// <summary>Известные магические навыки (подстроки, RU/EN) для проверки магических NPC.</summary>
    private static readonly string[] MagicSkillMarkers =
        ["магия", "arcana", "аркан", "divine", "божеств", "primal", "природ", "runes", "руны", "verse", "стих"];

    /// <summary>Полная валидация: собирает errors и warnings, ничего не бросает.</summary>
    public static NpcValidationResult Validate(Npc npc)
    {
        var r = new NpcValidationResult();

        if (string.IsNullOrWhiteSpace(npc.Name))
            r.Error("Имя NPC обязательно.");

        foreach (var (label, value) in Characteristics(npc))
            if (value is < 1 or > 6)
                r.Error($"Характеристика «{label}» должна быть от 1 до 6.");

        if (npc.WoundThreshold <= 0)
            r.Error("Порог ран должен быть больше 0.");
        if (npc.Soak < 0)
            r.Error("Поглощение не может быть отрицательным.");
        if (npc.MeleeDefense < 0 || npc.RangedDefense < 0)
            r.Error("Защита не может быть отрицательной.");
        if (npc.StrainThreshold is < 0)
            r.Error("Порог усталости не может быть отрицательным.");
        if (npc.Silhouette < 0)
            r.Error("Силуэт не может быть отрицательным.");

        ValidateByKind(npc, r);
        ValidateSkills(npc, r);
        ValidateAttacks(npc, r);
        ValidateDerivedGuidance(npc, r);
        ValidateMagic(npc, r);

        return r;
    }

    /// <summary>Бросает <see cref="DomainRuleException"/> при наличии errors (для хендлеров сохранения).</summary>
    public static NpcValidationResult ValidateAndThrow(Npc npc)
    {
        var r = Validate(npc);
        if (!r.IsValid)
            throw new DomainRuleException(string.Join(" ", r.Errors));
        return r;
    }

    private static void ValidateByKind(Npc npc, NpcValidationResult r)
    {
        switch (npc.Kind)
        {
            case NpcKind.Minion:
                // Миньон не имеет усталости и индивидуальных рангов (использует групповые навыки).
                if (npc.StrainThreshold is not null)
                    r.Error("Миньон не имеет порога усталости.");
                if (npc.Skills.Any(s => s.Ranks > 0))
                    r.Error("Миньон использует групповые навыки без индивидуальных рангов (ранг = размер группы − 1).");
                break;
            case NpcKind.Rival:
                // Соперник обычно без усталости (усталость считается ранами).
                if (npc.StrainThreshold is not null)
                    r.Warn("Соперник обычно не имеет порога усталости — усталость считается ранами.");
                break;
            case NpcKind.Nemesis:
                if (npc.StrainThreshold is null or <= 0)
                    r.Error("Главарь (Nemesis) должен иметь порог усталости.");
                break;
        }
    }

    private static void ValidateSkills(Npc npc, NpcValidationResult r)
    {
        foreach (var skill in npc.Skills)
        {
            if (string.IsNullOrWhiteSpace(skill.Name))
                r.Error("Навык без названия недопустим.");
            if (skill.Ranks is < 0 or > 5)
                r.Error($"Ранги навыка «{skill.Name}» должны быть от 0 до 5.");
        }

        if (npc.Skills.Count > SkillsWarn)
            r.Warn($"Слишком много навыков ({npc.Skills.Count}); оставьте до {SkillsWarn} значимых для стола.");
    }

    private static void ValidateAttacks(Npc npc, NpcValidationResult r)
    {
        foreach (var a in npc.Attacks)
        {
            var label = string.IsNullOrWhiteSpace(a.Name) ? "без названия" : a.Name;
            if (string.IsNullOrWhiteSpace(a.SkillName))
                r.Error($"Атака «{label}»: укажите навык броска.");
            if (string.IsNullOrWhiteSpace(a.Damage))
                r.Error($"Атака «{label}»: укажите урон.");
            if (string.IsNullOrWhiteSpace(a.RangeBand))
                r.Error($"Атака «{label}»: укажите дистанцию.");
            if (!string.IsNullOrWhiteSpace(a.Critical) && !(int.TryParse(a.Critical, out var crit) && crit >= 1))
                r.Error($"Атака «{label}»: крит должен быть положительным числом или пустым.");

            foreach (var q in a.Qualities)
                if (q.QualityDefId is null && !string.IsNullOrWhiteSpace(q.QualityCode))
                    r.Warn($"Атака «{label}»: качество «{(string.IsNullOrWhiteSpace(q.NameRu) ? q.QualityCode : q.NameRu)}» не из справочника (custom).");
        }
    }

    private static void ValidateDerivedGuidance(Npc npc, NpcValidationResult r)
    {
        var maxDef = Math.Max(npc.MeleeDefense, npc.RangedDefense);
        if (maxDef > DefenseError)
            r.Error($"Защита {maxDef} слишком высока — максимум {DefenseError} даже для исключительных NPC.");
        else if (maxDef > DefenseWarn)
            r.Warn($"Защита {maxDef} высока — обычно не выше {DefenseWarn}; используйте осторожно.");

        if (npc.Soak > SoakWarn)
            r.Warn($"Поглощение {npc.Soak} велико — выше {SoakWarn} применяйте осторожно.");

        // Крупные существа: запас ран должен соответствовать размеру.
        if (npc.Silhouette >= 2 && npc.WoundThreshold < npc.Silhouette * 10)
            r.Warn($"Для силуэта {npc.Silhouette} запас ран обычно ≥ {npc.Silhouette * 10}.");
    }

    private static void ValidateMagic(Npc npc, NpcValidationResult r)
    {
        var hasMagicSkill = npc.Skills.Any(s => IsMagicSkill(s.Name))
            || npc.Attacks.Any(a => IsMagicSkill(a.SkillName));
        if (!hasMagicSkill) return;

        // У магического NPC должны быть заклинания/способности или хотя бы магическая атака.
        var hasMagicAction = npc.Abilities.Count > 0 || npc.Attacks.Any(a => IsMagicSkill(a.SkillName));
        if (!hasMagicAction)
            r.Warn("Магический NPC: добавьте заклинания/способности или магическую атаку.");
    }

    private static bool IsMagicSkill(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var n = name.ToLowerInvariant().Replace('ё', 'е');
        return MagicSkillMarkers.Any(m => n.Contains(m));
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
