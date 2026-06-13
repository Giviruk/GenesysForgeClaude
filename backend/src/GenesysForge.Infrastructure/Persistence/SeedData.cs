using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Infrastructure.Persistence;

/// <summary>
/// Встроенный контент систем. Структура и правила соответствуют Genesys CRB и Realms of Terrinoth;
/// набор сокращён, значения можно расширять кастомным контентом через UI.
/// </summary>
public static class SeedData
{
    /// <summary>
    /// Идемпотентный сид: добавляет только отсутствующий встроенный контент (по System+Name,
    /// для героик — по Name). Безопасно вызывать на уже засеянной БД — новые встроенные записи
    /// (например, добавленные таланты) досеваются без пересоздания тома и без дублей.
    /// Кастомный контент (OwnerUserId != null) не затрагивается.
    /// </summary>
    public static void Apply(AppDbContext db)
    {
        var added = false;

        added |= SeedMissing(db, db.SkillDefs, CoreSkills().Concat(TerrinothSkills()), d => (d.System, d.Name));
        added |= SeedMissing(db, db.ArchetypeDefs, CoreArchetypes().Concat(TerrinothSpecies()), d => (d.System, d.Name));
        added |= SeedMissing(db, db.CareerDefs, CoreCareers().Concat(TerrinothCareers()), d => (d.System, d.Name));
        added |= SeedMissing(db, db.TalentDefs,
            Talents(GameSystem.GenesysCore).Concat(Talents(GameSystem.RealmsOfTerrinoth)).Concat(TerrinothTalents()),
            d => (d.System, d.Name));
        added |= SeedMissing(db, db.ItemDefs, CoreItems().Concat(TerrinothItems()), d => (d.System, d.Name));
        added |= SeedMissing(db, db.HeroicAbilityDefs, HeroicAbilities(), d => ((GameSystem)0, d.Name));

        if (added) db.SaveChanges();
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
        _ => true, // архетипы и карьеры всегда встроенные
    };

    private static SkillDef Skill(GameSystem sys, string name, CharacteristicType ch, SkillKind kind) =>
        new() { Id = Guid.NewGuid(), System = sys, Name = name, Characteristic = ch, Kind = kind };

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

    private static ArchetypeDef Arch(GameSystem sys, string name, int br, int ag, int @int, int cun, int will, int pr,
        int wt, int st, int xp, string desc) => new()
    {
        Id = Guid.NewGuid(), System = sys, Name = name,
        Brawn = br, Agility = ag, Intellect = @int, Cunning = cun, Willpower = will, Presence = pr,
        WoundBase = wt, StrainBase = st, StartingXp = xp, Description = desc,
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
        Arch(GameSystem.RealmsOfTerrinoth, "Dwarf", 2, 1, 2, 2, 3, 2, 12, 10, 100, "Гномы Даннских холмов — стойкие мастера и воины."),
        Arch(GameSystem.RealmsOfTerrinoth, "Orc", 3, 2, 2, 2, 2, 1, 12, 9, 100, "Орки — могучие и свирепые воины Брокенских земель."),
        Arch(GameSystem.RealmsOfTerrinoth, "Gnome", 1, 2, 2, 3, 2, 2, 8, 11, 100, "Гномы-изобретатели — малы ростом, но хитроумны."),
        Arch(GameSystem.RealmsOfTerrinoth, "Catfolk (Hyrrinx)", 2, 3, 2, 2, 1, 2, 10, 9, 100, "Кошачий народ — стремительные охотники."),
    ];

    private static CareerDef Career(GameSystem sys, string name, string desc, params string[] skills) =>
        new() { Id = Guid.NewGuid(), System = sys, Name = name, Description = desc, CareerSkillNames = [.. skills] };

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

    private static TalentDef Talent(GameSystem sys, string name, int tier, bool ranked, string activation, string desc,
        int wt = 0, int st = 0, int soak = 0, int mdef = 0, int rdef = 0) => new()
    {
        Id = Guid.NewGuid(), System = sys, Name = name, Tier = tier, IsRanked = ranked,
        Activation = activation, Description = desc,
        WoundBonus = wt, StrainBonus = st, SoakBonus = soak, MeleeDefenseBonus = mdef, RangedDefenseBonus = rdef,
    };

    private static IEnumerable<TalentDef> Talents(GameSystem s) =>
    [
        // Тир 1
        Talent(s, "Grit", 1, true, "Пассивный", "Порог стрейна +1 за ранг.", st: 1),
        Talent(s, "Toughened", 1, true, "Пассивный", "Порог ран +2 за ранг.", wt: 2),
        Talent(s, "Jump Up", 1, false, "Манёвр", "Раз в раунд встать из положения сидя/лёжа как инцидентальное действие."),
        Talent(s, "Quick Strike", 1, true, "Пассивный", "Добавьте 1 Boost к атакам по целям, ещё не действовавшим в этом столкновении (за ранг)."),
        Talent(s, "Parry", 1, true, "Инцидент", "Потратьте 3 стрейна, чтобы снизить урон ближней атаки на 2 + ранги Parry."),
        Talent(s, "Forager", 1, false, "Пассивный", "Убирает до 2 Setback с проверок поиска пищи, воды и укрытия."),
        Talent(s, "Surgeon", 1, true, "Пассивный", "При лечении проверкой Medicine цель восстанавливает +1 рану за ранг."),
        Talent(s, "Swift", 1, false, "Пассивный", "Игнорирует пересечённую местность при передвижении."),
        // Тир 2
        Talent(s, "Defensive Stance", 2, true, "Манёвр", "Потратьте манёвр и стрейн (≤ рангов): до конца хода атаки ближнего боя против вас сложнее на ранги."),
        Talent(s, "Side Step", 2, true, "Манёвр", "Потратьте манёвр и стрейн (≤ рангов): до конца хода дальние атаки против вас сложнее на ранги."),
        Talent(s, "Inspiring Rhetoric", 2, false, "Действие", "Проверка Leadership (Average): союзники восстанавливают стрейн за успехи."),
        Talent(s, "Scathing Tirade", 2, false, "Действие", "Проверка Coercion (Average): противники получают стрейн за успехи."),
        Talent(s, "Lucky Strike", 2, false, "Инцидент", "Потратив Story Point, добавьте урон, равный характеристике, к одному попаданию."),
        Talent(s, "Counteroffer", 2, false, "Действие", "Раз в сессию предложите противнику в пределах средней дальности «сделку» — проверка Negotiation, чтобы вывести его из боя."),
        // Тир 3
        Talent(s, "Dodge", 3, true, "Инцидент", "При атаке по вам потратьте стрейн (≤ рангов): сложность атаки повышается на потраченное."),
        Talent(s, "Animal Companion", 3, false, "Пассивный", "Верный зверь-спутник, действующий по вашей команде."),
        Talent(s, "Field Commander", 3, false, "Действие", "Проверка Leadership: союзники немедленно совершают манёвр."),
        Talent(s, "Full Throttle", 3, false, "Действие", "Повысьте максимальную скорость транспорта на 1 на несколько раундов."),
        Talent(s, "Natural", 3, false, "Инцидент", "Раз в сессию переброс одной проверки двух выбранных навыков."),
        Talent(s, "Heroic Recovery", 3, false, "Инцидент", "Потратив Story Point, восстановите стрейн, равный характеристике Willpower."),
        // Тир 4
        Talent(s, "Defensive", 4, true, "Пассивный", "Защита (ближняя и дальняя) +1 за ранг.", mdef: 1, rdef: 1),
        Talent(s, "Enduring", 4, true, "Пассивный", "Поглощение +1 за ранг.", soak: 1),
        Talent(s, "Can't We Talk About This?", 4, false, "Действие", "Проверка Charm/Deception: гуманоиды-противники не атакуют вас, пока вы не нападёте."),
        Talent(s, "Deadeye", 4, false, "Инцидент", "После попадания дальней атакой потратьте 2 стрейна: критическая травма по вашему выбору."),
        // Тир 5
        Talent(s, "Dedication", 5, true, "Пассивный", "Повышает одну характеристику на 1 (макс. 6). Выбор характеристики — на листе."),
        Talent(s, "Indomitable", 5, false, "Инцидент", "Раз в столкновение: получив травму, выводящую из строя, останьтесь в строю до конца следующего хода."),
        Talent(s, "Master", 5, true, "Инцидент", "Раз в раунд потратьте 2 стрейна: снизьте сложность проверки выбранного навыка на 1 (минимум Easy)."),
        Talent(s, "Ruinous Repartee", 5, false, "Действие", "Раз в столкновение: проверка Charm/Coercion — противник получает стрейн, равный удвоенной Presence."),
    ];

    /// <summary>
    /// Таланты, специфичные для Realms of Terrinoth (фэнтези-бой и магия) — в дополнение к общим Genesys-талантам.
    /// Набор сокращён и приближён к книге; расширяется кастомным контентом.
    /// </summary>
    private static IEnumerable<TalentDef> TerrinothTalents()
    {
        const GameSystem S = GameSystem.RealmsOfTerrinoth;
        return
        [
            // Тир 1
            Talent(S, "Brace", 1, true, "Манёвр", "Снимите до рангов Brace кубов Setback, наложенных на проверку условиями окружения или положением."),
            Talent(S, "Hamstring Shot", 1, false, "Действие", "Одна атака Ranged или Brawl; при попадании скорость цели до конца её следующего хода снижается до 0."),
            Talent(S, "Knack for It", 1, true, "Пассивный", "Выберите узкий навык: снимайте 1 Setback за ранг с его проверок."),
            Talent(S, "Second Wind", 1, true, "Инцидент", "Раз в столкновение восстановите стрейн, равный числу рангов Second Wind."),
            // Тир 2
            Talent(S, "Familiar", 2, false, "Пассивный", "Магический спутник-фамильяр действует по вашей команде и помогает в проверках."),
            Talent(S, "Heightened Awareness", 2, false, "Пассивный", "Союзники в короткой дальности добавляют 1 Boost к проверкам Perception и Vigilance."),
            Talent(S, "Bought Info", 2, false, "Действие", "Проверка Streetwise или Knowledge, чтобы собрать слухи и сведения в поселении."),
            // Тир 3
            Talent(S, "Berserk", 3, false, "Манёвр", "Ярость: до конца столкновения +1 успех к ближним атакам, но вы не можете защищаться и совершать иные действия, кроме атак."),
            Talent(S, "Dual Wielder", 3, false, "Манёвр", "Снизьте сложность совмещённой атаки двумя оружиями на 1."),
            Talent(S, "Eldritch Insight", 3, false, "Пассивный", "Добавьте ранги Knowledge (Lore) к проверкам одного выбранного магического навыка при определении эффекта."),
            // Тир 4
            Talent(S, "Counterspell", 4, false, "Инцидент", "Потратьте 3 стрейна, чтобы повысить сложность вражеской магической проверки на 1."),
            Talent(S, "Crippling Blow", 4, false, "Действие", "Атака ближнего боя; при попадании цель получает 1 стрейн при каждом своём действии до конца столкновения."),
            Talent(S, "Superior Reflexes", 4, true, "Пассивный", "Защита (ближняя и дальняя) +1 за ранг.", mdef: 1, rdef: 1),
            // Тир 5
            Talent(S, "Heroic Will", 5, false, "Инцидент", "Потратьте Story Point, чтобы немедленно снять с себя один негативный статус или контролирующий эффект."),
            Talent(S, "Runebound", 5, true, "Пассивный", "Связь с рунным осколком: порог ран +1 и порог стрейна +1 за ранг.", wt: 1, st: 1),
        ];
    }

    private static ItemDef Item(GameSystem sys, string name, ItemKind kind, int enc, string desc,
        int soak = 0, int mdef = 0, int rdef = 0, int encBonus = 0, int price = 0, int rarity = 1) => new()
    {
        Id = Guid.NewGuid(), System = sys, Name = name, Kind = kind, Encumbrance = enc, Description = desc,
        SoakBonus = soak, MeleeDefense = mdef, RangedDefense = rdef, EncumbranceThresholdBonus = encBonus,
        Price = price, Rarity = rarity,
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

    private static IEnumerable<HeroicAbilityDef> HeroicAbilities() =>
    [
        new() { Id = Guid.NewGuid(), Name = "Sixth Sense", Description = "Сверхъестественное чутьё на опасность в выбранной сфере: раз в сессию ГМ отвечает на вопрос о скрытой угрозе; активация инцидентом за Story Point." },
        new() { Id = Guid.NewGuid(), Name = "Signature Weapon", Description = "Легендарная связь с личным оружием: пока оно в руках, добавляйте Boost к атакам; оружие невозможно потерять навсегда." },
        new() { Id = Guid.NewGuid(), Name = "Battle Fury", Description = "Боевое неистовство: активируйте инцидентом за Story Point — до конца раунда совершите дополнительный манёвр и добавьте урон, равный рангам Resilience." },
        new() { Id = Guid.NewGuid(), Name = "Healing Hands", Description = "Целительное прикосновение: раз в столкновение исцелите союзника на величину Willpower; активация действием за Story Point." },
        new() { Id = Guid.NewGuid(), Name = "Shadow Walker", Description = "Хождение в тенях: активируйте за Story Point, чтобы немедленно скрыться и переместиться на среднюю дальность незамеченным." },
        new() { Id = Guid.NewGuid(), Name = "Unbreakable", Description = "Несокрушимость: раз в сессию, опустившись до 0 ран, останьтесь на ногах с 1 раной; активация инцидентом." },
        new() { Id = Guid.NewGuid(), Name = "Inspiring Presence", Description = "Воодушевляющее присутствие: союзники в пределах короткой дальности добавляют Boost к социальным проверкам; усиление за Story Point." },
    ];
}
