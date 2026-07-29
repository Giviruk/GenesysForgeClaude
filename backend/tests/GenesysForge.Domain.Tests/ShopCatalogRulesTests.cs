using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class ShopCatalogRulesTests
{
    private static ItemDef Item(string code, ItemKind kind = ItemKind.Gear, string skill = "") =>
        new()
        {
            Id = Guid.NewGuid(),
            Code = $"rot.item.{code}",
            Name = code,
            Kind = kind,
            SkillName = skill,
        };

    [Theory]
    [InlineData("ale-flagon")]
    [InlineData("lodging-common-room-1-night")]
    [InlineData("lodging-private-room-1-night")]
    [InlineData("meal-tavern")]
    [InlineData("porter-per-day")]
    [InlineData("torchbearer-per-day")]
    [InlineData("travel-riverboat-1-day")]
    [InlineData("travel-wagon-1-day")]
    [InlineData("wine-bottle")]
    public void ServicesHaveTheirOwnCategory(string code)
    {
        var item = Item(code);
        Assert.True(ShopCatalogRules.IsService(item.Code));
        Assert.Equal(ShopItemCategory.Service, ShopCatalogRules.Category(item));
    }

    [Theory]
    [InlineData("war-mount")]
    [InlineData("wagon")]
    [InlineData("saddlebags")]
    public void TransportUsesStableCodes(string code) =>
        Assert.Equal(ShopItemCategory.Transport, ShopCatalogRules.Category(Item(code)));

    [Theory]
    [InlineData("dagger", "Melee (Light)", ShopItemCategory.WeaponLight)]
    [InlineData("greatsword", "Melee (Heavy)", ShopItemCategory.WeaponHeavy)]
    [InlineData("longbow", "Ranged", ShopItemCategory.WeaponRanged)]
    [InlineData("rifle", "Ranged (Heavy)", ShopItemCategory.WeaponRanged)]
    [InlineData("cannon", "Gunnery", ShopItemCategory.WeaponRanged)]
    public void WeaponsUseTheirStructuredSkill(
        string code, string skill, ShopItemCategory expected) =>
        Assert.Equal(expected, ShopCatalogRules.Category(Item(code, ItemKind.Weapon, skill)));

    [Fact]
    public void ConsumableAndImplementDoNotFallBackToGear()
    {
        Assert.Equal(ShopItemCategory.Consumable,
            ShopCatalogRules.Category(Item("health-elixir")));
        Assert.Equal(ShopItemCategory.MagicImplement,
            ShopCatalogRules.Category(Item("magic-staff")));
    }
}
