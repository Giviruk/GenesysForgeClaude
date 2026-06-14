using GenesysForge.Application.Features.Reference;
using GenesysForge.Domain;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Тесты двух seed-pipeline (PrivateFull / PublicSafe): полнота private, copyright-safe public,
/// наличие source в public, идемпотентность каждого режима и совпадение структурного покрытия.
/// </summary>
public class ContentSeedTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"content-{Guid.NewGuid():N}").Options);

    [Fact]
    public void PrivateFull_BuiltInContent_HasFullDescriptions()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);

        // У всех встроенных талантов/архетипов есть полное описание (full content).
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.Description)));
        Assert.All(db.ArchetypeDefs, a => Assert.False(string.IsNullOrWhiteSpace(a.Description)));
        // Заклинания тоже отдают полное описание.
        Assert.All(db.SpellDefs, s => Assert.False(string.IsNullOrWhiteSpace(s.Description)));
    }

    [Fact]
    public void PrivateFull_OverlaysRicherDescription_FromPrivateContentFiles()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);

        // private-content/genesys-core.ru.json содержит расширенное описание для gc.item.sword.
        var sword = db.ItemDefs.Single(i => i.System == GameSystem.GenesysCore && i.Name == "Sword");
        Assert.Contains("надёжное универсальное", sword.Description);
        // safe-описание (механика) отличается от полного private-описания.
        Assert.NotEqual(sword.SafeDescription, sword.Description);
    }

    [Fact]
    public void PrivateContentStore_LoadsEmbeddedFiles()
    {
        var store = PrivateContentStore.Load();
        Assert.True(store.Count > 0, "private-content/*.ru.json должны подключаться как embedded resource");
        Assert.NotNull(store.Get("rot.item.plate-armor"));
    }

    [Fact]
    public void PublicSafe_BuiltInContent_HasNoFullDescriptions()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PublicSafe);

        // Полные (private) описания не отдаются ни у одной встроенной записи, включая заклинания.
        Assert.All(db.TalentDefs, t => Assert.True(string.IsNullOrEmpty(t.Description)));
        Assert.All(db.ArchetypeDefs, a => Assert.True(string.IsNullOrEmpty(a.Description)));
        Assert.All(db.ItemDefs, i => Assert.True(string.IsNullOrEmpty(i.Description)));
        Assert.All(db.SpellDefs, s => Assert.True(string.IsNullOrEmpty(s.Description)));
    }

    [Fact]
    public void PublicSafe_KeepsSafeDescription_NameRu_AndSource()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PublicSafe);

        // Source доступен в public у всех типов справочника.
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.Source)));
        Assert.All(db.ArchetypeDefs, a => Assert.False(string.IsNullOrWhiteSpace(a.Source)));
        Assert.All(db.ItemDefs, i => Assert.False(string.IsNullOrWhiteSpace(i.Source)));
        Assert.All(db.SkillDefs, s => Assert.False(string.IsNullOrWhiteSpace(s.Source)));
        Assert.All(db.SpellDefs, s => Assert.False(string.IsNullOrWhiteSpace(s.Source)));

        // Русские названия присутствуют, safe-описания у талантов заполнены.
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.NameRu)));
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.SafeDescription)));
        Assert.All(db.ArchetypeDefs, a => Assert.False(string.IsNullOrWhiteSpace(a.NameRu)));
    }

    [Theory]
    [InlineData(ContentMode.PrivateFull)]
    [InlineData(ContentMode.PublicSafe)]
    public void Apply_IsIdempotent_PerMode(ContentMode mode)
    {
        using var db = NewDb();
        SeedData.Apply(db, mode);
        var first = (db.SkillDefs.Count(), db.TalentDefs.Count(), db.ItemDefs.Count(),
            db.ArchetypeDefs.Count(), db.CareerDefs.Count(), db.HeroicAbilityDefs.Count(), db.SpellDefs.Count());

        SeedData.Apply(db, mode); // повторный сид того же режима не добавляет дублей

        var second = (db.SkillDefs.Count(), db.TalentDefs.Count(), db.ItemDefs.Count(),
            db.ArchetypeDefs.Count(), db.CareerDefs.Count(), db.HeroicAbilityDefs.Count(), db.SpellDefs.Count());
        Assert.Equal(first, second);
    }

    [Fact]
    public void PublicSafe_StructuralCoverage_MatchesPrivateFull()
    {
        // Public-набор должен быть достаточным: те же сущности (Code), что и в private — отличаются только описания.
        using var priv = NewDb();
        using var pub = NewDb();
        SeedData.Apply(priv, ContentMode.PrivateFull);
        SeedData.Apply(pub, ContentMode.PublicSafe);

        Assert.Equal(priv.TalentDefs.Count(), pub.TalentDefs.Count());
        Assert.Equal(priv.SkillDefs.Count(), pub.SkillDefs.Count());
        Assert.Equal(priv.SpellDefs.Count(), pub.SpellDefs.Count());

        var privCodes = priv.TalentDefs.Select(t => t.Code).OrderBy(c => c).ToList();
        var pubCodes = pub.TalentDefs.Select(t => t.Code).OrderBy(c => c).ToList();
        Assert.Equal(privCodes, pubCodes);
        Assert.All(pubCodes, c => Assert.False(string.IsNullOrWhiteSpace(c)));
    }

    [Fact]
    public void TalentCatalog_SeedsBothSystems_WithRussianNames()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);

        var core = db.TalentDefs.Count(t => t.System == GameSystem.GenesysCore);
        var terr = db.TalentDefs.Count(t => t.System == GameSystem.RealmsOfTerrinoth);
        Assert.True(core >= 50, $"ожидался полный каталог талантов Genesys Core, найдено {core}");
        Assert.True(terr > core, $"у Realms of Terrinoth должно быть больше талантов: terr={terr}, core={core}");

        // каждый встроенный талант имеет русское имя, активацию и описание
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.NameRu)));
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.Activation)));
        Assert.All(db.TalentDefs, t => Assert.False(string.IsNullOrWhiteSpace(t.SafeDescription)));
    }

    [Fact]
    public async Task Reference_GenesysCore_ShowsOnlyAnySetting_TerrinothAddsFantasy()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);
        var handler = new GetReferenceHandler(db);
        var userId = Guid.NewGuid();

        var core = await handler.Handle(new GetReferenceQuery(userId, GameSystem.GenesysCore));
        var terr = await handler.Handle(new GetReferenceQuery(userId, GameSystem.RealmsOfTerrinoth));

        // У Genesys Core все таланты — «для любого сеттинга».
        Assert.NotEmpty(core.Talents);
        Assert.All(core.Talents, t => Assert.True(t.Setting.HasFlag(GenesysSetting.Any)));

        // Фэнтези-таланты не видны в Genesys Core, но видны в Realms of Terrinoth.
        Assert.DoesNotContain(core.Talents, t => t.Setting.HasFlag(GenesysSetting.Fantasy));
        Assert.Contains(terr.Talents, t => t.Setting.HasFlag(GenesysSetting.Fantasy));

        // Общий талант «для любого сеттинга» (Упорство) виден в обеих системах.
        Assert.Contains(core.Talents, t => t.Name == "Упорство");
        Assert.Contains(terr.Talents, t => t.Name == "Упорство");
    }
}
