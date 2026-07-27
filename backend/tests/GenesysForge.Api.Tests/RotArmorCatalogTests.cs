using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-ARM-01: полная таблица брони RoT. Проверяется каждое значение строки — защита, поглощение,
/// вес в руках и на себе, слоты улучшений, цена, редкость и штраф к проверкам, — а не количество
/// записей. Заодно закреплён столбец HP оружия из таблицы ROT-WPN-01: он берётся из книги, а не
/// вычисляется из веса.
/// </summary>
public class RotArmorCatalogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Одна строка таблицы брони.</summary>
    /// <param name="Defense">Провайдер защиты по книге (общая защита).</param>
    /// <param name="WornEnc">Вес надетой брони: <c>max(0, Enc − 3)</c>.</param>
    /// <param name="StealthSetback">Помехи к Скрытности; 0 — штрафа нет.</param>
    public sealed record ArmorRow(
        string Name, int Defense, int Soak, int Enc, int WornEnc, int HardPoints, int Price, int Rarity,
        int StealthSetback)
    {
        public override string ToString() => Name;
    }

    public static IEnumerable<ArmorRow> Expected =>
    [
        new("Brigandine",  1, 1, 2, 0, 1,  400, 5, 0),
        new("Chainmail",   0, 2, 3, 0, 2,  550, 4, 1),
        new("Heavy Robes", 1, 0, 1, 0, 1,   45, 0, 0),
        new("Leather",     0, 1, 2, 0, 1,   50, 3, 0),
        new("Padded",      0, 1, 2, 0, 0,   35, 2, 0),
        new("Plate",       1, 2, 4, 1, 2, 1000, 6, 2),
        new("Scale",       0, 2, 4, 1, 1,  410, 4, 1),
    ];

    /// <summary>Столбец HP таблицы оружия RoT (ROT-WPN-01).</summary>
    public static IEnumerable<(string Name, int HardPoints)> ExpectedWeaponHardPoints =>
    [
        ("Axe", 1), ("Cestus", 0), ("Dagger", 1), ("Flail", 2), ("Greataxe", 2), ("Greatsword", 2),
        ("Halberd", 3), ("Katar", 1), ("Mace", 1), ("Military Pick", 1), ("Pike", 2), ("Shield", 1),
        ("Shield, Large", 2), ("Shield, Bulwark", 2), ("Spear", 1), ("Spear, Light", 1), ("Staff", 1),
        ("Sword", 1), ("War Hammer", 2), ("Bow", 1), ("Crossbow", 1), ("Crossbow, Hand", 0),
        ("Crossbow, Heavy", 2), ("Crossbow, Repeating", 2), ("Longbow", 2), ("Sling", 0), ("Throwing Axe", 1),
    ];

    private async Task<ReferenceResponse> ReferenceAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        return (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
    }

    public static TheoryData<ArmorRow> Rows() => [.. Expected];

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task EveryArmor_MatchesItsPublishedRow(ArmorRow expected)
    {
        var reference = await ReferenceAsync();
        var armor = reference.Items.Single(i => i.Name == expected.Name && i.Kind == ItemKind.Armor);
        var stealth = armor.CheckModifiers?
            .Where(m => m.SkillName == "Stealth" && m.Kind == CheckModifierKind.AddSetback)
            .Sum(m => m.Value) ?? 0;

        Assert.Equal(expected, new ArmorRow(
            armor.Name, armor.MeleeDefense, armor.SoakBonus, armor.Encumbrance,
            GenesysRules.WornArmorEncumbrance(armor.Encumbrance),
            armor.HardPoints ?? -1, armor.Price, armor.Rarity, stealth));
    }

    [Fact]
    public async Task ArmorDefense_IsGeneral_NotMeleeOnly()
    {
        var reference = await ReferenceAsync();

        // Броня защищает одинаково от ближних и дальних атак: разъехавшиеся колонки означали бы,
        // что латы прикрывают только от мечей.
        foreach (var armor in reference.Items.Where(i => i.Kind == ItemKind.Armor))
            Assert.Equal(armor.MeleeDefense, armor.RangedDefense);
    }

    [Fact]
    public async Task StealthPenalty_AppliesOnlyWhileWorn()
    {
        var reference = await ReferenceAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);

        var modifier = Assert.Single(plate.CheckModifiers!);
        Assert.True(modifier.RequiresWorn);
        Assert.Equal("", modifier.Condition);
    }

    [Fact]
    public async Task LightArmor_HasNoStealthPenalty()
    {
        var reference = await ReferenceAsync();

        foreach (var name in new[] { "Brigandine", "Heavy Robes", "Leather", "Padded" })
        {
            var armor = reference.Items.Single(i => i.Name == name && i.Kind == ItemKind.Armor);
            Assert.Empty(armor.CheckModifiers ?? []);
        }
    }

    public static TheoryData<string, int> WeaponHardPoints()
    {
        var data = new TheoryData<string, int>();
        foreach (var (name, hp) in ExpectedWeaponHardPoints) data.Add(name, hp);
        return data;
    }

    [Theory]
    [MemberData(nameof(WeaponHardPoints))]
    public async Task EveryWeapon_HasItsPublishedHardPoints(string name, int hardPoints)
    {
        var reference = await ReferenceAsync();
        var weapon = reference.Items.Single(i => i.Name == name && i.Kind == ItemKind.Weapon);

        Assert.Equal(hardPoints, weapon.HardPoints);
    }
}
