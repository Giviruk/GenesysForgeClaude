using GenesysForge.Application.Common;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Форма запроса, которым грузится персонаж. Связей у него больше десятка, и почти все —
/// коллекции: навыки, таланты, предметы со справочником, улучшения, транспорт со статблоком.
///
/// <para>
/// В режиме одной выборки EF склеивает их LEFT JOIN'ами, и строки перемножаются. Замер на реальных
/// данных: у персонажа с 40 предметами, 20 талантами, 3 единицами транспорта и 5 улучшениями один
/// такой запрос разворачивался в 7 360 000 строк, и в каждой ехали все описания справочника. Причём
/// на каждое действие он выполнялся дважды: один раз обработчиком правки, второй — сборкой листа
/// в ответ (см. <c>ReturnSheetFilter</c>).
/// </para>
///
/// <para>
/// Лечится это одной строкой в настройке контекста, поэтому её легко потерять при переезде на другую
/// конфигурацию — а заметно это станет не сразу, а у того, кто успел набрать инвентарь. Тест смотрит
/// на сгенерированный SQL и подключения к базе не требует.
/// </para>
/// </summary>
public class CharacterLoadQueryShapeTests
{
    /// <summary>Контекст, собранный ровно так же, как его собирает приложение.</summary>
    private static AppDbContext RealProviderContext()
    {
        var services = new ServiceCollection().AddInfrastructure(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] =
                    "Host=localhost;Port=5432;Database=genesysforge;Username=genesys;Password=x",
            }).Build());
        return services.BuildServiceProvider().GetRequiredService<AppDbContext>();
    }

    /// <summary>
    /// Срез не тянет из базы чужие связи. Само по себе это невидимо — лишний Include не ломает
    /// ответ, а только делает его дороже, — поэтому проверяется явно.
    /// </summary>
    [Theory]
    // Транспорт на базовые числа не влияет (его груз не входит в переносимый вес владельца),
    // профили атак нужны только карточкам инвентаря, выборы рангов — только вкладке талантов.
    [InlineData(SheetSlice.Base, new[] { "Mounts", "AttackProfiles", "Choices" })]
    // Инвентарю не нужны ни навыки, ни таланты, ни героика, ни транспорт.
    [InlineData(SheetSlice.Items, new[] { "Skills", "Talents", "Mounts", "HeroicAbility" })]
    // Талантам — вообще ничего, кроме них самих.
    [InlineData(SheetSlice.Talents, new[] { "Items", "Attachments", "Mounts", "Skills" })]
    [InlineData(SheetSlice.Attachments, new[] { "Items", "Talents", "Mounts", "Skills" })]
    public void ASliceDoesNotLoadWhatItDoesNotNeed(SheetSlice slice, string[] absent)
    {
        using var db = RealProviderContext();

        // В дереве выражения видны все пути Include — до провайдера и до подключения к базе.
        var tree = db.SliceQuery(slice).Expression.ToString();

        foreach (var relation in absent)
            Assert.DoesNotContain(relation, tree, StringComparison.Ordinal);
    }

    /// <summary>
    /// Базовый лист всё же грузит предметы, улучшения и таланты, хотя и не отдаёт их: поглощение,
    /// защита и порог веса считаются из них. Если это уедет, числа станут молча неверными.
    /// </summary>
    [Fact]
    public void TheBaseSliceStillLoadsWhatItsNumbersAreComputedFrom()
    {
        using var db = RealProviderContext();

        var tree = db.SliceQuery(SheetSlice.Base).Expression.ToString();

        foreach (var relation in new[] { "Items", "Attachments", "Talents", "Skills" })
            Assert.Contains(relation, tree, StringComparison.Ordinal);
    }

    /// <summary>Коллекции персонажа не приезжают одной выборкой: перемножаться нечему.</summary>
    [Fact]
    public void CharacterCollectionsAreLoadedWithSeparateQueries()
    {
        using var db = RealProviderContext();

        var sql = db.WithRelations().AsNoTracking().ToQueryString();

        // Каждая из этих таблиц — коллекция на уровне персонажа. Приезжай они одной выборкой,
        // их строки перемножились бы между собой.
        foreach (var table in new[]
                 {
                     "CharacterSkills", "CharacterTalents", "CharacterItems",
                     "CharacterAttachments", "CharacterMounts", "CharacterCriticalInjuries",
                 })
            Assert.DoesNotContain(table, sql, StringComparison.Ordinal);
    }
}
