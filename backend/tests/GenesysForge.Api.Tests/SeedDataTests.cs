using GenesysForge.Domain;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Tests;

public class SeedDataTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"seed-{Guid.NewGuid():N}").Options);

    [Fact]
    public void Apply_IsIdempotent_NoDuplicatesOnSecondRun()
    {
        using var db = NewDb();
        SeedData.Apply(db);
        var afterFirst = new
        {
            Skills = db.SkillDefs.Count(),
            Talents = db.TalentDefs.Count(),
            Items = db.ItemDefs.Count(),
            Archetypes = db.ArchetypeDefs.Count(),
            Careers = db.CareerDefs.Count(),
            Heroics = db.HeroicAbilityDefs.Count(),
            Spells = db.SpellDefs.Count(),
        };

        SeedData.Apply(db); // повторный вызов не должен ничего добавлять

        Assert.Equal(afterFirst.Skills, db.SkillDefs.Count());
        Assert.Equal(afterFirst.Talents, db.TalentDefs.Count());
        Assert.Equal(afterFirst.Items, db.ItemDefs.Count());
        Assert.Equal(afterFirst.Archetypes, db.ArchetypeDefs.Count());
        Assert.Equal(afterFirst.Careers, db.CareerDefs.Count());
        Assert.Equal(afterFirst.Heroics, db.HeroicAbilityDefs.Count());
        Assert.Equal(afterFirst.Spells, db.SpellDefs.Count());
    }

    [Fact]
    public void Apply_SeedsSpells_TerrinothHasMoreMagicSkills()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var core = db.SpellDefs.Where(s => s.System == GameSystem.GenesysCore).ToList();
        var terrinoth = db.SpellDefs.Where(s => s.System == GameSystem.RealmsOfTerrinoth).ToList();

        Assert.NotEmpty(core);
        // Terrinoth добавляет Runes и Verse поверх базовых навыков
        Assert.DoesNotContain(core, s => s.MagicSkill is "Runes" or "Verse");
        Assert.Contains(terrinoth, s => s.MagicSkill == "Runes");
        Assert.Contains(terrinoth, s => s.MagicSkill == "Verse");
        // у каждой записи заполнены русское имя, safe-описание и источник
        Assert.All(db.SpellDefs, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.NameRu));
            Assert.False(string.IsNullOrWhiteSpace(s.SafeDescription));
            Assert.False(string.IsNullOrWhiteSpace(s.Source));
        });
    }

    [Fact]
    public void Apply_BackfillsMissingBuiltInContent()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        // имитируем «старую» БД: удалили часть встроенных талантов
        var toRemove = db.TalentDefs.Where(t => t.OwnerUserId == null).Take(5).ToList();
        var removedNames = toRemove.Select(t => (t.System, t.Name)).ToList();
        db.TalentDefs.RemoveRange(toRemove);
        db.SaveChanges();
        var reduced = db.TalentDefs.Count();

        SeedData.Apply(db); // должен досеять удалённые

        Assert.Equal(reduced + 5, db.TalentDefs.Count());
        foreach (var (system, name) in removedNames)
            Assert.Contains(db.TalentDefs, t => t.System == system && t.Name == name && t.OwnerUserId == null);
    }

    [Fact]
    public void Apply_UpsertsRuleTables_RestoresChangedFieldsFromCatalog()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        // имитируем «старую» БД: у строк дистанций ещё не было под-раздела (groupRu пустой)
        var ranges = db.RuleTableEntries.Where(r => r.Kind == Domain.Entities.RuleTableKind.RangeBand).ToList();
        Assert.NotEmpty(ranges);
        foreach (var r in ranges) r.GroupRu = "";
        db.SaveChanges();
        var countBefore = db.RuleTableEntries.Count();

        SeedData.Apply(db); // upsert должен синхронизировать поля с каталогом, не плодя дублей

        Assert.Equal(countBefore, db.RuleTableEntries.Count());
        var restored = db.RuleTableEntries.Where(r => r.Kind == Domain.Entities.RuleTableKind.RangeBand).ToList();
        Assert.All(restored, r => Assert.Contains(r.GroupRu, new[] { "Общая информация", "Перемещение" }));
        Assert.Contains(restored, r => r.GroupRu == "Перемещение");
    }

    [Fact]
    public void Apply_SeedsArchetypeCatalog_DetailedTerrinothSpecies()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var rot = db.ArchetypeDefs.Where(a => a.System == GameSystem.RealmsOfTerrinoth && !a.Retired).ToList();
        // Новые детальные виды Терринота из каталога.
        Assert.Contains(rot, a => a.NameRu == "Дунваррский дварф");
        Assert.Contains(rot, a => a.NameRu == "Глубинный эльф");
        Assert.Contains(rot, a => a.NameRu == "Норный гном");
        // У каждого активного вида заполнены RU-имя и safe-описание (способность/навыки).
        Assert.All(rot, a =>
        {
            Assert.False(string.IsNullOrWhiteSpace(a.NameRu));
            Assert.False(string.IsNullOrWhiteSpace(a.SafeDescription));
        });
    }

    [Fact]
    public void Apply_RetiresBuiltInArchetypesNotInCatalog()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        // имитируем «старую» БД: вид, которого нет в текущем каталоге
        db.ArchetypeDefs.Add(new Domain.Entities.ArchetypeDef
        {
            Id = Guid.NewGuid(), System = GameSystem.RealmsOfTerrinoth,
            Code = "rot.archetype.legacy-species", Name = "Legacy", NameRu = "Старый вид",
            Brawn = 2, Agility = 2, Intellect = 2, Cunning = 2, Willpower = 2, Presence = 2,
            WoundBase = 10, StrainBase = 10, StartingXp = 100,
        });
        db.SaveChanges();

        SeedData.Apply(db); // повторный сид деактивирует вид вне каталога

        var legacy = db.ArchetypeDefs.Single(a => a.Code == "rot.archetype.legacy-species");
        Assert.True(legacy.Retired); // остаётся в БД, но Retired
    }

    [Fact]
    public void Apply_NewFantasyCareers_PresentAndSkillsResolveToSeededSkills()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var careers = db.CareerDefs.ToList();
        Assert.Contains(careers, c => c.NameRu == "Рыцарь" && c.System == GameSystem.RealmsOfTerrinoth);
        Assert.Contains(careers, c => c.NameRu == "Волшебник" && c.System == GameSystem.GenesysCore);
        Assert.Contains(careers, c => c.NameRu == "Друид" && c.System == GameSystem.GenesysCore);
        Assert.Contains(careers, c => c.NameRu == "Жрец" && c.System == GameSystem.GenesysCore);

        // Каждый карьерный навык должен существовать среди встроенных навыков своей системы,
        // иначе выбор бесплатных карьерных рангов при создании сломается.
        foreach (var career in careers)
        {
            var skillNames = db.SkillDefs.Where(s => s.System == career.System).Select(s => s.Name).ToHashSet();
            foreach (var cs in career.CareerSkillNames)
                Assert.Contains(cs, skillNames);
        }
    }

    [Fact]
    public void Apply_DoesNotTouchCustomContent()
    {
        using var db = NewDb();
        SeedData.Apply(db);

        var userId = Guid.NewGuid();
        db.SkillDefs.Add(new Domain.Entities.SkillDef
        {
            Id = Guid.NewGuid(), System = GameSystem.GenesysCore, Name = "My Custom Skill",
            Characteristic = CharacteristicType.Brawn, Kind = SkillKind.General, OwnerUserId = userId,
        });
        db.SaveChanges();

        SeedData.Apply(db);

        Assert.Single(db.SkillDefs.Where(s => s.OwnerUserId == userId));
    }
}
