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
        };

        SeedData.Apply(db); // повторный вызов не должен ничего добавлять

        Assert.Equal(afterFirst.Skills, db.SkillDefs.Count());
        Assert.Equal(afterFirst.Talents, db.TalentDefs.Count());
        Assert.Equal(afterFirst.Items, db.ItemDefs.Count());
        Assert.Equal(afterFirst.Archetypes, db.ArchetypeDefs.Count());
        Assert.Equal(afterFirst.Careers, db.CareerDefs.Count());
        Assert.Equal(afterFirst.Heroics, db.HeroicAbilityDefs.Count());
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
