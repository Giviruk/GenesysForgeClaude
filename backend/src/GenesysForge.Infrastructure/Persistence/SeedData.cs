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
        var items = CoreItems().Concat(TerrinothItems()).ToList();
        var heroics = HeroicAbilities().ToList();
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

    private static readonly Dictionary<string, string> ItemRu = new()
    {
        ["Knife"] = "Нож", ["Sword"] = "Меч", ["Pistol"] = "Пистолет", ["Rifle"] = "Винтовка",
        ["Shield"] = "Щит", ["Heavy Jacket"] = "Плотная куртка", ["Armored Vest"] = "Бронежилет",
        ["Backpack"] = "Рюкзак", ["Medkit"] = "Аптечка", ["Rations (1 day)"] = "Паёк (1 день)",
        ["Dagger"] = "Кинжал", ["Longsword"] = "Длинный меч", ["Greatsword"] = "Двуручный меч",
        ["Bow"] = "Лук", ["Crossbow"] = "Арбалет", ["Padded Armor"] = "Стёганый доспех",
        ["Chainmail"] = "Кольчуга", ["Plate Armor"] = "Латный доспех", ["Healing Potion"] = "Зелье лечения",
        ["Rope (10 m)"] = "Верёвка (10 м)", ["Torch"] = "Факел",
    };

    private static ItemDef Item(GameSystem sys, string name, ItemKind kind, int enc, string safe,
        int soak = 0, int mdef = 0, int rdef = 0, int encBonus = 0, int price = 0, int rarity = 1) => new()
    {
        Id = Guid.NewGuid(), System = sys, Code = Code(sys, "item", name),
        Name = name, NameRu = Ru(ItemRu, name), Kind = kind, Encumbrance = enc, SafeDescription = safe,
        SoakBonus = soak, MeleeDefense = mdef, RangedDefense = rdef, EncumbranceThresholdBonus = encBonus,
        Price = price, Rarity = rarity,
        Source = (sys == GameSystem.GenesysCore ? "Genesys Core Rulebook, гл. «Снаряжение»" : "Realms of Terrinoth, гл. «Снаряжение»"),
    };

    private static IEnumerable<ItemDef> CoreItems()
    {
        const GameSystem S = GameSystem.GenesysCore;
        return
        [
            Item(S, "Knife", ItemKind.Weapon, 1, "Урон +1; Крит 3; Ближний бой (Melee).", price: 10),
            Item(S, "Sword", ItemKind.Weapon, 1, "Урон +2; Крит 2; Defensive 1 (Melee).", price: 250, rarity: 3),
            Item(S, "Pistol", ItemKind.Weapon, 1, "Урон 6; Крит 3; Средняя дальность (Ranged Light).", price: 400, rarity: 4),
            Item(S, "Rifle", ItemKind.Weapon, 4, "Урон 8; Крит 3; Дальняя дальность (Ranged Heavy).", price: 600, rarity: 5),
            Item(S, "Shield", ItemKind.Weapon, 2, "Урон +0; Крит 5; Defensive 1, Deflection 1.", mdef: 1, price: 50, rarity: 2),
            Item(S, "Heavy Jacket", ItemKind.Armor, 1, "Плотная куртка.", soak: 1, price: 50),
            Item(S, "Armored Vest", ItemKind.Armor, 3, "Бронежилет.", soak: 2, price: 500, rarity: 5),
            Item(S, "Backpack", ItemKind.Gear, 0, "Увеличивает порог переносимого веса на 4 (пока надет).", encBonus: 4, price: 25),
            Item(S, "Medkit", ItemKind.Gear, 1, "Снимает Setback с проверок Medicine.", price: 100, rarity: 2),
            Item(S, "Rations (1 day)", ItemKind.Gear, 1, "Дневной паёк.", price: 5),
        ];
    }

    private static IEnumerable<ItemDef> TerrinothItems()
    {
        const GameSystem S = GameSystem.RealmsOfTerrinoth;
        return
        [
            Item(S, "Dagger", ItemKind.Weapon, 1, "Урон +1; Крит 3 (Melee Light).", price: 5),
            Item(S, "Longsword", ItemKind.Weapon, 2, "Урон +3; Крит 2; Defensive 1 (Melee Light).", price: 150, rarity: 4),
            Item(S, "Greatsword", ItemKind.Weapon, 3, "Урон +4; Крит 2; Cumbersome 3, Pierce 1 (Melee Heavy).", price: 300, rarity: 5),
            Item(S, "Bow", ItemKind.Weapon, 2, "Урон 7; Крит 3; Средняя дальность; Unwieldy 2 (Ranged).", price: 80, rarity: 2),
            Item(S, "Crossbow", ItemKind.Weapon, 3, "Урон 7; Крит 2; Средняя дальность; Pierce 2, Prepare 1 (Ranged).", price: 120, rarity: 3),
            Item(S, "Shield", ItemKind.Weapon, 2, "Урон +0; Крит 5; Defensive 1, Deflection 1.", mdef: 1, price: 25, rarity: 1),
            Item(S, "Padded Armor", ItemKind.Armor, 2, "Стёганый доспех.", soak: 1, price: 15),
            Item(S, "Chainmail", ItemKind.Armor, 4, "Кольчуга.", soak: 2, price: 200, rarity: 4),
            Item(S, "Plate Armor", ItemKind.Armor, 6, "Латный доспех.", soak: 2, mdef: 1, rdef: 1, price: 1200, rarity: 7),
            Item(S, "Backpack", ItemKind.Gear, 0, "Увеличивает порог переносимого веса на 4 (пока надет).", encBonus: 4, price: 10),
            Item(S, "Healing Potion", ItemKind.Gear, 0, "Восстанавливает 4 раны (раз в день на персонажа).", price: 50, rarity: 4),
            Item(S, "Rope (10 m)", ItemKind.Gear, 1, "Прочная верёвка.", price: 2),
            Item(S, "Torch", ItemKind.Gear, 1, "Освещает короткую дальность.", price: 1),
            Item(S, "Rations (1 day)", ItemKind.Gear, 1, "Дневной паёк.", price: 1),
        ];
    }

    // ─────────────────────────── heroic abilities ───────────────────────────

    private static readonly Dictionary<string, string> HeroicRu = new()
    {
        ["Sixth Sense"] = "Шестое чувство", ["Signature Weapon"] = "Именное оружие",
        ["Battle Fury"] = "Боевая ярость", ["Healing Hands"] = "Целящие руки",
        ["Shadow Walker"] = "Идущий в тенях", ["Unbreakable"] = "Несокрушимость",
        ["Inspiring Presence"] = "Воодушевляющее присутствие",
    };

    private static HeroicAbilityDef Heroic(string name, string safe) => new()
    {
        Id = Guid.NewGuid(), Code = $"rot.heroic.{Slug(name)}",
        Name = name, NameRu = Ru(HeroicRu, name), SafeDescription = safe,
        Source = "Realms of Terrinoth, гл. «Героические способности»",
    };

    private static IEnumerable<HeroicAbilityDef> HeroicAbilities() =>
    [
        Heroic("Sixth Sense", "Сверхъестественное чутьё на опасность в выбранной сфере: раз в сессию ГМ отвечает на вопрос о скрытой угрозе; активация инцидентом за Story Point."),
        Heroic("Signature Weapon", "Легендарная связь с личным оружием: пока оно в руках, добавляйте Boost к атакам; оружие невозможно потерять навсегда."),
        Heroic("Battle Fury", "Боевое неистовство: активируйте инцидентом за Story Point — до конца раунда совершите дополнительный манёвр и добавьте урон, равный рангам Resilience."),
        Heroic("Healing Hands", "Целительное прикосновение: раз в столкновение исцелите союзника на величину Willpower; активация действием за Story Point."),
        Heroic("Shadow Walker", "Хождение в тенях: активируйте за Story Point, чтобы немедленно скрыться и переместиться на среднюю дальность незамеченным."),
        Heroic("Unbreakable", "Несокрушимость: раз в сессию, опустившись до 0 ран, останьтесь на ногах с 1 раной; активация инцидентом."),
        Heroic("Inspiring Presence", "Воодушевляющее присутствие: союзники в пределах короткой дальности добавляют Boost к социальным проверкам; усиление за Story Point."),
    ];

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
    /// </summary>
    private static IEnumerable<SpellDef> Spells(GameSystem sys)
    {
        var terrinoth = sys == GameSystem.RealmsOfTerrinoth;
        var systemSkills = terrinoth
            ? new[] { "Arcana", "Divine", "Primal", "Runes", "Verse" }
            : ["Arcana", "Divine", "Primal"];

        const string coreSource = "Genesys CRB, гл. «Магия»";
        const string terrSource = "Realms of Terrinoth, гл. «Магия»";
        string skillSource(string skill) => skill is "Runes" or "Verse" ? terrSource : coreSource;
        var sysSource = terrinoth ? terrSource : coreSource;

        // Базовые эффекты + навыки, которым они доступны (матрица доступности).
        var effects = new (string En, string Ru, string Diff, string Desc, string Safe, string[] Skills, int Sort)[]
        {
            ("Attack", "Атака", "2 (Average)",
                "Нанести магический урон цели в пределах короткой дальности; базовый урон равен значению связанной характеристики.",
                "Боевое заклинание, наносящее урон.", ["Arcana", "Divine", "Primal", "Runes"], 1),
            ("Augment", "Усиление", "2 (Average)",
                "Наделить цель полезным эффектом до конца столкновения: дополнительный манёвр перемещения или бонусные кубы к проверкам.",
                "Накладывает на цель полезный эффект.", ["Divine", "Primal", "Runes", "Verse"], 2),
            ("Barrier", "Барьер", "2 (Average)",
                "Создать защиту: повысить поглощение или защиту цели до конца столкновения.",
                "Повышает защиту/поглощение цели.", ["Arcana", "Divine", "Runes"], 3),
            ("Conjure", "Призыв", "3 (Hard)",
                "Создать существо или предмет под вашим контролем на ограниченное время.",
                "Создаёт существо или предмет.", ["Arcana", "Primal"], 4),
            ("Curse", "Проклятие", "3 (Hard)",
                "Наложить помеху на врага: штрафные кубы к его проверкам или иной негативный эффект.",
                "Накладывает на врага помеху.", ["Arcana", "Divine", "Primal", "Runes", "Verse"], 5),
            ("Dispel", "Развеивание", "2 (Average)",
                "Снять или подавить активный магический эффект в пределах короткой дальности.",
                "Снимает магический эффект.", ["Arcana", "Divine", "Primal"], 6),
            ("Heal", "Лечение", "1 (Easy)",
                "Восстановить раны союзнику в пределах короткой дальности; объём лечения зависит от связанной характеристики.",
                "Восстанавливает раны.", ["Divine", "Primal", "Verse"], 7),
            ("Utility", "Утилита", "1 (Easy)",
                "Прочие мелкие магические эффекты: свет, перемещение предмета, простое послание и т. п.",
                "Мелкие вспомогательные эффекты.", ["Arcana", "Divine", "Primal", "Runes", "Verse"], 8),
        };

        // Дополнительные эффекты, привязанные к базовому (Parent = En базового эффекта).
        var additional = new (string Parent, string Ru, string En, string Diff, string Desc, string Safe, int Sort)[]
        {
            // Attack
            ("Attack", "Доп. цель", "Additional Target", "+1 за цель", "Поразить ещё одну цель в пределах дальности.", "Добавляет цель.", 1),
            ("Attack", "Увеличить дальность", "Range", "+1 за шаг", "Повысить дальность заклинания на один шаг.", "Увеличивает дальность.", 2),
            ("Attack", "Область", "Blast", "+1", "Поразить всех в пределах короткой дальности от цели.", "Делает атаку зональной.", 3),
            ("Attack", "Усилить урон", "Empowered", "+1", "Добавить к урону значение связанной характеристики.", "Усиливает урон.", 4),
            ("Attack", "Сбивание с ног", "Knockdown", "+1", "При успехе цель падает на землю.", "Опрокидывает цель.", 5),
            ("Attack", "Пробивание", "Pierce", "+1", "Игнорировать часть поглощения цели.", "Пробивает поглощение.", 6),
            // Augment
            ("Augment", "Доп. цель", "Additional Target", "+1 за цель", "Усилить ещё одну цель.", "Добавляет цель.", 1),
            ("Augment", "Усилить эффект", "Empowered", "+1", "Увеличить величину положительного эффекта.", "Усиливает эффект.", 2),
            ("Augment", "Длительность", "Duration", "+1", "Эффект сохраняется дольше.", "Продлевает эффект.", 3),
            // Barrier
            ("Barrier", "Доп. цель", "Additional Target", "+1 за цель", "Защитить ещё одну цель.", "Добавляет цель.", 1),
            ("Barrier", "Усилить защиту", "Empowered", "+1", "Увеличить бонус поглощения или защиты.", "Усиливает защиту.", 2),
            ("Barrier", "Длительность", "Duration", "+1", "Барьер держится дольше.", "Продлевает барьер.", 3),
            // Conjure
            ("Conjure", "Доп. существо", "Additional Summon", "+1", "Призвать ещё одно существо или предмет.", "Добавляет призыв.", 1),
            ("Conjure", "Усилить призыв", "Empowered", "+1", "Повысить характеристики призванного.", "Усиливает призыв.", 2),
            ("Conjure", "Длительность", "Duration", "+1", "Призыв сохраняется дольше.", "Продлевает призыв.", 3),
            // Curse
            ("Curse", "Доп. цель", "Additional Target", "+1 за цель", "Проклясть ещё одну цель.", "Добавляет цель.", 1),
            ("Curse", "Усилить помеху", "Empowered", "+1", "Увеличить штраф или негативный эффект.", "Усиливает помеху.", 2),
            ("Curse", "Область", "Blast", "+1", "Затронуть всех в пределах короткой дальности от цели.", "Делает проклятие зональным.", 3),
            // Dispel
            ("Dispel", "Доп. цель", "Additional Target", "+1 за цель", "Затронуть ещё один эффект или цель.", "Добавляет цель.", 1),
            ("Dispel", "Увеличить дальность", "Range", "+1 за шаг", "Повысить дальность на один шаг.", "Увеличивает дальность.", 2),
            // Heal
            ("Heal", "Доп. цель", "Additional Target", "+1 за цель", "Лечить ещё одну цель.", "Добавляет цель.", 1),
            ("Heal", "Усилить лечение", "Empowered", "+1", "Добавить к лечению значение связанной характеристики.", "Усиливает лечение.", 2),
            ("Heal", "Снять состояние", "Remove Condition", "+2", "Дополнительно убрать критическое ранение или негативное состояние.", "Снимает состояние.", 3),
            // Utility
            ("Utility", "Увеличить дальность", "Range", "+1 за шаг", "Повысить дальность на один шаг.", "Увеличивает дальность.", 1),
            ("Utility", "Доп. цель", "Additional Target", "+1 за цель", "Добавить ещё одну цель.", "Добавляет цель.", 2),
            ("Utility", "Длительность", "Duration", "+1", "Эффект действует дольше.", "Продлевает эффект.", 3),
        };

        // Базовые эффекты: одна запись на (навык, эффект) для тех навыков системы, где эффект доступен.
        foreach (var e in effects)
            foreach (var skill in e.Skills)
            {
                if (!systemSkills.Contains(skill)) continue; // в Genesys Core нет Runes/Verse
                yield return Spell(sys, skill, SpellEntryKind.Effect, "",
                    e.Ru, e.En, e.Diff, e.Desc, e.Safe, skillSource(skill), e.Sort);
            }

        // Дополнительные эффекты: одна запись на (система, базовый эффект), независимо от навыка.
        foreach (var m in additional)
            yield return Spell(sys, "", SpellEntryKind.AdditionalEffect, m.Parent,
                m.Ru, m.En, m.Diff, m.Desc, m.Safe, sysSource, m.Sort);
    }
}
