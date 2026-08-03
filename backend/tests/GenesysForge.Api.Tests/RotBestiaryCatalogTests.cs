using GenesysForge.Domain;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-CLEAN-3.6 и ROT-BEST-01: канонический состав активного бестиария RoT. Одного счётчика мало —
/// фикстура фиксирует полный список стабильных кодов, поэтому и пропавшая, и неожиданно появившаяся
/// запись валят тест.
/// </summary>
public class RotBestiaryCatalogTests
{
    /// <summary>
    /// 86 встроенных профилей − 9 записей Haunted City = 77 активных. Четыре профиля скакунов из
    /// ROT-MOUNT-NPC-01 в бестиарий не вошли: их объём владелец исключил, поэтому исходное
    /// число 81 из ТЗ пересчитано.
    /// </summary>
    private static readonly string[] ActiveCodes =
    [
        "ancient-dragon", "assassin", "aymhelin-scion", "barghest", "baronial-knight", "beastman",
        "berserker", "bloodsister-and-nightseer", "carnivorous-flora", "death-knight", "deep-elf",
        "deepwood-archer", "dimora", "djinn", "dragon-hybrid", "dwarf-ancestral-specter", "dwarf-guilder",
        "dwarven-dragon-hunter", "feral-dragon", "ferrox", "flesh-ripper", "forest-guardian", "giant",
        "giant-snake", "gnome-minstrel", "goblin", "goblin-witcher", "greyhaven-wizard", "grotesque",
        "gurak-tol", "ice-blood-warrior", "ice-wyrm", "ironbound", "kennsir-dwarf", "kobold",
        "lava-elemental", "leonx", "leonx-rider", "lord-of-bilehall", "lorimor-marine", "lost-knight",
        "makhim", "manticore", "merriod", "minor-elemental-fire", "minor-elemental-quicksand",
        "minor-elemental-spring", "naga-priestess", "necromancer", "ogre", "onoit-shaman", "orc-outrider",
        "orc-spiritspeaker", "pirate", "priest-of-kellos", "reanimate", "rune-golem", "salamander",
        "scorpion-swarm", "shade", "singhara-hunter", "singhara-pridelord", "siren", "spined-thresher",
        "splig-king-of-all-golbins", "storm-sorceress", "sword-poet", "tamalir-guildmaster",
        "thieves-guild-cutpurse", "true-fae", "viper-legion-archer", "weik-warrior", "wendigo",
        "witch-and-warlock", "wraith", "wyrm-of-the-deep", "young-dragon",
    ];

    /// <summary>Девять записей Haunted City (ROT-CLEAN-3.6): по подтверждённым кодам, не по словам в имени.</summary>
    private static readonly string[] RetiredCodes =
    [
        "brigand", "brigand-leader", "city-guard", "coachman", "danne-bulvert",
        "eliza-farrow", "farrows-guard", "magistrate-edmin-cawl", "mavaris-skain-necromancer",
    ];

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"bestiary-{Guid.NewGuid():N}").Options);

    [Fact]
    public void Bestiary_ActiveProfiles_ExactlyTheExpectedCodes()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var active = db.Npcs.Where(n => n.IsBuiltIn && !n.Retired).Select(n => n.Code).ToList();

        Assert.Equal(77, active.Count);
        Assert.Equal(active.Count, active.Distinct().Count());
        Assert.Equal(ActiveCodes.Order(), active.Order());
    }

    [Fact]
    public void Bestiary_HauntedCityProfiles_RetiredButKept()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var retired = db.Npcs.Where(n => n.IsBuiltIn && n.Retired).ToList();

        Assert.Equal(RetiredCodes.Order(), retired.Select(n => n.Code).Order());
        // Записи не удалены: статблок и вложенные атаки остаются доступными по id
        // для уже созданных столкновений и копий.
        Assert.All(retired, n =>
        {
            Assert.Equal("Haunted City", n.Source);
            Assert.True(n.WoundThreshold > 0);
        });
    }

    [Fact]
    public void Bestiary_RetiredProfiles_HiddenFromLibraryButOpenById()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var brigand = db.Npcs.Single(n => n.Code == "brigand");
        Assert.True(brigand.Retired);
        Assert.DoesNotContain(
            db.Npcs.Where(n => !n.Retired).Select(n => n.Id).ToList(), id => id == brigand.Id);
        Assert.NotNull(db.Npcs.Find(brigand.Id));
    }

    /// <summary>
    /// Переименование `Goblin (Official)` → `Goblin` идёт по стабильному коду: строка, засиженная
    /// под старым именем, получает новое имя и тот же id, а не превращается во второго гоблина.
    /// </summary>
    [Fact]
    public void Bestiary_GoblinRename_KeepsRowAndId()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var goblin = db.Npcs.Single(n => n.IsBuiltIn && n.Code == "goblin");
        Assert.Equal("Гоблин", goblin.Name);

        // Имитация старой установки: код ещё не проставлен, имя прежнее.
        var id = goblin.Id;
        goblin.Code = "";
        goblin.Name = "Гоблин (Официальный)";
        db.SaveChanges();

        SeedData.Apply(db);

        var renamed = db.Npcs.Single(n => n.IsBuiltIn && n.Code == "goblin");
        Assert.Equal(id, renamed.Id);
        Assert.Equal("Гоблин", renamed.Name);
        Assert.Equal(77, db.Npcs.Count(n => n.IsBuiltIn && !n.Retired));
    }

    /// <summary>
    /// Сид пересчитывает Retired и источник у уже засиженной базы: без этого девять записей
    /// Haunted City остались бы в активном бестиарии навсегда.
    /// </summary>
    [Fact]
    public void Bestiary_ExistingRows_GetRetiredFlagOnReseed()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var brigand = db.Npcs.Single(n => n.Code == "brigand");
        brigand.Retired = false;
        brigand.Source = "Realms of Terrinoth";
        db.SaveChanges();

        SeedData.Apply(db);

        var after = db.Npcs.Single(n => n.Code == "brigand");
        Assert.True(after.Retired);
        Assert.Equal("Haunted City", after.Source);
    }

    /// <summary>
    /// Правила kind проверяются при загрузке каталога, а не подгоняются молча: тихая правка
    /// прятала бы ошибку данных. Предела Defense среди них нет — по решению владельца NPC
    /// может иметь защиту выше 4 (ROT-CMB-04 отклонён).
    /// </summary>
    [Fact]
    public void Bestiary_KindRules_HoldForEveryProfile()
    {
        var catalog = BestiaryCatalog.Load();

        Assert.Equal(86, catalog.Count);
        Assert.All(catalog.Where(n => n.Kind == NpcKind.Minion), n =>
        {
            Assert.Null(n.StrainThreshold);
            Assert.All(n.Skills, s => Assert.Equal(0, s.Ranks));
        });
        Assert.All(catalog.Where(n => n.Kind == NpcKind.Rival), n => Assert.Null(n.StrainThreshold));
        Assert.All(catalog.Where(n => n.Kind == NpcKind.Nemesis), n => Assert.True(n.StrainThreshold > 0));
        Assert.All(catalog, n => Assert.All(n.Attacks, a =>
        {
            Assert.NotEmpty(a.Name);
            Assert.NotEmpty(a.SkillName);
            Assert.NotEmpty(a.Damage);
        }));
    }
}
