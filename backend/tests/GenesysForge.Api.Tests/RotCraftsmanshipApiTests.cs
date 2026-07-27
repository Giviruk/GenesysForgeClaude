using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-WPN-02 на листе: качество изготовления экземпляра меняет вес, поглощение, защиту, слоты,
/// урон, крит, цену и редкость — и всё это считает сервер, а не клиент.
/// </summary>
public class RotCraftsmanshipApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static CharacterSkillDto Skill(CharacterSheetDto sheet, string name) =>
        sheet.Skills.Single(s => s.Name == name);

    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateCharacterAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Кузнец", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<Guid> AddAsync(
        HttpClient client, Guid characterId, Guid itemDefId,
        WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Steel,
        ItemState state = ItemState.Equipped)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, state, Free: true, Craftsmanship: craftsmanship), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static ItemDefDto Item(ReferenceResponse reference, string name, ItemKind kind) =>
        reference.Items.Single(i => i.Name == name && i.Kind == kind);

    [Fact]
    public async Task IronArmor_IsHeavier_Cheaper_AndHindersFourSkills()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        await AddAsync(client, id, plate.Id, WeaponCraftsmanship.Iron);

        var sheet = await SheetAsync(client, id);
        var item = sheet.Items.Single(i => i.ItemDefId == plate.Id);

        Assert.Equal(WeaponCraftsmanship.Iron, item.Craftsmanship);
        Assert.Equal(plate.Encumbrance + 2, item.Encumbrance);
        Assert.Equal(plate.Price / 2, item.Price);
        Assert.Equal(Math.Max(0, plate.Rarity - 1), item.Rarity);

        // Латы сами дают 2 помехи Скрытности (ROT-ARM-01), железная работа добавляет третью.
        Assert.Equal(3, Skill(sheet, "Stealth").SetbackDice);
        Assert.Equal(1, Skill(sheet, "Athletics").SetbackDice);
        Assert.Equal(1, Skill(sheet, "Coordination").SetbackDice);
        Assert.Equal(1, Skill(sheet, "Riding").SetbackDice);
        // Проверок, которых в правиле нет, железо не касается.
        Assert.Equal(0, Skill(sheet, "Vigilance").SetbackDice);
    }

    [Fact]
    public async Task ElvenArmor_IsLighter_AndRemovesOneStealthSetback()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        await AddAsync(client, id, plate.Id, WeaponCraftsmanship.Elven);

        var sheet = await SheetAsync(client, id);
        var item = sheet.Items.Single(i => i.ItemDefId == plate.Id);

        Assert.Equal(plate.Encumbrance - 2, item.Encumbrance);
        Assert.Equal(plate.Price * 2, item.Price);
        // Латы дают 2 помехи, эльфийская работа снимает одну.
        Assert.Equal(1, Skill(sheet, "Stealth").SetbackDice);
    }

    [Fact]
    public async Task AncientArmor_RaisesSoakAndDefense_IsReinforced_AndCostsTwentyTimes()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);

        var before = await SheetAsync(client, id);
        await AddAsync(client, id, plate.Id, WeaponCraftsmanship.Ancient);
        var after = await SheetAsync(client, id);

        var item = after.Items.Single(i => i.ItemDefId == plate.Id);
        Assert.True(item.Reinforced);
        Assert.Equal(plate.Price * 20, item.Price);
        Assert.Equal(10, item.Rarity);
        Assert.Equal(plate.SoakBonus + 1, item.SoakBonus);
        Assert.Equal(plate.HardPoints - 1, item.HardPoints);

        // Поглощение и защита персонажа поднимаются вместе с бронёй, а не только на карточке:
        // древняя работа прибавляет единицу к тому, что даёт сама броня.
        Assert.Equal(before.Derived.Soak + plate.SoakBonus + 1, after.Derived.Soak);
        Assert.Equal(plate.MeleeDefense + 1, after.Derived.MeleeDefense);
        Assert.Equal(plate.RangedDefense + 1, after.Derived.RangedDefense);
        Assert.Equal(0, before.Derived.MeleeDefense);
    }

    [Fact]
    public async Task DwarvenWeapon_HitsHarder_AndWeighsMore()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        await AddAsync(client, id, sword.Id, WeaponCraftsmanship.Dwarven);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var item = sheet.Items.Single(i => i.ItemDefId == sword.Id);
        var profile = item.AttackProfiles!.Single(p => p.IsDefault);

        // Меч — «Мощь +3», крит 2 (ROT-WPN-01): гномья работа поднимает урон и не трогает крит.
        Assert.Equal(brawn + 4, profile.BaseDamage);
        Assert.Equal(2, profile.Crit);
        Assert.Equal(sword.Encumbrance + 1, item.Encumbrance);
    }

    [Fact]
    public async Task ElvenWeapon_HitsSofter_ButCritsBetter()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        await AddAsync(client, id, sword.Id, WeaponCraftsmanship.Elven);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var profile = sheet.Items.Single(i => i.ItemDefId == sword.Id).AttackProfiles!
            .Single(p => p.IsDefault);

        Assert.Equal(brawn + 2, profile.BaseDamage);
        Assert.Equal(1, profile.Crit);
    }

    [Fact]
    public async Task IronWeapon_CritsWorse_AndCostsHalf()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        await AddAsync(client, id, sword.Id, WeaponCraftsmanship.Iron);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var item = sheet.Items.Single(i => i.ItemDefId == sword.Id);
        var profile = item.AttackProfiles!.Single(p => p.IsDefault);

        Assert.Equal(brawn + 3, profile.BaseDamage);
        Assert.Equal(3, profile.Crit);
        Assert.Equal(sword.Price / 2, item.Price);
        // Железо не трогает вес оружия — только брони.
        Assert.Equal(sword.Encumbrance, item.Encumbrance);
    }

    [Fact]
    public async Task AlternateProfiles_GetTheSameCraftsmanship()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var dagger = Item(reference, "Dagger", ItemKind.Weapon);
        await AddAsync(client, id, dagger.Id, WeaponCraftsmanship.Dwarven);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var profiles = sheet.Items.Single(i => i.ItemDefId == dagger.Id).AttackProfiles!;

        // Кинжал метают тем же кинжалом: гномья работа действует на оба профиля.
        Assert.All(profiles, p => Assert.Equal(brawn + 3, p.BaseDamage));
    }

    [Fact]
    public async Task ServerChargesTheCraftsmanshipPrice_NotTheCatalogOne()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);

        var before = await SheetAsync(client, id);
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(sword.Id, 1, ItemState.Carried,
                Craftsmanship: WeaponCraftsmanship.Iron), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var after = await SheetAsync(client, id);
        var spent = (before.Money + before.StartingPurchaseBudget)
            - (after.Money + after.StartingPurchaseBudget);
        Assert.Equal(sword.Price / 2, spent);
    }

    [Fact]
    public async Task SaleProceeds_FollowTheInstancePrice()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var itemId = await AddAsync(client, id, sword.Id, WeaponCraftsmanship.Dwarven, ItemState.Carried);

        var before = await SheetAsync(client, id);
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items/{itemId}/sell",
            new SellItemRequest(1), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var after = await SheetAsync(client, id);
        var gained = (after.Money + after.StartingPurchaseBudget)
            - (before.Money + before.StartingPurchaseBudget);
        Assert.Equal(sword.Price * 2, gained);
    }

    [Fact]
    public async Task Gear_RejectsCraftsmanship()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var gear = reference.Items.First(i => i.Kind == ItemKind.Gear && !i.IsCustom);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(gear.Id, 1, ItemState.Carried, Free: true,
                Craftsmanship: WeaponCraftsmanship.Elven), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task WithoutAChoice_TheItemIsOrdinaryWork()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        await AddAsync(client, id, plate.Id);

        var sheet = await SheetAsync(client, id);
        var item = sheet.Items.Single(i => i.ItemDefId == plate.Id);

        Assert.Equal(WeaponCraftsmanship.Steel, item.Craftsmanship);
        Assert.Equal(plate.Encumbrance, item.Encumbrance);
        Assert.Equal(plate.Price, item.Price);
        Assert.False(item.Reinforced);
        Assert.Empty(item.Adjustments!);
    }

    [Fact]
    public async Task Adjustments_ShowWhatChangedAndFromWhat()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        await AddAsync(client, id, plate.Id, WeaponCraftsmanship.Iron);

        var sheet = await SheetAsync(client, id);
        var item = sheet.Items.Single(i => i.ItemDefId == plate.Id);

        var enc = Assert.Single(item.Adjustments!, a => a.Field == "encumbrance");
        Assert.Equal(plate.Encumbrance, enc.Base);
        Assert.Equal(plate.Encumbrance + 2, enc.Effective);
        Assert.Equal(ItemStatStage.Craftsmanship, enc.Stage);
    }
}
