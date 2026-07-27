using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-WPN-01: полные профили оружия. Проверяется каждая строка таблицы — навык, тип и значение
/// урона, критическое, дистанция и качества, — а также альтернативные профили одного экземпляра
/// и правило пики. Числа берутся из типизированного профиля, а не из строки «+3».
/// </summary>
public class RotWeaponProfileCatalogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Одна строка таблицы оружия в том виде, в каком её задаёт ТЗ.</summary>
    public sealed record WeaponRow(
        string Name, string Skill, DamageKind DamageKind, int Damage, int Crit, WeaponRange Range,
        string Qualities)
    {
        public override string ToString() => Name;
    }

    public static IEnumerable<WeaponRow> Expected =>
    [
        new("Axe", "Melee (Light)", DamageKind.BrawnPlus, 3, 3, WeaponRange.Engaged, "vicious 1"),
        new("Cestus", "Brawl", DamageKind.BrawnPlus, 1, 4, WeaponRange.Engaged, "disorient 3"),
        new("Dagger", "Melee (Light)", DamageKind.BrawnPlus, 2, 3, WeaponRange.Engaged, "accurate 1"),
        new("Flail", "Melee (Heavy)", DamageKind.BrawnPlus, 4, 3, WeaponRange.Engaged,
            "cumbersome 3, linked 1, unwieldy 3"),
        new("Greataxe", "Melee (Heavy)", DamageKind.BrawnPlus, 4, 3, WeaponRange.Engaged,
            "cumbersome 3, pierce 2, vicious 1"),
        new("Greatsword", "Melee (Heavy)", DamageKind.BrawnPlus, 4, 2, WeaponRange.Engaged,
            "defensive 1, pierce 1, unwieldy 3"),
        new("Halberd", "Melee (Heavy)", DamageKind.BrawnPlus, 3, 3, WeaponRange.Engaged,
            "defensive 1, pierce 3"),
        new("Katar", "Brawl", DamageKind.BrawnPlus, 1, 2, WeaponRange.Engaged, "accurate 1"),
        new("Mace", "Melee (Light)", DamageKind.BrawnPlus, 3, 4, WeaponRange.Engaged, ""),
        new("Military Pick", "Melee (Light)", DamageKind.BrawnPlus, 1, 2, WeaponRange.Engaged, "pierce 2"),
        new("Pike", "Melee (Heavy)", DamageKind.BrawnPlus, 4, 4, WeaponRange.Short, "prepare 1"),
        new("Shield", "Melee (Light)", DamageKind.BrawnPlus, 0, 6, WeaponRange.Engaged,
            "defensive 1, deflection 1, inaccurate 1, knockdown"),
        new("Shield, Large", "Melee (Light)", DamageKind.BrawnPlus, 1, 5, WeaponRange.Engaged,
            "defensive 2, deflection 2, inaccurate 2, knockdown"),
        new("Shield, Bulwark", "Melee (Light)", DamageKind.BrawnPlus, 2, 5, WeaponRange.Engaged,
            "cumbersome 4, defensive 2, deflection 3, inaccurate 2, knockdown, reinforced"),
        new("Spear", "Melee (Heavy)", DamageKind.BrawnPlus, 3, 3, WeaponRange.Engaged, "accurate 1"),
        new("Spear, Light", "Melee (Light)", DamageKind.BrawnPlus, 2, 4, WeaponRange.Engaged,
            "accurate 1, defensive 1"),
        new("Staff", "Melee (Heavy)", DamageKind.BrawnPlus, 2, 4, WeaponRange.Engaged, "defensive 1"),
        new("Sword", "Melee (Light)", DamageKind.BrawnPlus, 3, 2, WeaponRange.Engaged, "defensive 1"),
        new("War Hammer", "Melee (Heavy)", DamageKind.BrawnPlus, 5, 4, WeaponRange.Engaged,
            "concussive 1, cumbersome 4, inaccurate 1, knockdown"),
        new("Bow", "Ranged", DamageKind.Fixed, 7, 3, WeaponRange.Medium, "unwieldy 2"),
        new("Crossbow", "Ranged", DamageKind.Fixed, 7, 2, WeaponRange.Medium, "pierce 2, prepare 1"),
        new("Crossbow, Hand", "Ranged", DamageKind.Fixed, 5, 2, WeaponRange.Short, "pierce 1, prepare 1"),
        new("Crossbow, Heavy", "Ranged", DamageKind.Fixed, 8, 2, WeaponRange.Long,
            "cumbersome 3, pierce 3, prepare 2"),
        new("Crossbow, Repeating", "Ranged", DamageKind.Fixed, 6, 2, WeaponRange.Short, "linked 2, prepare 2"),
        new("Longbow", "Ranged", DamageKind.Fixed, 8, 3, WeaponRange.Long, "unwieldy 3"),
        new("Sling", "Ranged", DamageKind.Fixed, 4, 4, WeaponRange.Medium, "disorient 2, prepare 1"),
        new("Throwing Axe", "Ranged", DamageKind.BrawnPlus, 2, 3, WeaponRange.Short,
            "inaccurate 1, limited-ammo 1, vicious 1"),
    ];

    private async Task<ReferenceResponse> ReferenceAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        return (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
    }

    /// <summary>Качества в стабильном виде «код рейтинг», отсортированные по коду.</summary>
    private static string QualityText(IReadOnlyList<ItemQualityRefDto> qualities) =>
        string.Join(", ", qualities
            .Select(q => q.Rating is { } r ? $"{q.Code} {r}" : q.Code)
            .OrderBy(x => x, StringComparer.Ordinal));

    public static TheoryData<WeaponRow> Rows() => [.. Expected];

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task EveryWeapon_HasItsPublishedDefaultProfile(WeaponRow expected)
    {
        var reference = await ReferenceAsync();
        var weapon = reference.Items.Single(i => i.Name == expected.Name && i.Kind == ItemKind.Weapon);
        var profile = Assert.Single(weapon.AttackProfiles!, p => p.IsDefault);

        Assert.Equal(expected, new WeaponRow(
            weapon.Name, profile.SkillName, profile.DamageKind, profile.DamageValue, profile.Crit,
            profile.Range, QualityText(profile.Qualities)));
    }

    [Fact]
    public async Task DaggerAndLightSpear_CanBeThrown_WithTheirOwnNumbers()
    {
        var reference = await ReferenceAsync();

        var dagger = reference.Items.Single(i => i.Name == "Dagger" && i.Kind == ItemKind.Weapon);
        var daggerThrown = dagger.AttackProfiles!.Single(p => p.Code == "thrown");
        Assert.Equal("Ranged", daggerThrown.SkillName);
        Assert.Equal(DamageKind.BrawnPlus, daggerThrown.DamageKind);
        Assert.Equal(2, daggerThrown.DamageValue);
        Assert.Equal(3, daggerThrown.Crit);
        Assert.Equal(WeaponRange.Short, daggerThrown.Range);
        Assert.Equal("accurate 1, limited-ammo 1", QualityText(daggerThrown.Qualities));

        var spear = reference.Items.Single(i => i.Name == "Spear, Light" && i.Kind == ItemKind.Weapon);
        var spearThrown = spear.AttackProfiles!.Single(p => p.Code == "thrown");
        Assert.Equal(4, spearThrown.Crit);
        Assert.Equal("accurate 1, limited-ammo 1", QualityText(spearThrown.Qualities));
    }

    [Fact]
    public async Task ThrowingAxe_HeldProfile_LosesLimitedAmmoAndBecomesMelee()
    {
        var reference = await ReferenceAsync();
        var axe = reference.Items.Single(i => i.Name == "Throwing Axe" && i.Kind == ItemKind.Weapon);

        var held = axe.AttackProfiles!.Single(p => p.Code == "held");
        Assert.Equal("Melee (Light)", held.SkillName);
        Assert.Equal(WeaponRange.Engaged, held.Range);
        // Ограниченного боезапаса у профиля «в руке» нет: топорик остаётся в руке.
        Assert.Equal("inaccurate 1, vicious 1", QualityText(held.Qualities));
    }

    [Fact]
    public async Task Pike_DoesNotReachEngaged_AndCarriesItsOwnDifficulty()
    {
        var reference = await ReferenceAsync();
        var pike = reference.Items.Single(i => i.Name == "Pike" && i.Kind == ItemKind.Weapon);

        var profile = pike.AttackProfiles!.Single();
        Assert.True(profile.CannotAttackEngaged);
        Assert.Equal(2, profile.FixedDifficulty);
    }

    [Fact]
    public async Task Weapons_NoLongerCarryDefenseColumns_TheirQualitiesDoIt()
    {
        var reference = await ReferenceAsync();

        // Колонки защиты дублировали Defensive/Deflection: после ROT-WPN-01 источник ровно один,
        // иначе щит посчитался бы дважды.
        foreach (var weapon in reference.Items.Where(i => i.Kind == ItemKind.Weapon))
        {
            Assert.Equal(0, weapon.MeleeDefense);
            Assert.Equal(0, weapon.RangedDefense);
        }

        var shield = reference.Items.Single(i => i.Name == "Shield, Large" && i.Kind == ItemKind.Weapon);
        Assert.Contains(shield.Qualities, q => q.Code == "defensive" && q.Rating == 2);
        Assert.Contains(shield.Qualities, q => q.Code == "deflection" && q.Rating == 2);
    }

    [Fact]
    public async Task EveryWeapon_HasExactlyOneDefaultProfile()
    {
        var reference = await ReferenceAsync();

        foreach (var weapon in reference.Items.Where(i => i.Kind == ItemKind.Weapon))
            Assert.Single(weapon.AttackProfiles!, p => p.IsDefault);
    }
}
