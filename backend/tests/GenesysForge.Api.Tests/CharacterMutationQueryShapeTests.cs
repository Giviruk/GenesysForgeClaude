using GenesysForge.Application.Common;
using GenesysForge.Infrastructure;
using GenesysForge.Infrastructure.Persistence;
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

        var tree = db.AddItemQuery(Guid.NewGuid(), needsEquipmentValidation: false)
            .Expression.ToString();

        Assert.Contains("Items", tree, StringComparison.Ordinal);
        Assert.Contains("Attachments", tree, StringComparison.Ordinal);
        Assert.Contains("ItemDefId", tree, StringComparison.Ordinal);

        foreach (var relation in new[]
                 {
                     "Skills", "Talents", "Mounts", "HeroicAbility", "CriticalInjuries",
                     "HeroicConfiguration", "SignatureWeapon", "AttackProfiles", "Qualities",
                 })
            Assert.DoesNotContain(relation, tree, StringComparison.Ordinal);
    }

    [Fact]
    public void AddItemLoadsEquippedRowsWhenSlotValidationIsRequired()
    {
        using var db = RealProviderContext();

        var tree = db.AddItemQuery(Guid.NewGuid(), needsEquipmentValidation: true)
            .Expression.ToString();

        Assert.Contains("State", tree, StringComparison.Ordinal);
        Assert.Contains("Equipped", tree, StringComparison.Ordinal);
    }
}
