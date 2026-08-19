using GenesysForge.Application.Common;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

/// <summary>
/// Регрессия производительности tracking-запросов правок персонажа. Эти запросы находятся на
/// горячем пути UI и не должны незаметно вернуться к полному WithRelations().
/// </summary>
public class CharacterMutationQueryShapeTests
{
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

    [Fact]
    public void AddItemLoadsOnlyInventoryDataNeededByPurchaseRules()
    {
        using var db = RealProviderContext();

        // AddItemQuery в production остаётся split query. Для проверки формы полного Include-графа
        // тест временно переводит только диагностический запрос в single-query режим, потому что
        // ToQueryString() для split query показывает лишь первый SELECT.
        var sql = db.AddItemQuery(Guid.NewGuid(), needsEquipmentValidation: false)
            .AsSingleQuery()
            .Where(c => c.Id == Guid.NewGuid())
            .ToQueryString();

        Assert.Contains("\"CharacterItems\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ItemDefs\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"CharacterAttachments\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ItemDefId\"", sql, StringComparison.Ordinal);

        foreach (var relationTable in new[]
                 {
                     "CharacterSkills", "CharacterTalents", "CharacterMounts", "CharacterCriticalInjuries",
                     "CharacterHeroicSecondaryEffects", "CharacterHeroicConfigurations", "CharacterSignatureWeapons",
                     "WeaponAttackProfiles", "ItemQualityValues",
                 })
            Assert.DoesNotContain($"\"{relationTable}\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void AddItemLoadsEquippedRowsWhenSlotValidationIsRequired()
    {
        using var db = RealProviderContext();

        var itemDefId = Guid.NewGuid();
        var withoutSlotValidation = db.AddItemQuery(itemDefId, needsEquipmentValidation: false)
            .AsSingleQuery()
            .Where(c => c.Id == Guid.NewGuid())
            .ToQueryString();
        var withSlotValidation = db.AddItemQuery(itemDefId, needsEquipmentValidation: true)
            .AsSingleQuery()
            .Where(c => c.Id == Guid.NewGuid())
            .ToQueryString();

        // Оба варианта загружают сущность CharacterItem целиком, поэтому поле State будет в SELECT.
        // Нас интересует именно дополнительное условие фильтра Include для уже экипированных строк.
        Assert.Contains(" OR ", withSlotValidation, StringComparison.Ordinal);
        Assert.DoesNotContain(" OR ", withoutSlotValidation, StringComparison.Ordinal);
        Assert.NotEqual(withoutSlotValidation, withSlotValidation);
    }
}
