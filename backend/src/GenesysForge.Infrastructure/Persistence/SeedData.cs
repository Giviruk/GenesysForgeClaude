using System.Text;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Встроенный контент систем. Структура и правила соответствуют Genesys CRB и Realms of Terrinoth;
/// набор сокращён, значения можно расширять кастомным контентом через UI.
///
/// Каждая запись несёт content-model: стабильный <c>Code</c>, русское имя <c>NameRu</c>,
/// оригинальное имя <c>Name</c> (NameEn), safe-описание <c>SafeDescription</c> и ссылку на источник <c>Source</c>.
/// Полные (private) описания берутся из <see cref="PrivateContentStore"/> (файлы private-content/*.ru.json).
///
/// Два независимых seed-pipeline выбираются <see cref="ContentMode"/>:
/// <list type="bullet">
/// <item>PrivateFull — полный контент: <c>Description</c> = private-описание (или fallback на safe);</item>
/// <item>PublicSafe — copyright-safe: <c>Description</c> очищается, остаются NameRu + SafeDescription + Source.</item>
/// </list>
/// Наборы не смешиваются: одну БД сеют одним режимом (см. docs/database.md).
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Идемпотентный сид встроенного контента в выбранном <paramref name="mode"/>: добавляет только
    /// отсутствующие записи (по System+Name, для героик — по Name). Безопасно вызывать повторно —
    /// дублей не будет. Кастомный контент (OwnerUserId != null) не затрагивается.
    /// </summary>
    public static void Apply(AppDbContext db, ContentMode mode = ContentMode.PrivateFull)
    {
        var store = mode == ContentMode.PrivateFull ? PrivateContentStore.Load() : null;

        var skills = CoreSkills().Concat(TerrinothSkills()).ToList();
        var archetypes = CoreArchetypes().Concat(TerrinothSpecies()).ToList();
        var careers = CoreCareers().Concat(TerrinothCareers()).ToList();
        var talents = TalentCatalog.Load().ToList();
        var items = ItemCatalog.Load().ToList();
        var heroics = HeroicCatalog.Load().ToList();
        var spells = Spells(GameSystem.GenesysCore).Concat(Spells(GameSystem.RealmsOfTerrinoth)).ToList();

        // Проекция описаний под режим контента — единственное отличие private/public pipeline.
        ProjectContent(skills, mode, store);
        ProjectContent(archetypes, mode, store);
        ProjectContent(careers, mode, store);
        ProjectContent(talents, mode, store);
        ProjectContent(items, mode, store);
        ProjectContent(heroics, mode, store);
        ProjectSpells(spells, mode);

        var added = false;
        added |= SeedMissing(db, db.SkillDefs, skills, d => (d.System, d.Name));
        added |= SeedMissing(db, db.ArchetypeDefs, archetypes, d => (d.System, d.Name));
        added |= SeedMissing(db, db.CareerDefs, careers, d => (d.System, d.Name));
        added |= SeedMissing(db, db.TalentDefs, talents, d => (d.System, d.Name));
        added |= SeedMissing(db, db.ItemDefs, items, d => (d.System, d.Name));
        added |= SeedMissing(db, db.HeroicAbilityDefs, heroics, d => ((GameSystem)0, d.Name));
        added |= SeedMissing(db, db.SpellDefs, spells,
            d => (d.System, $"{d.MagicSkill}:{(int)d.Kind}:{d.ParentEffect}:{d.NameEn}"));

        if (added) db.SaveChanges();
    }

    /// <summary>
    /// Проекция content-model под режим: PublicSafe очищает полное описание (остаётся только safe),
    /// PrivateFull подставляет полное описание из private-набора (или fallback на safe, чтобы было непусто).
    /// </summary>
    private static void ProjectContent(IEnumerable<IContentDef> items, ContentMode mode, PrivateContentStore? store)
    {
        foreach (var item in items)
        {
            if (mode == ContentMode.PublicSafe)
            {
                item.Description = "";
                continue;
            }

            var full = store?.Get(item.Code);
            if (!string.IsNullOrEmpty(full)) item.Description = full;
            else if (string.IsNullOrEmpty(item.Description)) item.Description = item.SafeDescription;
        }
    }

    /// <summary>Заклинания несут полное описание в коде (safe-парафраз из п1); в PublicSafe оно очищается.</summary>
    private static void ProjectSpells(IEnumerable<SpellDef> spells, ContentMode mode)
    {
        if (mode != ContentMode.PublicSafe) return;
        foreach (var s in spells) s.Description = "";
    }

    /// <summary>Добавляет элементы, чьи ключи отсутствуют среди встроенных (OwnerUserId == null) записей.</summary>
    private static bool SeedMissing<T>(
        AppDbContext db,
        DbSet<T> set,
        IEnumerable<T> builtIn,
        Func<T, (GameSystem System, string Name)> key) where T : class
    {
        var existing = set.AsEnumerable()
            .Where(IsBuiltIn)
            .Select(key)
            .ToHashSet();

        var added = false;
        foreach (var def in builtIn)
        {
            if (existing.Add(key(def))) // true — ключа ещё не было
            {
                set.Add(def);
                added = true;
            }
        }
        return added;
    }

    private static bool IsBuiltIn<T>(T entity) => entity switch
    {
        SkillDef s => s.OwnerUserId == null,
        TalentDef t => t.OwnerUserId == null,
        ItemDef i => i.OwnerUserId == null,
        HeroicAbilityDef h => h.OwnerUserId == null,
        SpellDef sp => sp.OwnerUserId == null,
        _ => true, // архетипы и карьеры всегда встроенные
    };

    // ─────────────────────────── content-model helpers ───────────────────────────

    private static string Sys(GameSystem s) => s == GameSystem.GenesysCore ? "gc" : "rot";

    /// <summary>ASCII-slug по оригинальному (английскому) имени для стабильного кода.</summary>
    private static string Slug(string s)
    {
        var sb = new StringBuilder(s.Length);
        var prevDash = false;
        foreach (var ch in s.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch)) { sb.Append(ch); prevDash = false; }
            else if (!prevDash && sb.Length > 0) { sb.Append('-'); prevDash = true; }
        }
        return sb.ToString().Trim('-');
    }

    private static string Code(GameSystem sys, string type, string name) => $"{Sys(sys)}.{type}.{Slug(name)}";

    private static string Ru(IReadOnlyDictionary<string, string> map, string name) =>
        map.TryGetValue(name, out var ru) ? ru : name;

    // ─────────────────────────── skills ───────────────────────────

    private static readonly Dictionary<string, string> SkillRu = new()
    {
        ["Athletics"] = "Атлетика", ["Computers"] = "Компьютеры", ["Cool"] = "Хладнокровие",
        ["Coordination"] = "Координация", ["Discipline"] = "Дисциплина", ["Driving"] = "Вождение",
        ["Mechanics"] = "Механика", ["Medicine"] = "Медицина", ["Operating"] = "Операторство",
        ["Perception"] = "Внимательность", ["Piloting"] = "Пилотирование", ["Resilience"] = "Выносливость",
        ["Riding"] = "Верховая езда", ["Skulduggery"] = "Плутовство", ["Stealth"] = "Скрытность",
        ["Streetwise"] = "Уличная смекалка", ["Survival"] = "Выживание", ["Vigilance"] = "Бдительность",
        ["Brawl"] = "Рукопашный бой", ["Gunnery"] = "Тяжёлое орудие", ["Melee"] = "Ближний бой",
        ["Ranged (Heavy)"] = "Дальний бой (тяжёлый)", ["Ranged (Light)"] = "Дальний бой (лёгкий)",
        ["Charm"] = "Обаяние", ["Coercion"] = "Принуждение", ["Deception"] = "Обман",
        ["Leadership"] = "Лидерство", ["Negotiation"] = "Переговоры", ["Knowledge"] = "Знание",
        ["Arcana"] = "Аркана", ["Divine"] = "Божественное", ["Primal"] = "Первозданное",
        ["Alchemy"] = "Алхимия", ["Melee (Heavy)"] = "Ближний бой (тяжёлый)",
        ["Melee (Light)"] = "Ближний бой (лёгкий)", ["Ranged"] = "Дальний бой",
        ["Knowledge (Adventuring)"] = "Знание (приключения)", ["Knowledge (Forbidden)"] = "Знание (запретное)",
        ["Knowledge (Geography)"] = "Знание (география)", ["Knowledge (Lore)"] = "Знание (предания)",
        ["Runes"] = "Руны", ["Verse"] = "Стих",
    };

    private static SkillDef Skill(GameSystem sys, string name, CharacteristicType ch, SkillKind kind) =>
        new()
        {
            Id = Guid.NewGuid(), System = sys, Code = Code(sys, "skill", name),
            Name = name, NameRu = Ru(SkillRu, name), Characteristic = ch, Kind = kind,
            Source = (sys == GameSystem.GenesysCore ? "Genesys Core Rulebook" : "Realms of Terrinoth") + ", гл. «Навыки»",
        };

    private static IEnumerable<SkillDef> CoreSkills()
    {
        const GameSystem S = GameSystem.GenesysCore;
        return
        [
            Skill(S, "Athletics", CharacteristicType.Brawn, SkillKind.General),
            Skill(S, "Computers", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Cool", CharacteristicType.Presence, SkillKind.General),
            Skill(S, "Coordination", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Discipline", CharacteristicType.Willpower, SkillKind.General),
            Skill(S, "Driving", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Mechanics", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Medicine", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Operating", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Perception", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Piloting", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Resilience", CharacteristicType.Brawn, SkillKind.General),
            Skill(S, "Riding", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Skulduggery", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Stealth", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Streetwise", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Survival", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Vigilance", CharacteristicType.Willpower, SkillKind.General),
            Skill(S, "Brawl", CharacteristicType.Brawn, SkillKind.Combat),
            Skill(S, "Gunnery", CharacteristicType.Agility, SkillKind.Combat),
            Skill(S, "Melee", CharacteristicType.Brawn, SkillKind.Combat),
            Skill(S, "Ranged (Heavy)", CharacteristicType.Agility, SkillKind.Combat),
            Skill(S, "Ranged (Light)", CharacteristicType.Agility, SkillKind.Combat),
            Skill(S, "Charm", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Coercion", CharacteristicType.Willpower, SkillKind.Social),
            Skill(S, "Deception", CharacteristicType.Cunning, SkillKind.Social),
            Skill(S, "Leadership", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Negotiation", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Knowledge", CharacteristicType.Intellect, SkillKind.Knowledge),
            Skill(S, "Arcana", CharacteristicType.Intellect, SkillKind.Magic),
            Skill(S, "Divine", CharacteristicType.Willpower, SkillKind.Magic),
            Skill(S, "Primal", CharacteristicType.Cunning, SkillKind.Magic),
        ];
    }

    private static IEnumerable<SkillDef> TerrinothSkills()
    {
        const GameSystem S = GameSystem.RealmsOfTerrinoth;
        return
        [
            Skill(S, "Alchemy", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Athletics", CharacteristicType.Brawn, SkillKind.General),
            Skill(S, "Cool", CharacteristicType.Presence, SkillKind.General),
            Skill(S, "Coordination", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Discipline", CharacteristicType.Willpower, SkillKind.General),
            Skill(S, "Mechanics", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Medicine", CharacteristicType.Intellect, SkillKind.General),
            Skill(S, "Perception", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Resilience", CharacteristicType.Brawn, SkillKind.General),
            Skill(S, "Riding", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Skulduggery", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Stealth", CharacteristicType.Agility, SkillKind.General),
            Skill(S, "Streetwise", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Survival", CharacteristicType.Cunning, SkillKind.General),
            Skill(S, "Vigilance", CharacteristicType.Willpower, SkillKind.General),
            Skill(S, "Brawl", CharacteristicType.Brawn, SkillKind.Combat),
            Skill(S, "Gunnery", CharacteristicType.Agility, SkillKind.Combat),
            Skill(S, "Melee (Heavy)", CharacteristicType.Brawn, SkillKind.Combat),
            Skill(S, "Melee (Light)", CharacteristicType.Brawn, SkillKind.Combat),
            Skill(S, "Ranged", CharacteristicType.Agility, SkillKind.Combat),
            Skill(S, "Charm", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Coercion", CharacteristicType.Willpower, SkillKind.Social),
            Skill(S, "Deception", CharacteristicType.Cunning, SkillKind.Social),
            Skill(S, "Leadership", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Negotiation", CharacteristicType.Presence, SkillKind.Social),
            Skill(S, "Knowledge (Adventuring)", CharacteristicType.Intellect, SkillKind.Knowledge),
            Skill(S, "Knowledge (Forbidden)", CharacteristicType.Intellect, SkillKind.Knowledge),
            Skill(S, "Knowledge (Geography)", CharacteristicType.Intellect, SkillKind.Knowledge),
            Skill(S, "Knowledge (Lore)", CharacteristicType.Intellect, SkillKind.Knowledge),
            Skill(S, "Arcana", CharacteristicType.Intellect, SkillKind.Magic),
            Skill(S, "Divine", CharacteristicType.Willpower, SkillKind.Magic),
            Skill(S, "Primal", CharacteristicType.Cunning, SkillKind.Magic),
            Skill(S, "Runes", CharacteristicType.Intellect, SkillKind.Magic),
            Skill(S, "Verse", CharacteristicType.Presence, SkillKind.Magic),
        ];
    }

    // ─────────────────────────── archetypes ───────────────────────────

    private static readonly Dictionary<string, string> ArchetypeRu = new()
    {
        ["Average Human"] = "Обычный человек", ["The Laborer"] = "Работяга",
        ["The Intellectual"] = "Интеллектуал", ["The Aristocrat"] = "Аристократ",
        ["Human"] = "Человек", ["Elf (Latari)"] = "Эльф (Латари)", ["Dwarf"] = "Дворф",
        ["Orc"] = "Орк", ["Gnome"] = "Гном", ["Catfolk (Hyrrinx)"] = "Котолюд (Хирринкс)",
    };

    private static ArchetypeDef Arch(GameSystem sys, string name, int br, int ag, int @int, int cun, int will, int pr,
        int wt, int st, int xp, string safe) => new()
    {
        Id = Guid.NewGuid(), System = sys, Code = Code(sys, "archetype", name),
        Name = name, NameRu = Ru(ArchetypeRu, name),
        Brawn = br, Agility = ag, Intellect = @int, Cunning = cun, Willpower = will, Presence = pr,
        WoundBase = wt, StrainBase = st, StartingXp = xp, SafeDescription = safe,
        Source = (sys == GameSystem.GenesysCore ? "Genesys Core Rulebook, гл. «Архетипы»" : "Realms of Terrinoth, гл. «Народы»"),
    };

    private static IEnumerable<ArchetypeDef> CoreArchetypes() =>
    [
        Arch(GameSystem.GenesysCore, "Average Human", 2, 2, 2, 2, 2, 2, 10, 10, 110, "Универсальный архетип без выраженных сильных и слабых сторон."),
        Arch(GameSystem.GenesysCore, "The Laborer", 3, 2, 2, 2, 2, 1, 12, 8, 100, "Сильный и выносливый труженик."),
        Arch(GameSystem.GenesysCore, "The Intellectual", 1, 2, 3, 2, 2, 2, 8, 12, 100, "Учёный и мыслитель."),
        Arch(GameSystem.GenesysCore, "The Aristocrat", 1, 2, 2, 2, 2, 3, 9, 11, 100, "Харизматичный представитель высшего общества."),
    ];

    private static IEnumerable<ArchetypeDef> TerrinothSpecies() =>
    [
        Arch(GameSystem.RealmsOfTerrinoth, "Human", 2, 2, 2, 2, 2, 2, 10, 10, 110, "Люди Терринота — самый многочисленный и разносторонний народ."),
        Arch(GameSystem.RealmsOfTerrinoth, "Elf (Latari)", 2, 3, 2, 2, 2, 1, 9, 11, 100, "Латарийские эльфы — ловкие и долговечные жители лесов."),
        Arch(GameSystem.RealmsOfTerrinoth, "Dwarf", 2, 1, 2, 2, 3, 2, 12, 10, 100, "Дворфы Даннских холмов — стойкие мастера и воины."),
        Arch(GameSystem.RealmsOfTerrinoth, "Orc", 3, 2, 2, 2, 2, 1, 12, 9, 100, "Орки — могучие и свирепые воины Брокенских земель."),
        Arch(GameSystem.RealmsOfTerrinoth, "Gnome", 1, 2, 2, 3, 2, 2, 8, 11, 100, "Гномы-изобретатели — малы ростом, но хитроумны."),
        Arch(GameSystem.RealmsOfTerrinoth, "Catfolk (Hyrrinx)", 2, 3, 2, 2, 1, 2, 10, 9, 100, "Кошачий народ — стремительные охотники."),
    ];

    // ─────────────────────────── careers ───────────────────────────

    private static readonly Dictionary<string, string> CareerRu = new()
    {
        ["Entertainer"] = "Артист", ["Explorer"] = "Исследователь", ["Healer"] = "Целитель",
        ["Leader"] = "Лидер", ["Scoundrel"] = "Пройдоха", ["Socialite"] = "Светский лев",
        ["Soldier"] = "Солдат", ["Tradesperson"] = "Ремесленник", ["Disciple"] = "Послушник",
        ["Envoy"] = "Посланник", ["Mage"] = "Маг", ["Runemaster"] = "Рунный мастер",
        ["Primalist"] = "Первозданник", ["Scholar"] = "Учёный", ["Scout"] = "Разведчик",
        ["Warrior"] = "Воин",
    };

    private static CareerDef Career(GameSystem sys, string name, string safe, params string[] skills) =>
        new()
        {
            Id = Guid.NewGuid(), System = sys, Code = Code(sys, "career", name),
            Name = name, NameRu = Ru(CareerRu, name), SafeDescription = safe, CareerSkillNames = [.. skills],
            Source = (sys == GameSystem.GenesysCore ? "Genesys Core Rulebook, гл. «Карьеры»" : "Realms of Terrinoth, гл. «Карьеры»"),
        };

    private static IEnumerable<CareerDef> CoreCareers() =>
    [
        Career(GameSystem.GenesysCore, "Entertainer", "Артист и душа компании.",
            "Charm", "Coordination", "Deception", "Discipline", "Leadership", "Melee", "Skulduggery", "Stealth"),
        Career(GameSystem.GenesysCore, "Explorer", "Первопроходец и искатель приключений.",
            "Athletics", "Brawl", "Perception", "Piloting", "Ranged (Light)", "Resilience", "Streetwise", "Survival"),
        Career(GameSystem.GenesysCore, "Healer", "Врач и спаситель жизней.",
            "Brawl", "Cool", "Discipline", "Knowledge", "Medicine", "Resilience", "Survival", "Vigilance"),
        Career(GameSystem.GenesysCore, "Leader", "Командир и вдохновитель.",
            "Charm", "Coercion", "Cool", "Discipline", "Leadership", "Melee", "Negotiation", "Perception"),
        Career(GameSystem.GenesysCore, "Scoundrel", "Плут, живущий на грани закона.",
            "Charm", "Cool", "Coordination", "Deception", "Perception", "Ranged (Light)", "Skulduggery", "Streetwise"),
        Career(GameSystem.GenesysCore, "Socialite", "Мастер светских интриг.",
            "Charm", "Cool", "Deception", "Knowledge", "Negotiation", "Perception", "Streetwise", "Vigilance"),
        Career(GameSystem.GenesysCore, "Soldier", "Профессиональный боец.",
            "Athletics", "Brawl", "Gunnery", "Melee", "Perception", "Ranged (Heavy)", "Resilience", "Vigilance"),
        Career(GameSystem.GenesysCore, "Tradesperson", "Ремесленник и мастер своего дела.",
            "Athletics", "Discipline", "Mechanics", "Negotiation", "Perception", "Resilience", "Streetwise", "Vigilance"),
    ];

    private static IEnumerable<CareerDef> TerrinothCareers() =>
    [
        Career(GameSystem.RealmsOfTerrinoth, "Disciple", "Служитель божества, несущий веру и исцеление.",
            "Charm", "Coercion", "Discipline", "Divine", "Knowledge (Lore)", "Leadership", "Medicine", "Melee (Light)"),
        Career(GameSystem.RealmsOfTerrinoth, "Envoy", "Дипломат и посредник.",
            "Charm", "Cool", "Deception", "Discipline", "Knowledge (Geography)", "Leadership", "Negotiation", "Streetwise"),
        Career(GameSystem.RealmsOfTerrinoth, "Mage", "Заклинатель, постигший тайны арканы.",
            "Arcana", "Cool", "Discipline", "Knowledge (Forbidden)", "Knowledge (Lore)", "Melee (Light)", "Perception", "Vigilance"),
        Career(GameSystem.RealmsOfTerrinoth, "Runemaster", "Мастер рун — вариант мага, черпающий силу из рунных осколков.",
            "Runes", "Cool", "Discipline", "Knowledge (Forbidden)", "Knowledge (Lore)", "Melee (Heavy)", "Perception", "Resilience"),
        Career(GameSystem.RealmsOfTerrinoth, "Primalist", "Заклинатель первозданной магии природы.",
            "Primal", "Brawl", "Coercion", "Medicine", "Perception", "Resilience", "Survival", "Vigilance"),
        Career(GameSystem.RealmsOfTerrinoth, "Scholar", "Учёный, искатель знаний.",
            "Alchemy", "Discipline", "Knowledge (Adventuring)", "Knowledge (Forbidden)", "Knowledge (Geography)", "Knowledge (Lore)", "Medicine", "Perception"),
        Career(GameSystem.RealmsOfTerrinoth, "Scoundrel", "Вор, контрабандист и авантюрист.",
            "Charm", "Cool", "Coordination", "Deception", "Ranged", "Skulduggery", "Stealth", "Streetwise"),
        Career(GameSystem.RealmsOfTerrinoth, "Scout", "Следопыт и разведчик диких земель.",
            "Athletics", "Brawl", "Coordination", "Knowledge (Geography)", "Perception", "Ranged", "Stealth", "Survival"),
        Career(GameSystem.RealmsOfTerrinoth, "Warrior", "Воин, мастер ближнего боя.",
            "Athletics", "Brawl", "Coercion", "Melee (Heavy)", "Melee (Light)", "Perception", "Resilience", "Vigilance"),
    ];

    // ─────────────────────────── talents ───────────────────────────
    // Таланты загружаются из каталога SeedContent/talents.catalog.json (см. TalentCatalog).
    // Сеттинг каждого таланта задаёт систему: Any → обе, Fantasy → только Realms of Terrinoth.

    // ─────────────────────────── items ───────────────────────────
    // Снаряжение загружается из каталога SeedContent/items.catalog.json (см. ItemCatalog).
    // Сеттинг каждого предмета задаёт систему: Any → обе, Fantasy → только Realms of Terrinoth.

    // ─────────────────────────── heroic abilities ───────────────────────────
    // Героические способности (с улучшениями Improved/Supreme) загружаются из каталога
    // SeedContent/heroics.catalog.json (см. HeroicCatalog). Только Realms of Terrinoth.

    // ─────────────────────────── spells ───────────────────────────

    private static SpellDef Spell(GameSystem sys, string skill, SpellEntryKind kind, string parent, string ru,
        string en, string difficulty, string desc, string safe, string source, int sort) => new()
    {
        Id = Guid.NewGuid(), System = sys, MagicSkill = skill, Kind = kind, ParentEffect = parent,
        NameRu = ru, NameEn = en, Difficulty = difficulty, Description = desc,
        SafeDescription = safe, Source = source, SortOrder = sort,
    };

    /// <summary>
    /// Справочник магии. Базовые эффекты (направления) доступны не для всех магических навыков —
    /// доступность задаётся матрицей <see cref="EffectSkills"/>. Дополнительные эффекты-модификаторы
    /// привязаны к конкретному базовому эффекту через <see cref="SpellDef.ParentEffect"/>.
    /// Только структура, числа и краткие парафразы — без текста книг. Description — полный (private)
    /// парафраз, SafeDescription — copyright-safe вариант для публичной версии, Source — ссылка на раздел.
    /// Arcana/Divine/Primal есть в обеих системах; Runes/Verse — только в Realms of Terrinoth.
    /// Mask/Predict/Transform взяты из Expanded Player's Guide.
    /// </summary>
    private static IEnumerable<SpellDef> Spells(GameSystem sys)
    {
        var terrinoth = sys == GameSystem.RealmsOfTerrinoth;
        var systemSkills = terrinoth
            ? new[] { "Arcana", "Divine", "Primal", "Runes", "Verse" }
            : ["Arcana", "Divine", "Primal"];

        const string coreSource = "Genesys CRB, гл. «Магия»";
        const string terrSource = "Realms of Terrinoth, гл. «Магия»";
        const string epgSource = "Expanded Player's Guide, гл. «Магия»";
        string skillSource(string skill) => skill is "Runes" or "Verse" ? terrSource : coreSource;
        var sysSource = terrinoth ? terrSource : coreSource;

        // Базовые эффекты + навыки, которым они доступны (матрица доступности).
        // Skills: максимальный набор (для Terrinoth); Core фильтрует Runes/Verse.
        // SrcOverride: если null — используется skillSource(skill); иначе — указанный источник.
        var effects = new (string En, string Ru, string Diff, string Desc, string Safe, string[] Skills, string? SrcOverride, int Sort)[]
        {
            ("Attack", "Атака", "1 (Easy)",
                "Боевое магическое действие против одной цели на короткой дистанции: урон равен характеристике используемого магического навыка + 1 за каждый неотменённый успех. Базовая атака не имеет критического значения; критическую травму можно причинить только за триумф или через добавленный эффект с критическим значением.",
                "Магическая атака цели на короткой дистанции.",
                ["Arcana", "Divine", "Primal", "Runes"], null, 1),
            ("Augment", "Усиление", "2 (Average)",
                "Временно усиливает цель вплотную с заклинателем; базово повышает одну характеристику для всех проверок навыков.",
                "Временно усиливает характеристику цели.",
                ["Divine", "Primal", "Runes", "Verse"], null, 2),
            ("Barrier", "Барьер", "1 (Easy)",
                "Создаёт магическую защиту для цели вплотную с заклинателем до конца следующего хода. Базово уменьшает урон от каждого попадания по цели на 1; при дополнительных успехах снижение урона может увеличиваться по правилам барьера.",
                "Защищает цель, снижая получаемый урон.",
                ["Arcana", "Divine", "Runes"], null, 3),
            ("Conjure", "Призыв", "1 (Easy)",
                "Создаёт простой предмет, оружие ближнего боя без движущихся частей или временного приспешника силуэта не больше 1.",
                "Создаёт предмет или приспешника.",
                ["Arcana", "Primal"], null, 4),
            ("Curse", "Проклятье", "2 (Average)",
                "Накладывает на цель негативный боевой эффект; базово снижает способность проверок цели на 1.",
                "Накладывает штраф на проверки цели.",
                ["Arcana", "Divine", "Runes", "Verse"], null, 5),
            ("Dispel", "Рассеивание", "3 (Hard)",
                "Пытается снять с цели магические эффекты; при успехе эффекты на цели немедленно заканчиваются.",
                "Снимает активные магические эффекты.",
                ["Arcana", "Verse"], null, 6),
            ("Heal", "Лечение", "1 (Easy)",
                "Магическое лечение ран и усталости у цели вплотную, которая не выведена из строя. При успехе цель лечит 1 рану за каждый неотменённый успех и 1 усталость за каждое неотменённое преимущество.",
                "Восстанавливает раны и усталость.",
                ["Divine", "Primal", "Verse"], null, 7),
            ("Utility", "Вспомогательная магия", "1 (Easy)",
                "Малые и повествовательные магические эффекты: свет, звук, мелкое перемещение предметов, обнаружение магии и подобные фокусы.",
                "Мелкие вспомогательные магические трюки.",
                ["Arcana", "Divine", "Primal", "Runes", "Verse"], null, 8),
            // EPG
            ("Mask", "Маска", "1 (Easy)",
                "Создаёт иллюзию существа или предмета силуэта 1 или меньше в пределах короткой дистанции либо меняет внешний вид заклинателя или цели вплотную.",
                "Создаёт иллюзию или меняет внешний вид цели.",
                ["Arcana"], epgSource, 9),
            ("Predict", "Предсказание", "2 (Average)",
                "Позволяет задать вопрос о событиях ближайших 24 часов; ответ ведущего правдив, но может быть неоднозначным.",
                "Задаёт вопрос о ближайших событиях.",
                ["Arcana", "Divine"], epgSource, 10),
            ("Transform", "Трансформация", "2 (Average)",
                "Позволяет заклинателю принять форму природного животного силуэта 0, сохраняя свои навыки, таланты и порог усталости.",
                "Превращает заклинателя в животное.",
                ["Primal"], epgSource, 11),
        };

        // Дополнительные эффекты, привязанные к базовому (Parent = En базового эффекта).
        // SrcOverride: если null — используется sysSource; для EPG-эффектов — epgSource.
        var additional = new (string Parent, string Ru, string En, string Diff, string Desc, string Safe, string? SrcOverride, int Sort)[]
        {
            // Attack
            ("Attack", "Ближний бой", "Close Combat", "+1",
                "Позволяет выбрать целью персонажа, находящегося вплотную с заклинателем.",
                "Цель может быть вплотную с заклинателем.", null, 1),
            ("Attack", "Взрывной", "Blast", "+1",
                "Атака получает свойство «Взрыв» с рейтингом, равным рангу Знания заклинателя. Чтобы нанести урон взрывом соседним целям, после успешного попадания нужно активировать свойство: обычно 2 преимущества за срабатывание.",
                "Добавляет свойство «Взрыв» (активация: 2 преимущества).", null, 2),
            ("Attack", "Двигающий", "Move", "+1",
                "Если атака попадает, можно потратить 1 преимущество, чтобы переместить цель на один диапазон дистанции в любом направлении. Только Магия/Arcana. В русском тексте эффект дублирует Управляющий; оставлен отдельной строкой как в таблице.",
                "При попадании 1 преимущество → переместить цель (только Arcana).", null, 3),
            ("Attack", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; эффект можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 4),
            ("Attack", "Ледяной", "Ice", "+1",
                "Атака получает свойство «Сковывание» с рейтингом, равным рангу Знания заклинателя. Чтобы обездвижить цель, после попадания нужно активировать свойство: обычно 2 преимущества.",
                "Добавляет «Сковывание» (активация: 2 преимущества).", null, 5),
            ("Attack", "Молниеносный", "Lightning", "+1",
                "Атака получает «Оглушение» с рейтингом, равным рангу Знания, и свойство «Автоматическое». Оглушение и дополнительные попадания от Автоматического требуют отдельной активации после броска: обычно 2 преимущества за каждое срабатывание. При использовании Автоматического свойства сложность увеличивается дополнительно по обычным правилам.",
                "Добавляет «Оглушение» и «Автоматическое» (активация: 2 преимущества каждое).", null, 6),
            ("Attack", "Нелетальный", "Non-Lethal", "+1",
                "Атака получает свойство «Оглушающий урон»: наносит урон усталостью вместо ран. Отдельная активация преимуществами не требуется. Только Природа/Primal.",
                "Урон усталостью вместо ран, без активации (только Primal).", null, 7),
            ("Attack", "Огненный", "Fire", "+1",
                "Атака получает свойство «Жжение» с рейтингом, равным рангу Знания заклинателя. Чтобы поджечь цель, после успешного попадания нужно активировать свойство: обычно 2 преимущества.",
                "Добавляет «Жжение» (активация: 2 преимущества).", null, 8),
            ("Attack", "Святой/нечестивый", "Holy/Unholy", "+1",
                "Против целей, признанных врагами веры или божества заклинателя, каждый неотменённый успех даёт +2 урона вместо +1. Только Вера/Divine.",
                "Усиленный урон против врагов веры (только Divine).", null, 9),
            ("Attack", "Смертельный", "Deadly", "+1",
                "Атака получает критическое значение 2 и свойство «Высококритичное» с рейтингом, равным рангу Знания. После успешного попадания критическая травма обычно стоит 2 преимущества или 1 триумф.",
                "Крит. значение 2 и «Высококритичное» (крит: 2 преимущества или 1 триумф).", null, 10),
            ("Attack", "Ударный", "Impact", "+1",
                "Атака получает «Нокдаун» и «Дезориентацию» с рейтингом, равным рангу Знания. Нокдаун активируется за 1 преимущество плюс 1 преимущество за каждый пункт силуэта цели выше 1; Дезориентация обычно активируется за 2 преимущества.",
                "«Нокдаун» и «Дезориентация» (активация раздельная).", null, 11),
            ("Attack", "Управляющий", "Manipulative", "+1",
                "Если атака попадает, можно потратить 1 преимущество, чтобы переместить цель на один диапазон дистанции в любом направлении. Только Магия/Arcana.",
                "При попадании 1 преимущество → переместить цель (только Arcana).", null, 12),
            ("Attack", "Разрушительный", "Destructive", "+2",
                "Атака получает свойства «Повреждение» и «Проникающее» с рейтингом, равным рангу Знания. Проникающее действует пассивно, а Повреждение активируется за 1 преимущество и может быть активировано даже при промахе.",
                "«Проникающее» пассивно; «Повреждение» за 1 преимущество (даже при промахе).", null, 13),
            ("Attack", "Усиленный", "Empowered", "+2",
                "Базовый урон атаки равен удвоенному рейтингу характеристики магического навыка; если есть Взрыв, он действует на всех в пределах короткой дистанции.",
                "Удваивает базовый урон атаки.", null, 14),
            ("Attack", "Ядовитый", "Poisonous", "+2",
                "Если атака наносит урон, цель немедленно совершает сложную проверку Стойкости. При провале цель получает раны и усталость, каждое значение равно рангу Знания заклинателя. Считается действием яда; отдельная трата преимуществ не требуется.",
                "При уроне — проверка Стойкости или раны + усталость (без активации).", null, 15),
            // Barrier
            ("Barrier", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 1),
            ("Barrier", "Дополнительная цель", "Additional Target", "+1",
                "Заклинание воздействует на одну дополнительную цель в пределах дистанции. После наложения можно потратить 1 преимущество, чтобы воздействовать ещё на одну дополнительную цель в пределах дистанции; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет цель; после броска +1 цель за 1 преимущество.", null, 2),
            ("Barrier", "Добавление защиты", "Add Defense", "+2",
                "Цели получают ближнюю и дальнюю защиту, равную рангу Знания заклинателя.",
                "Добавляет ближнюю и дальнюю защиту.", null, 3),
            ("Barrier", "Отражающий", "Reflective", "+2",
                "Если противник совершает магическую атаку по цели под этим барьером и на своей проверке получает 3 угрозы или 1 крах, после проверки он сам получает попадание, наносящее урон, равный итоговому урону его атаки. Только Магия/Arcana.",
                "3 угрозы/1 крах атакующего → он получает свой урон обратно (только Arcana).", null, 4),
            ("Barrier", "Святилище", "Sanctuary", "+2",
                "Враги веры или божества заклинателя автоматически перестают быть вплотную с целью и не могут снова войти вплотную до конца барьера. Только Вера/Divine.",
                "Враги веры не могут приблизиться к цели (только Divine).", null, 5),
            ("Barrier", "Усиленный", "Empowered", "+2",
                "Барьер уменьшает урон на количество неотменённых успехов вместо базового эффекта.",
                "Урон снижается на число успехов.", null, 6),
            // Heal
            ("Heal", "Восстанавливающий", "Restoration", "+1",
                "Завершает один длительный эффект состояния, действующий на цель.",
                "Снимает один длительный эффект состояния.", null, 1),
            ("Heal", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 2),
            ("Heal", "Дополнительная цель", "Additional Target", "+1",
                "Заклинание воздействует на одну дополнительную цель в пределах дистанции. После наложения можно потратить 1 преимущество, чтобы воздействовать ещё на одну дополнительную цель в пределах дистанции; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет цель; после броска +1 цель за 1 преимущество.", null, 3),
            ("Heal", "Лечение критических травм", "Heal Critical Injury", "+2",
                "Выбранная критическая травма цели излечивается при успешном заклинании.",
                "Излечивает критическую травму.", null, 4),
            ("Heal", "Оживление выведенных из строя", "Revive Incapacitated", "+2",
                "Позволяет выбирать целями персонажей, выведенных из строя.",
                "Позволяет лечить выведенных из строя.", null, 5),
            ("Heal", "Воскрешение", "Resurrection", "+4",
                "Позволяет выбрать цель, умершую в текущей сцене; при успехе цель оживает с ранами на уровне порога ран. При провале никто больше не может пытаться воскресить эту цель.",
                "Воскрешает цель, умершую в текущей сцене.", null, 6),
            // Conjure
            ("Conjure", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию между заклинателем и местом появления призванного предмета или существа; можно добавлять несколько раз.",
                "Увеличивает дальность призыва.", null, 1),
            ("Conjure", "Дополнительный призыв", "Additional Summon", "+1",
                "Заклинание призывает один дополнительный предмет, оружие или существо. После наложения можно потратить 1 преимущество, чтобы призвать ещё один дополнительный предмет, оружие или существо; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет призыв; после броска +1 призыв за 1 преимущество.", null, 2),
            ("Conjure", "Призыв союзника", "Summon Ally", "+1",
                "Призванное существо дружественно к персонажу и подчиняется его командам; персонаж может тратить манёвр, чтобы давать ему указания.",
                "Существо дружественно и выполняет команды.", null, 3),
            ("Conjure", "Средний призыв", "Medium Summon", "+1",
                "Позволяет призвать более сложный инструмент с движущимися частями, соперника силуэта не больше 1 или двуручное оружие ближнего боя.",
                "Призывает более сложный предмет или соперника.", null, 4),
            ("Conjure", "Великий призыв", "Grand Summon", "+2",
                "Позволяет призвать соперника силуэта не больше 3.",
                "Призывает крупного соперника (силуэт ≤ 3).", null, 5),
            // Curse
            ("Curse", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 1),
            ("Curse", "Неудача", "Misfortune", "+1",
                "После проверки цели можно изменить один результат так, чтобы ухудшить её успех.",
                "После проверки можно ухудшить один результат.", null, 2),
            ("Curse", "Ослабление", "Enervate", "+1",
                "Когда цель получает усталость по любой причине, она получает на 1 усталость больше.",
                "Цель получает +1 усталость при каждом получении усталости.", null, 3),
            ("Curse", "Дополнительная цель", "Additional Target", "+2",
                "Заклинание воздействует на одну дополнительную цель в пределах дистанции. После наложения можно потратить 1 преимущество, чтобы воздействовать ещё на одну дополнительную цель в пределах дистанции; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет цель; после броска +1 цель за 1 преимущество.", null, 4),
            ("Curse", "Отчаяние", "Despair", "+2",
                "Пороги ран и усталости цели уменьшаются на ранг Знания заклинателя. Только Вера/Divine. Нельзя сочетать с «Дополнительная цель».",
                "Снижает пороги ран/усталости (только Divine).", null, 5),
            ("Curse", "Рок", "Doom", "+2",
                "После проверки цели можно повернуть одну любую кость в наборе, не показывающую Триумф или Крах, на другую грань. Только Магия/Arcana.",
                "Можно изменить грань одной кости цели (только Arcana).", null, 6),
            ("Curse", "Паралич", "Paralyzed", "+3",
                "Цель становится ошеломлённой на время действия заклинания. Нельзя сочетать с «Дополнительная цель».",
                "Цель ошеломлена на время заклинания.", null, 7),
            // Dispel
            ("Dispel", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 1),
            ("Dispel", "Дополнительная цель", "Additional Target", "+2",
                "Заклинание воздействует на одну дополнительную цель в пределах дистанции. После наложения можно потратить 1 преимущество, чтобы воздействовать ещё на одну дополнительную цель в пределах дистанции; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет цель; после броска +1 цель за 1 преимущество.", null, 2),
            // Augment
            ("Augment", "Божественное здоровье", "Divine Health", "+1",
                "Цель увеличивает порог ран на число, равное рангу Знания заклинателя, на время действия заклинания. Только Вера/Divine.",
                "Повышает порог ран цели (только Divine).", null, 1),
            ("Augment", "Быстрота", "Haste", "+1",
                "Цели игнорируют эффекты пересечённой местности и не могут быть обездвижены.",
                "Цель игнорирует пересечённую местность и обездвиживание.", null, 2),
            ("Augment", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", null, 3),
            ("Augment", "Природная ярость", "Primal Fury", "+1",
                "Цель добавляет к безоружным боевым проверкам дополнительный урон, равный рангу Знания; критическое значение таких проверок становится 3. Только Природа/Primal.",
                "Усиливает безоружный бой цели (только Primal).", null, 4),
            ("Augment", "Ускорение", "Swift", "+1",
                "Цели всегда могут совершать второй манёвр в свой ход без получения усталости; обычный лимит двух манёвров сохраняется.",
                "Второй манёвр без усталости.", null, 5),
            ("Augment", "Дополнительная цель", "Additional Target", "+2",
                "Заклинание воздействует на одну дополнительную цель в пределах дистанции. После наложения можно потратить 1 преимущество, чтобы воздействовать ещё на одну дополнительную цель в пределах дистанции; это можно повторять, каждый раз тратя 1 преимущество.",
                "Добавляет цель; после броска +1 цель за 1 преимущество.", null, 6),
            // Mask (EPG)
            ("Mask", "Размытие", "Blur", "+1",
                "Если цель — персонаж, её форма размывается; до конца заклинания атаки по ней получают штраф к результатам.",
                "Атаки по цели получают штраф.", epgSource, 1),
            ("Mask", "Зеркальные образы", "Mirror Image", "+1",
                "Если заклинание нацелено на персонажа, оно создаёт несколько копий, которые двигаются вместе с ним. Пока эффект действует, можно потратить 3 угрозы или 1 крах из любой боевой проверки, нацеленной на персонажа, чтобы атака безвредно попала в зеркальный образ вместо настоящей цели.",
                "3 угрозы/1 крах атакующего → атака попадает в копию.", epgSource, 2),
            ("Mask", "Дополнительная иллюзия", "Additional Illusion", "+1",
                "Заклинание создаёт одну дополнительную иллюзию или маскирует одного дополнительного персонажа. После наложения можно потратить 2 преимущества, чтобы создать ещё одну иллюзию или замаскировать ещё одного персонажа; это можно повторять, каждый раз тратя 2 преимущества.",
                "Добавляет иллюзию; после броска +1 иллюзия за 2 преимущества.", epgSource, 3),
            ("Mask", "Дистанционный", "Range", "+1",
                "Увеличивает дистанцию заклинания на одну категорию; можно добавлять несколько раз.",
                "Увеличивает дальность заклинания.", epgSource, 4),
            ("Mask", "Размер", "Size", "+1",
                "Увеличивает силуэт создаваемой иллюзии или размер маскируемой цели на 1; можно добавлять несколько раз.",
                "Увеличивает силуэт иллюзии.", epgSource, 5),
            ("Mask", "Реализм", "Realism", "+1",
                "Увеличивает сложность проверок, чтобы распознать иллюзию, на 1. После наложения можно потратить 2 преимущества, чтобы увеличить эту сложность ещё на 1; можно повторять. Иллюзия также может обманывать дополнительные чувства, например запах, вкус или осязание.",
                "Иллюзию труднее распознать; после броска +1 сложность за 2 преимущества.", epgSource, 6),
            ("Mask", "Ужас", "Terror", "+2",
                "Иллюзия пугает тех, кто не знает, что она ненастоящая. Когда такой персонаж видит иллюзию, он совершает сложную проверку Дисциплины/страха; он получает 2 усталости за каждую угрозу на этой проверке, а при провале не может приблизиться к иллюзии.",
                "Иллюзия пугает; жертва получает 2 усталости за каждую угрозу на проверке страха.", epgSource, 7),
            ("Mask", "Невидимость", "Invisibility", "+3",
                "Если цель — персонаж, заклинание делает его невидимым для зрения вместо изменения внешности.",
                "Делает цель невидимой.", epgSource, 8),
            // Predict (EPG)
            ("Predict", "Молниеносные рефлексы", "Quicksilver Reflexes", "+0",
                "Вместо вопроса о событиях персонаж добавляет бонусы к проверкам инициативы в следующей структурированной сцене.",
                "Бонус к инициативе вместо вопроса.", epgSource, 1),
            ("Predict", "Ясновидение", "Scry", "+1",
                "Вместо вопроса позволяет узнать местоположение одного предмета силуэта 0 в пределах дальней дистанции, если персонаж заранее знает, что ищет. Не раскрывает, как пройти через препятствия.",
                "Определяет местоположение известного предмета.", epgSource, 2),
            ("Predict", "Усиленный", "Empowered", "+1",
                "Позволяет спрашивать о событиях в пределах ближайшего месяца вместо ближайших 24 часов.",
                "Вопрос может охватывать до месяца.", epgSource, 3),
            ("Predict", "Дополнительные вопросы", "Additional Questions", "+1",
                "Позволяет задать один дополнительный вопрос о событиях. После наложения можно потратить 2 преимущества, чтобы задать ещё один дополнительный вопрос; это можно повторять, каждый раз тратя 2 преимущества.",
                "Добавляет вопрос; после броска +1 вопрос за 2 преимущества.", epgSource, 4),
            ("Predict", "Вспышка предвидения", "Flash of Precognition", "+2",
                "Помимо вопроса, один раз до конца текущей сцены персонаж может добавить 1 бонусную кость к одной своей проверке, а также один раз добавить 1 штрафную кость к проверке, нацеленной на него. После наложения можно потратить 3 преимущества, чтобы вместо обычной выгоды добавить 2 бонусные кости к своей проверке и 2 штрафные кости к проверке против персонажа.",
                "+1 бонусная/штрафная кость; за 3 преимущества — по 2 кости.", epgSource, 5),
            ("Predict", "Обмануть смерть", "Cheat Death", "+2",
                "Помимо вопроса, персонаж предвидит возможную гибель в ближайшие 24 часа. Один раз до конца текущей сессии, когда персонаж иначе был бы выведен из строя или убит, можно потратить 1 очко сюжета: персонаж получает раны и усталость до своих порогов, но не превышает их, и выживает.",
                "1 очко сюжета → выжить, когда иначе был бы убит.", epgSource, 6),
            // Transform (EPG)
            ("Transform", "Увеличение силуэта", "Silhouette Increase", "+1",
                "Позволяет превратиться в животное на один силуэт крупнее; можно добавлять несколько раз.",
                "Превращение в более крупное животное.", epgSource, 1),
            ("Transform", "Сохранение характеристик", "Characteristic Retention", "+1",
                "В форме животного персонаж сохраняет собственные Интеллект и Волю вместо характеристик существа.",
                "Сохраняет Интеллект и Волю в форме животного.", epgSource, 2),
            ("Transform", "Трансформация снаряжения", "Transform Gear", "+1",
                "Надетое и удерживаемое снаряжение превращается в естественные отметины на теле животного и возвращается при обратном превращении. Во время превращения снаряжение не даёт преимуществ.",
                "Снаряжение сохраняется при превращении в виде отметин.", epgSource, 3),
            ("Transform", "Ужасная форма", "Dire Form", "+1",
                "Персонаж принимает усиленную версию животного: больше урон оружия, поглощение, порог ран и силуэт.",
                "Усиленная версия животной формы.", epgSource, 4),
            ("Transform", "Проклятие дикой природы", "Curse of the Wild", "+3",
                "Вместо себя персонаж превращает одну цель на короткой дистанции в животное силуэта 0 по своему выбору.",
                "Превращает цель в животное силуэта 0.", epgSource, 5),
        };

        // Базовые эффекты: одна запись на (навык, эффект) для тех навыков системы, где эффект доступен.
        foreach (var e in effects)
            foreach (var skill in e.Skills)
            {
                if (!systemSkills.Contains(skill)) continue; // в Genesys Core нет Runes/Verse
                yield return Spell(sys, skill, SpellEntryKind.Effect, "",
                    e.Ru, e.En, e.Diff, e.Desc, e.Safe, e.SrcOverride ?? skillSource(skill), e.Sort);
            }

        // Дополнительные эффекты: одна запись на (система, базовый эффект), независимо от навыка.
        foreach (var m in additional)
            yield return Spell(sys, "", SpellEntryKind.AdditionalEffect, m.Parent,
                m.Ru, m.En, m.Diff, m.Desc, m.Safe, m.SrcOverride ?? sysSource, m.Sort);
    }
}
