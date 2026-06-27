using GenesysForge.Domain.Entities;

namespace GenesysForge.Domain.Rules;

/// <summary>Параметры быстрого детерминированного черновика NPC.</summary>
public readonly record struct NpcDraftRequest(
    GameSystem System,
    NpcKind Kind,
    NpcRole Role,
    NpcPowerLevel PowerLevel,
    CharacteristicType? PrimaryCharacteristic,
    NpcCombatStyle CombatStyle,
    string? Name);

/// <summary>
/// Детерминированный генератор черновиков NPC (без LLM). По роли, типу и уровню силы
/// собирает разумную заготовку статблока, которую мастер затем правит вручную.
/// </summary>
public static class NpcDraftGenerator
{
    public static Npc Generate(Guid ownerUserId, NpcDraftRequest req)
    {
        var level = (int)req.PowerLevel; // 0..3

        var c = BaseCharacteristics(req.Role);
        // Уровень силы поднимает основную характеристику роли; «элита» — ещё и вторичную.
        var primary = req.PrimaryCharacteristic ?? PrimaryOf(req.Role);
        Bump(c, primary, level switch { 0 => -1, 1 => 0, 2 => 1, _ => 2 });
        if (req.PowerLevel == NpcPowerLevel.Elite)
            Bump(c, SecondaryOf(req.Role), 1);

        Clamp(c);

        var npc = new Npc
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            System = req.System,
            Name = string.IsNullOrWhiteSpace(req.Name) ? DefaultName(req.Role, req.Kind) : req.Name!.Trim(),
            Kind = req.Kind,
            Role = req.Role,
            Brawn = c[CharacteristicType.Brawn],
            Agility = c[CharacteristicType.Agility],
            Intellect = c[CharacteristicType.Intellect],
            Cunning = c[CharacteristicType.Cunning],
            Willpower = c[CharacteristicType.Willpower],
            Presence = c[CharacteristicType.Presence],
            Tags = [RoleTag(req.Role)],
        };

        // Базовое поглощение — только от Мощи (+ природная броня у чудовищ). Доспех как
        // отдельный предмет снаряжения добавляет обработчик черновика и поднимает Soak на свой бонус.
        var naturalSoak = req.Role == NpcRole.Monster ? 1 + level / 2 : 0;
        npc.Soak = npc.Brawn + naturalSoak;

        // Защита: Skirmisher подвижнее, «элита» получает +1.
        var def = (req.Role == NpcRole.Skirmisher ? 1 : 0) + (req.PowerLevel == NpcPowerLevel.Elite ? 1 : 0);
        npc.MeleeDefense = def;
        npc.RangedDefense = def;

        // Wound/Strain threshold по формулам правил создания adversary (характеристики уже подняты уровнем).
        npc.WoundThreshold = req.Kind switch
        {
            NpcKind.Minion => 3 + level,        // 3..6 для группы миньонов
            NpcKind.Rival => 8 + npc.Brawn,     // JSON: 8 + Brawn
            _ => 12 + npc.Brawn,                // Nemesis: 12 + Brawn
        };
        npc.StrainThreshold = req.Kind switch
        {
            NpcKind.Nemesis => 10 + npc.Willpower, // JSON: 10 + Willpower
            _ => null,                              // Minion и Rival — без усталости (считается ранами)
        };

        // Крупные монстры (силуэт ≥ 2): запас ран не ниже силуэт×10.
        if (req.Role == NpcRole.Monster && level >= 2)
        {
            npc.Silhouette = 2;
            npc.WoundThreshold = Math.Max(npc.WoundThreshold, npc.Silhouette * 10);
        }

        // Основной навык по боевому стилю. Миньон использует групповые навыки без рангов (ранг = размер−1
        // считается за столом); остальные растут с уровнем.
        var ranks = req.Kind == NpcKind.Minion ? 0 : Math.Min(5, 1 + level);
        npc.Skills.Add(new NpcSkill { NpcId = npc.Id, Name = SkillFor(req.CombatStyle), Ranks = ranks });
        if (req.Role is NpcRole.Leader or NpcRole.Social)
            npc.Skills.Add(new NpcSkill { NpcId = npc.Id, Name = "Лидерство", Ranks = ranks });

        // Оружие; для чисто социального стиля оружие не добавляем.
        var weapon = WeaponFor(req.CombatStyle);
        if (weapon != "—")
            npc.Equipment.Add(weapon);

        return npc;
    }

    private static Dictionary<CharacteristicType, int> BaseCharacteristics(NpcRole role) => role switch
    {
        NpcRole.Brute => Make(4, 2, 2, 2, 2, 2),
        NpcRole.Skirmisher => Make(2, 4, 2, 3, 2, 2),
        NpcRole.Archer => Make(2, 4, 2, 2, 2, 2),
        NpcRole.Caster => Make(2, 2, 3, 2, 4, 2),
        NpcRole.Leader => Make(2, 2, 3, 2, 3, 3),
        NpcRole.Social => Make(1, 2, 3, 3, 2, 4),
        NpcRole.Support => Make(2, 2, 3, 2, 3, 3),
        NpcRole.Monster => Make(4, 3, 1, 2, 2, 1),
        _ => Make(2, 2, 2, 2, 2, 2),
    };

    public static CharacteristicType PrimaryOf(NpcRole role) => role switch
    {
        NpcRole.Brute or NpcRole.Monster => CharacteristicType.Brawn,
        NpcRole.Skirmisher or NpcRole.Archer => CharacteristicType.Agility,
        NpcRole.Caster or NpcRole.Support => CharacteristicType.Willpower,
        NpcRole.Leader or NpcRole.Social => CharacteristicType.Presence,
        _ => CharacteristicType.Brawn,
    };

    public static CharacteristicType SecondaryOf(NpcRole role) => role switch
    {
        NpcRole.Brute or NpcRole.Monster => CharacteristicType.Agility,
        NpcRole.Skirmisher => CharacteristicType.Cunning,
        NpcRole.Archer => CharacteristicType.Cunning,
        NpcRole.Caster => CharacteristicType.Intellect,
        NpcRole.Support => CharacteristicType.Intellect,
        NpcRole.Leader or NpcRole.Social => CharacteristicType.Cunning,
        _ => CharacteristicType.Agility,
    };

    private static string SkillFor(NpcCombatStyle style) => style switch
    {
        NpcCombatStyle.Melee => "Ближний бой",
        NpcCombatStyle.Ranged => "Дальний бой",
        NpcCombatStyle.Magic => "Магия",
        _ => "Обаяние",
    };

    private static string WeaponFor(NpcCombatStyle style) => style switch
    {
        NpcCombatStyle.Melee => "Меч (Ближний; урон +3; крит 3)",
        NpcCombatStyle.Ranged => "Лук (Дальний; урон 7; крит 3)",
        NpcCombatStyle.Magic => "Посох (Ближний; урон +1)",
        _ => "—",
    };

    private static string RoleTag(NpcRole role) => role switch
    {
        NpcRole.Brute => "громила",
        NpcRole.Skirmisher => "застрельщик",
        NpcRole.Archer => "стрелок",
        NpcRole.Caster => "маг",
        NpcRole.Leader => "командир",
        NpcRole.Social => "интриган",
        NpcRole.Support => "поддержка",
        NpcRole.Monster => "монстр",
        _ => "npc",
    };

    private static string DefaultName(NpcRole role, NpcKind kind)
    {
        var roleName = role switch
        {
            NpcRole.Brute => "Громила",
            NpcRole.Skirmisher => "Застрельщик",
            NpcRole.Archer => "Стрелок",
            NpcRole.Caster => "Маг",
            NpcRole.Leader => "Командир",
            NpcRole.Social => "Интриган",
            NpcRole.Support => "Целитель",
            NpcRole.Monster => "Чудовище",
            _ => "NPC",
        };
        var kindName = kind switch
        {
            NpcKind.Minion => "(миньон)",
            NpcKind.Rival => "(ривал)",
            _ => "(немезида)",
        };
        return $"{roleName} {kindName}";
    }

    private static Dictionary<CharacteristicType, int> Make(int b, int a, int i, int c, int w, int p) => new()
    {
        [CharacteristicType.Brawn] = b,
        [CharacteristicType.Agility] = a,
        [CharacteristicType.Intellect] = i,
        [CharacteristicType.Cunning] = c,
        [CharacteristicType.Willpower] = w,
        [CharacteristicType.Presence] = p,
    };

    private static void Bump(Dictionary<CharacteristicType, int> c, CharacteristicType t, int delta) => c[t] += delta;

    private static void Clamp(Dictionary<CharacteristicType, int> c)
    {
        foreach (var key in c.Keys.ToList())
            c[key] = Math.Clamp(c[key], 1, 6);
    }
}
