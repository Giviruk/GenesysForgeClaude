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
    string? Name,
    CreatureTemplate Template = CreatureTemplate.None,
    string? MagicSkill = null,
    string? Environment = null);

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
        // считается за столом); остальные растут с уровнем. Для магии — заданная магшкола (или общий навык).
        var ranks = req.Kind == NpcKind.Minion ? 0 : Math.Min(5, 1 + level);
        var primarySkill = req.CombatStyle == NpcCombatStyle.Magic && !string.IsNullOrWhiteSpace(req.MagicSkill)
            ? req.MagicSkill!.Trim()
            : SkillFor(req.CombatStyle);
        npc.Skills.Add(new NpcSkill { NpcId = npc.Id, Name = primarySkill, Ranks = ranks });
        if (req.Role is NpcRole.Leader or NpcRole.Social)
            npc.Skills.Add(new NpcSkill { NpcId = npc.Id, Name = "Лидерство", Ranks = ranks });

        // Маг получает способность «Заклинания» (магический NPC должен иметь действие — см. NpcValidator).
        if (req.CombatStyle == NpcCombatStyle.Magic)
            npc.Abilities.Add(new NpcAbility
            {
                NpcId = npc.Id, Name = "Заклинания",
                Description = $"Творит заклинания навыком «{primarySkill}».",
            });

        // Таланты и сигнатурная способность по правилам создания adversary: соперник/главарь получают
        // талант Adversary и роль-специфичные таланты, главарь — ещё и сигнатурное действие «каждый ход».
        ApplyTalentsAndSignature(npc, req, level);

        // Тип существа (нежить/зверь/дракон/…): теги, способности, природные атаки, terror/иммунитеты.
        ApplyTemplate(npc, req.Template, level);

        // Оружие гуманоида (если нет шаблона существа с природной атакой); социальный стиль — без оружия.
        if (req.Template == CreatureTemplate.None)
        {
            var weapon = WeaponFor(req.CombatStyle);
            if (weapon != "—")
                npc.Equipment.Add(weapon);
        }

        if (!string.IsNullOrWhiteSpace(req.Environment))
            npc.Tags.Add(req.Environment!.Trim());

        return npc;
    }

    /// <summary>
    /// Применяет шаблон типа существа: добавляет тег, тематические способности, природную структурную
    /// атаку и корректирует поглощение/силуэт. Чистая логика; каталожное оружие к таким NPC не применяется.
    /// Идемпотентно: если тег существа уже есть, шаблон считается применённым и повторно не накладывается
    /// (используется и генератором, и ручным режимом «Применить шаблон»).
    /// </summary>
    public static void ApplyTemplate(Npc npc, CreatureTemplate template, int level)
    {
        if (template == CreatureTemplate.None) return;
        if (npc.Tags.Contains(CreatureTag(template))) return; // уже применён

        npc.Tags.Add(CreatureTag(template));

        switch (template)
        {
            case CreatureTemplate.Undead:
                Ability(npc, "Ужас", "Проверка страха при встрече (см. правила Ужаса).");
                Ability(npc, "Природа нежити", "Не дышит, иммунен к ядам и усталости; не получает стрейн.");
                NaturalAttack(npc, "Когти", damage: $"+{1 + level / 2}", crit: "4");
                break;
            case CreatureTemplate.Beast:
                Ability(npc, "Природное оружие", "Атакует когтями и клыками без оружия.");
                NaturalAttack(npc, "Клыки и когти", damage: $"+{1 + level / 2}", crit: "3");
                break;
            case CreatureTemplate.Dragon:
                npc.Silhouette = Math.Max(npc.Silhouette, level >= 2 ? 3 : 2);
                npc.WoundThreshold = Math.Max(npc.WoundThreshold, npc.Silhouette * 10);
                npc.Soak += 2;
                Ability(npc, "Ужас", "Проверка страха при встрече.");
                NaturalAttack(npc, "Когти и хвост", damage: $"+{2 + level / 2}", crit: "3");
                NaturalAttack(npc, "Огненное дыхание", damage: $"{8 + 2 * level}", crit: "2",
                    range: "Короткая", notes: "Площадь; качество «Взрывное».");
                break;
            case CreatureTemplate.Demon:
                npc.Soak += 1;
                Ability(npc, "Ужас", "Проверка страха при встрече.");
                Ability(npc, "Магическое сопротивление", "Повышает сложность нацеленной на него магии.");
                NaturalAttack(npc, "Когти", damage: $"+{2 + level / 2}", crit: "3");
                break;
            case CreatureTemplate.Construct:
                npc.Soak += 2;
                Ability(npc, "Иммунитеты конструкта", "Иммунен к яду, усталости и страху.");
                NaturalAttack(npc, "Сокрушающий удар", damage: $"+{2 + level / 2}", crit: "4");
                break;
        }
    }

    /// <summary>
    /// Выдаёт таланты и сигнатурную способность по типу/роли/уровню (правила создания adversary):
    /// <list type="bullet">
    ///   <item>Adversary: у главаря 1–2 (по уровню), у боевого соперника — 1; миньоны без талантов.</item>
    ///   <item>Роль-специфичные таланты: соперник (Strong/Elite) — 1, главарь — 1–2.</item>
    ///   <item>Сигнатурная способность главаря — чтобы он «делал что-то интересное каждый ход».</item>
    /// </list>
    /// Талант Adversary записывается строкой «Adversary N» (единый формат с бестиарием).
    /// </summary>
    private static void ApplyTalentsAndSignature(Npc npc, NpcDraftRequest req, int level)
    {
        var adversary = req.Kind switch
        {
            NpcKind.Nemesis => level >= 2 ? 2 : 1,     // 1–2 (3+ только для эпических — вручную)
            NpcKind.Rival => IsCombat(req) ? 1 : 0,    // Adversary 1 для опасного боевого соперника
            _ => 0,                                     // миньоны — без талантов
        };
        if (adversary > 0)
            npc.Talents.Add($"Adversary {adversary}");

        var roleTalentCount = req.Kind switch
        {
            NpcKind.Nemesis => level >= 2 ? 2 : 1,
            NpcKind.Rival => level >= 2 ? 1 : 0,
            _ => 0,
        };
        foreach (var talent in RoleTalents(req.Role).Take(roleTalentCount))
            if (!npc.Talents.Contains(talent))
                npc.Talents.Add(talent);

        // Сигнатурное действие главаря (у соперника/миньона его нет — они проще).
        if (req.Kind == NpcKind.Nemesis)
        {
            var (name, desc) = SignatureAbility(req.Role);
            if (npc.Abilities.All(a => a.Name != name))
                Ability(npc, name, desc);
        }
    }

    /// <summary>Боевой NPC: боевой стиль (не социальный) и не чисто социальная роль.</summary>
    private static bool IsCombat(NpcDraftRequest req) =>
        req.CombatStyle != NpcCombatStyle.Social && req.Role != NpcRole.Social;

    /// <summary>Приоритетные роль-специфичные таланты (берётся нужное число сверху). Формат — имя-строка.</summary>
    private static IReadOnlyList<string> RoleTalents(NpcRole role) => role switch
    {
        NpcRole.Brute => ["Яростная атака", "Выносливость"],
        NpcRole.Skirmisher => ["Быстрый удар", "Уклонение"],
        NpcRole.Archer => ["Точный прицел", "Стрельба в упор"],
        NpcRole.Caster => ["Могущественная магия", "Железная воля"],
        NpcRole.Leader => ["Полевой командир", "Согласованная атака"],
        NpcRole.Social => ["Меня не проведёшь", "Прирождённый лжец"],
        NpcRole.Support => ["Опытный целитель", "Воодушевление"],
        NpcRole.Monster => ["Свирепая сила", "Смертельные удары"],
        _ => [],
    };

    /// <summary>Сигнатурное действие главаря по роли — «что-то интересное каждый ход».</summary>
    private static (string Name, string Desc) SignatureAbility(NpcRole role) => role switch
    {
        NpcRole.Brute => ("Сокрушительный натиск",
            "Один раз за ход при попадании в ближнем бою наносит +1 урон за каждый успех сверх первого."),
        NpcRole.Skirmisher => ("Стремительный выпад",
            "Один раз за встречу совершает манёвр перемещения и атаку ближнего боя как одно действие."),
        NpcRole.Archer => ("Прицельный выстрел",
            "Один раз за ход тратит ⬥⬥ преимущество для автоматической критической травмы при дальней атаке."),
        NpcRole.Caster => ("Всплеск силы",
            "Один раз за встречу творит заклинание без увеличения сложности за дополнительные эффекты."),
        NpcRole.Leader => ("Тактический приказ",
            "Действием даёт союзнику в пределах средней дистанции немедленный бесплатный манёвр."),
        NpcRole.Social => ("Манипулятор",
            "Действием проводит противоборствующую проверку Обмана, чтобы навязать цели ложное убеждение до конца сцены."),
        NpcRole.Support => ("Экстренная помощь",
            "Действием восстанавливает союзнику раны, равные значению Воли NPC; один раз за встречу."),
        NpcRole.Monster => ("Дикая ярость",
            "Один раз за ход, будучи раненым, добавляет ▲ к своей следующей атаке ближнего боя."),
        _ => ("Инициатива главаря",
            "Один раз за встречу совершает дополнительный манёвр в свой ход."),
    };

    private static void Ability(Npc npc, string name, string description) =>
        npc.Abilities.Add(new NpcAbility { NpcId = npc.Id, Name = name, Description = description });

    /// <summary>Природная атака существа (структурная). Навык совпадает с основным боевым навыком NPC.</summary>
    private static void NaturalAttack(Npc npc, string name, string damage, string crit,
        string range = "Вплотную", string notes = "") =>
        npc.Attacks.Add(new NpcAttack
        {
            NpcId = npc.Id, Name = name,
            SkillName = npc.Skills.Count > 0 ? npc.Skills[0].Name : "Ближний бой",
            Damage = damage, Critical = crit, RangeBand = range, Notes = notes,
        });

    private static string CreatureTag(CreatureTemplate t) => t switch
    {
        CreatureTemplate.Undead => "нежить",
        CreatureTemplate.Beast => "зверь",
        CreatureTemplate.Dragon => "дракон",
        CreatureTemplate.Demon => "демон",
        CreatureTemplate.Construct => "конструкт",
        _ => "существо",
    };

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
