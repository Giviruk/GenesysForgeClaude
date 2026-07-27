using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-WPN-02: качество изготовления. Проверяется каждая клетка таблицы по отдельности — и для
/// брони, и для оружия, — плюс полы, потолки и то, что типы не складываются.
/// </summary>
public class CraftsmanshipRulesTests
{
    private static EffectiveItemStats Armor(
        WeaponCraftsmanship craftsmanship, int enc = 4, int soak = 2, int def = 0, int? hp = 2,
        int price = 500, int rarity = 4) =>
        CraftsmanshipRules.For(ItemKind.Armor, craftsmanship, enc, soak, def, def, hp, price, rarity);

    private static EffectiveItemStats Weapon(
        WeaponCraftsmanship craftsmanship, int enc = 3, int? hp = 2, int price = 300, int rarity = 4) =>
        CraftsmanshipRules.For(ItemKind.Weapon, craftsmanship, enc, 0, 0, 0, hp, price, rarity);

    // ── Броня: вес, поглощение, защита, слоты ──

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 4)]
    [InlineData(WeaponCraftsmanship.Iron, 6)]
    [InlineData(WeaponCraftsmanship.Dwarven, 5)]
    [InlineData(WeaponCraftsmanship.Elven, 2)]
    [InlineData(WeaponCraftsmanship.Ancient, 4)]
    public void ArmorEncumbrance_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, Armor(craftsmanship).Encumbrance);

    [Fact]
    public void ElvenArmor_DoesNotDropEncumbranceBelowZero() =>
        Assert.Equal(0, Armor(WeaponCraftsmanship.Elven, enc: 1).Encumbrance);

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 2, 0)]
    [InlineData(WeaponCraftsmanship.Iron, 2, 0)]
    [InlineData(WeaponCraftsmanship.Dwarven, 2, 0)]
    [InlineData(WeaponCraftsmanship.Elven, 2, 0)]
    // Древняя броня — единственная, что поднимает и поглощение, и саму защиту провайдера.
    [InlineData(WeaponCraftsmanship.Ancient, 3, 1)]
    public void AncientArmor_RaisesSoakAndProvidedDefense(
        WeaponCraftsmanship craftsmanship, int soak, int defense)
    {
        var stats = Armor(craftsmanship);
        Assert.Equal(soak, stats.SoakBonus);
        Assert.Equal(defense, stats.MeleeDefense);
        Assert.Equal(defense, stats.RangedDefense);
    }

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 2)]
    [InlineData(WeaponCraftsmanship.Iron, 2)]
    [InlineData(WeaponCraftsmanship.Dwarven, 3)]
    [InlineData(WeaponCraftsmanship.Elven, 2)]
    [InlineData(WeaponCraftsmanship.Ancient, 1)]
    public void ArmorHardPoints_FollowTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, Armor(craftsmanship).HardPoints);

    [Fact]
    public void AncientHardPoints_DoNotGoBelowZero() =>
        Assert.Equal(0, Armor(WeaponCraftsmanship.Ancient, hp: 0).HardPoints);

    [Fact]
    public void UnknownHardPoints_StayUnknown() =>
        Assert.Null(Armor(WeaponCraftsmanship.Ancient, hp: null).HardPoints);

    // ── Оружие: вес, слоты, урон, крит ──

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 3)]
    [InlineData(WeaponCraftsmanship.Iron, 3)]
    [InlineData(WeaponCraftsmanship.Dwarven, 4)]
    [InlineData(WeaponCraftsmanship.Elven, 3)]
    [InlineData(WeaponCraftsmanship.Ancient, 3)]
    public void WeaponEncumbrance_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, Weapon(craftsmanship).Encumbrance);

    [Theory]
    // Гномий доспех получает слот, гномье оружие — нет: колонки таблицы разные.
    [InlineData(WeaponCraftsmanship.Dwarven, 2)]
    [InlineData(WeaponCraftsmanship.Ancient, 1)]
    [InlineData(WeaponCraftsmanship.Iron, 2)]
    public void WeaponHardPoints_FollowTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, Weapon(craftsmanship).HardPoints);

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 7)]
    [InlineData(WeaponCraftsmanship.Iron, 7)]
    [InlineData(WeaponCraftsmanship.Dwarven, 8)]
    [InlineData(WeaponCraftsmanship.Elven, 6)]
    [InlineData(WeaponCraftsmanship.Ancient, 8)]
    public void Damage_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, CraftsmanshipRules.Damage(7, craftsmanship));

    [Fact]
    public void ElvenDamage_FloorsAtOne()
    {
        // Пол считается по итогу — тому урону, который персонаж действительно наносит.
        Assert.Equal(1, CraftsmanshipRules.Damage(1, WeaponCraftsmanship.Elven));
        Assert.Equal(1, CraftsmanshipRules.Damage(0, WeaponCraftsmanship.Elven));
    }

    [Fact]
    public void ZeroDamage_StaysZero_WithoutElvenWork() =>
        Assert.Equal(0, CraftsmanshipRules.Damage(0, WeaponCraftsmanship.Steel));

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 3)]
    [InlineData(WeaponCraftsmanship.Iron, 4)]
    [InlineData(WeaponCraftsmanship.Dwarven, 3)]
    [InlineData(WeaponCraftsmanship.Elven, 2)]
    [InlineData(WeaponCraftsmanship.Ancient, 2)]
    public void Crit_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, CraftsmanshipRules.Crit(3, craftsmanship));

    [Theory]
    [InlineData(WeaponCraftsmanship.Elven)]
    [InlineData(WeaponCraftsmanship.Ancient)]
    public void Crit_FloorsAtOne(WeaponCraftsmanship craftsmanship) =>
        Assert.Equal(1, CraftsmanshipRules.Crit(1, craftsmanship));

    // ── Цена и редкость ──

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 300)]
    [InlineData(WeaponCraftsmanship.Iron, 150)]
    [InlineData(WeaponCraftsmanship.Dwarven, 600)]
    [InlineData(WeaponCraftsmanship.Elven, 600)]
    [InlineData(WeaponCraftsmanship.Ancient, 6000)]
    public void Price_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, CraftsmanshipRules.Price(300, craftsmanship));

    [Theory]
    [InlineData(75, 37)]
    [InlineData(1, 0)]
    [InlineData(0, 0)]
    public void IronPrice_RoundsDown(int basePrice, int expected) =>
        Assert.Equal(expected, CraftsmanshipRules.Price(basePrice, WeaponCraftsmanship.Iron));

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, 4)]
    [InlineData(WeaponCraftsmanship.Iron, 3)]
    [InlineData(WeaponCraftsmanship.Dwarven, 6)]
    [InlineData(WeaponCraftsmanship.Elven, 7)]
    [InlineData(WeaponCraftsmanship.Ancient, 10)]
    public void Rarity_FollowsTheTable(WeaponCraftsmanship craftsmanship, int expected) =>
        Assert.Equal(expected, CraftsmanshipRules.Rarity(4, craftsmanship));

    [Fact]
    public void Rarity_StaysInsideZeroToTen()
    {
        Assert.Equal(0, CraftsmanshipRules.Rarity(0, WeaponCraftsmanship.Iron));
        Assert.Equal(10, CraftsmanshipRules.Rarity(9, WeaponCraftsmanship.Dwarven));
        Assert.Equal(10, CraftsmanshipRules.Rarity(8, WeaponCraftsmanship.Elven));
    }

    [Fact]
    public void AncientRarity_IsExactlyTen_NotAShift()
    {
        // Ancient задаёт значение, а не сдвигает: и у дешёвой дубины, и у редчайшего клинка — 10.
        Assert.Equal(10, CraftsmanshipRules.Rarity(0, WeaponCraftsmanship.Ancient));
        Assert.Equal(10, CraftsmanshipRules.Rarity(10, WeaponCraftsmanship.Ancient));
    }

    // ── Помехи ──

    [Fact]
    public void IronArmor_AddsSetbackToFourSkills()
    {
        var modifiers = CraftsmanshipRules.CheckModifiers(ItemKind.Armor, WeaponCraftsmanship.Iron);
        Assert.Equal(
            ["Athletics", "Coordination", "Riding", "Stealth"],
            modifiers.Select(m => m.SkillName).Order(StringComparer.Ordinal));
        Assert.All(modifiers, m =>
        {
            Assert.Equal(CheckModifierKind.AddSetback, m.Kind);
            Assert.Equal(1, m.Value);
        });
    }

    [Fact]
    public void ElvenArmor_RemovesOneStealthSetback()
    {
        var modifier = Assert.Single(
            CraftsmanshipRules.CheckModifiers(ItemKind.Armor, WeaponCraftsmanship.Elven));
        Assert.Equal(CheckModifierKind.RemoveSetback, modifier.Kind);
        Assert.Equal("Stealth", modifier.SkillName);
        Assert.Equal(1, modifier.Value);
    }

    [Theory]
    [InlineData(WeaponCraftsmanship.Iron)]
    [InlineData(WeaponCraftsmanship.Elven)]
    public void Weapons_NeverCarryTheArmorPenalties(WeaponCraftsmanship craftsmanship) =>
        Assert.Empty(CraftsmanshipRules.CheckModifiers(ItemKind.Weapon, craftsmanship));

    // ── Укреплённость, применимость и порядок ──

    [Theory]
    [InlineData(WeaponCraftsmanship.Steel, false)]
    [InlineData(WeaponCraftsmanship.Iron, false)]
    [InlineData(WeaponCraftsmanship.Dwarven, false)]
    [InlineData(WeaponCraftsmanship.Elven, false)]
    [InlineData(WeaponCraftsmanship.Ancient, true)]
    public void OnlyAncientWork_IsReinforced(WeaponCraftsmanship craftsmanship, bool expected)
    {
        Assert.Equal(expected, CraftsmanshipRules.IsReinforced(craftsmanship));
        Assert.Equal(expected, Armor(craftsmanship).Reinforced);
        Assert.Equal(expected, Weapon(craftsmanship).Reinforced);
    }

    [Fact]
    public void Gear_TakesNoCraftsmanship()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            CraftsmanshipRules.EnsureApplicable(ItemKind.Gear, WeaponCraftsmanship.Elven));
        Assert.Equal("item.craftsmanship.not_applicable", ex.ReasonCode);
        CraftsmanshipRules.EnsureApplicable(ItemKind.Gear, WeaponCraftsmanship.Steel);
    }

    [Fact]
    public void UnknownCraftsmanship_IsRejected()
    {
        var ex = Assert.Throws<DomainRuleException>(() =>
            CraftsmanshipRules.EnsureApplicable(ItemKind.Weapon, (WeaponCraftsmanship)99));
        Assert.Equal("item.craftsmanship.unknown", ex.ReasonCode);
    }

    [Fact]
    public void GearKeepsCatalogNumbers_EvenIfCraftsmanshipSlipsThrough()
    {
        // Снаряжение отклоняется на входе; если тип всё-таки дошёл — числа каталога не меняются.
        var stats = CraftsmanshipRules.For(ItemKind.Gear, WeaponCraftsmanship.Ancient, 2, 0, 0, 0, null, 50, 3);
        Assert.Equal(2, stats.Encumbrance);
        Assert.Equal(50, stats.Price);
        Assert.Equal(3, stats.Rarity);
        Assert.False(stats.Reinforced);
        Assert.Empty(stats.Adjustments);
    }

    [Fact]
    public void SteelWork_ChangesNothing()
    {
        var stats = Armor(WeaponCraftsmanship.Steel);
        Assert.Equal(4, stats.Encumbrance);
        Assert.Equal(500, stats.Price);
        Assert.Empty(stats.Adjustments);
        Assert.Empty(stats.CheckModifiers);
    }

    [Fact]
    public void CraftsmanshipDoesNotStack_ItReplaces()
    {
        // Древняя работа поверх гномьей — это древняя работа: считается от чисел каталога,
        // а не от уже изменённых гномьими правилами.
        var dwarven = Armor(WeaponCraftsmanship.Dwarven);
        var ancient = CraftsmanshipRules.For(
            ItemKind.Armor, WeaponCraftsmanship.Ancient, 4, 2, 0, 0, 2, 500, 4);
        Assert.Equal(5, dwarven.Encumbrance);
        Assert.Equal(4, ancient.Encumbrance);
        Assert.Equal(1, ancient.HardPoints);
    }

    [Fact]
    public void Adjustments_ExplainEveryChangedNumber()
    {
        var stats = Armor(WeaponCraftsmanship.Iron);
        var enc = Assert.Single(stats.Adjustments, a => a.Field == "encumbrance");
        Assert.Equal(4, enc.Base);
        Assert.Equal(6, enc.Effective);
        Assert.Equal(ItemStatStage.Craftsmanship, enc.Stage);
        Assert.Equal(nameof(WeaponCraftsmanship.Iron), enc.Source);
        // Неизменившиеся характеристики в разбор не попадают: «поглощение 2 → 2» ничего не объясняет.
        Assert.DoesNotContain(stats.Adjustments, a => a.Field == "soak");
    }

    [Fact]
    public void CatalogFixedCraftsmanship_IsLookedUpByCode_NotByName()
    {
        // Таблица уникальных записей пока пуста: выводить работу разбором названия запрещено.
        Assert.Null(CraftsmanshipRules.FixedFor("rot.item.sword"));
        Assert.Null(CraftsmanshipRules.FixedFor("rot.item.elven-blade"));
        Assert.Null(CraftsmanshipRules.FixedFor(null));
    }
}
