using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// U-13 + ROT-CRE-03: взаимоисключающие режимы стартового снаряжения. В режиме стандартных денег
/// комплект не выдаётся; в режиме комплекта он выдаётся целиком и бюджета 500 нет.
/// </summary>
public class CreateCharacterCareerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, ReferenceResponse Reference)> SetupAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        return (client, reference);
    }

    /// <summary>Полный набор выборов комплекта карьеры: по первой опции каждой группы.</summary>
    private static List<CareerGearChoice> AllFirstOptions(CareerDto career) => career.StartingGear
        .Where(g => g.IsChoice)
        .GroupBy(g => g.ChoiceGroup)
        .Select(g => new CareerGearChoice(g.Key, g.Min(x => x.ChoiceOption)))
        .ToList();

    private static async Task<Guid> CreateOkAsync(HttpClient client, CreateCharacterRequest req)
    {
        var resp = await client.PostAsJsonAsync("/api/characters/", req, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    [Fact]
    public async Task StandardMoney_IsDefault_GivesBudgetAndPocketMoney_ButNoPackage()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.Name == "Warrior");

        // Поле режима не передано — старый клиент должен получить безопасный StandardMoney.
        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Воин", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null));

        var sheet = await SheetAsync(client, id);
        Assert.Equal(StartingEquipmentMode.StandardMoney, sheet.StartingEquipmentMode);
        Assert.Equal(500, sheet.StartingPurchaseBudget);
        Assert.InRange(sheet.Money, 1, 100); // карманные 1d100, а не 500 и не 500+1d100
        Assert.Empty(sheet.Items!);
    }

    [Fact]
    public async Task StandardMoney_WithPackageChoices_IsRejected()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.Name == "Warrior");
        var group = warrior.StartingGear.First(g => g.IsChoice).ChoiceGroup;

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Смешанный режим", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: [new CareerGearChoice(group, 0)],
            StartingEquipmentMode: StartingEquipmentMode.StandardMoney), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task CareerPackage_GivesWholePackageAndCareerMoney_ButNoBudget()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.Name == "Warrior");

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Воин с комплектом", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: AllFirstOptions(warrior),
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage));

        var sheet = await SheetAsync(client, id);
        Assert.Equal(StartingEquipmentMode.CareerPackage, sheet.StartingEquipmentMode);
        Assert.Equal(0, sheet.StartingPurchaseBudget); // бюджета 500 в этом режиме нет
        Assert.InRange(sheet.Money, 1, 100);           // 1d100 по формуле карьеры

        // Комплект Warrior: кожаная броня, 2 целебных эликсира, Traveling Gear и выбранный набор оружия.
        Assert.Contains(sheet.Items!, i => i.Name == "Leather");
        Assert.Contains(sheet.Items!, i => i.Name == "Health Elixir" && i.Quantity == 2);
        Assert.Contains(sheet.Items!, i => i.Name == "Backpack");
        Assert.Contains(sheet.Items!, i => i.Name == "Bedroll");
        Assert.DoesNotContain(sheet.Items!, i => i.Name == "Adventuring Pack");
    }

    [Fact]
    public async Task CareerPackage_PartialChoices_AreRejected()
    {
        var (client, reference) = await SetupAsync();
        var scout = reference.Careers.First(c => c.Name == "Scout");
        var slots = AllFirstOptions(scout);
        Assert.True(slots.Count > 1);

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Неполный комплект", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, scout.Id, null,
            CareerGearChoices: slots.Take(1).ToList(),
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("career.package.group_missing", error!.ReasonCode);
    }

    [Fact]
    public async Task CareerPackage_DuplicateGroup_IsRejected()
    {
        var (client, reference) = await SetupAsync();
        var scout = reference.Careers.First(c => c.Name == "Scout");
        var slots = AllFirstOptions(scout);
        var withDuplicate = slots.Concat([slots[0]]).ToList();

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Дубль группы", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, scout.Id, null,
            CareerGearChoices: withDuplicate,
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("career.package.group_duplicated", error!.ReasonCode);
    }

    [Fact]
    public async Task CareerPackage_UnknownGroupOrOption_IsRejected()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.Name == "Warrior");
        var slots = AllFirstOptions(warrior);

        var unknownGroup = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Чужая группа", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: [.. slots, new CareerGearChoice("slot-999", 0)],
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, unknownGroup.StatusCode);
        Assert.Equal("career.package.group_unknown",
            (await unknownGroup.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var unknownOption = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Чужой вариант", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: slots.Select(s => s with { OptionIndex = 999 }).ToList(),
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, unknownOption.StatusCode);
        Assert.Equal("career.package.option_unknown",
            (await unknownOption.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task InvalidPackageRequest_CreatesNothing()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.Name == "Warrior");

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Ничего не создаётся", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: [],
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var list = await client.GetFromJsonAsync<List<CharacterListItemDto>>("/api/characters", Json.Options);
        Assert.DoesNotContain(list!, c => c.Name == "Ничего не создаётся");
    }

    [Fact]
    public async Task EnvoyPackage_AddsFixedMoneyOnTopOfRoll()
    {
        var (client, reference) = await SetupAsync();
        var envoy = reference.Careers.First(c => c.Name == "Envoy"); // 200 + 1d100

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Посланник", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, envoy.Id, null,
            CareerGearChoices: AllFirstOptions(envoy),
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage));

        var sheet = await SheetAsync(client, id);
        Assert.InRange(sheet.Money, 201, 300);
    }

    /// <summary>ROT-CRE-04: обе ветки первой группы Scout дают ровно один комплект кожаной брони.</summary>
    [Theory]
    [InlineData(0, "Bow")]
    [InlineData(1, "Spear, Light")]
    public async Task ScoutPackage_EitherBranch_GivesExactlyOneLeatherArmor(int option, string expectedWeapon)
    {
        var (client, reference) = await SetupAsync();
        var scout = reference.Careers.First(c => c.Name == "Scout");
        var choices = AllFirstOptions(scout)
            .Select(c => c.ChoiceGroup == "slot-1" ? c with { OptionIndex = option } : c)
            .ToList();

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            $"Разведчик {option}", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, scout.Id, null,
            CareerGearChoices: choices,
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage));

        var sheet = await SheetAsync(client, id);
        var leather = sheet.Items!.Single(i => i.Name == "Leather");
        Assert.Equal(1, leather.Quantity);
        Assert.Contains(sheet.Items!, i => i.Name == expectedWeapon);
    }

    [Fact]
    public async Task TravelingGear_IsExpandedIntoRealItems_NotAnInventedBundle()
    {
        var (client, reference) = await SetupAsync();
        var scoundrel = reference.Careers.First(c => c.Name == "Scoundrel"); // Traveling Gear — фиксированный

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Пройдоха", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, scoundrel.Id, null,
            CareerGearChoices: AllFirstOptions(scoundrel),
            StartingEquipmentMode: StartingEquipmentMode.CareerPackage));

        var sheet = await SheetAsync(client, id);
        foreach (var name in new[] { "Backpack", "Bedroll", "Rope", "Flint and Steel", "Torches (3)", "Waterskin (Empty)" })
            Assert.Contains(sheet.Items!, i => i.Name == name);
        Assert.DoesNotContain(sheet.Items!, i => i.Name == "Adventuring Pack");
    }

    [Fact]
    public async Task RetiredAdventuringPack_IsNotOfferedInReference()
    {
        var (_, reference) = await SetupAsync();
        Assert.DoesNotContain(reference.Items, i => i.Name == "Adventuring Pack");
    }

    /// <summary>Бюджет создания тратится раньше кошелька и не смешивается с карманными деньгами.</summary>
    [Fact]
    public async Task StartingBudget_IsSpentBeforeWallet_AndRestoredOnSale()
    {
        var (client, reference) = await SetupAsync();
        var career = reference.Careers.First(c => c.Name == "Warrior");
        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Покупатель", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, career.Id, null));

        var before = await SheetAsync(client, id);
        var item = reference.Items.First(i => i.Price is > 0 and <= 100);

        var add = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(item.Id, 1, ItemState.Backpack), Json.Options);
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var afterBuy = await SheetAsync(client, id);
        Assert.Equal(before.StartingPurchaseBudget - item.Price, afterBuy.StartingPurchaseBudget);
        Assert.Equal(before.Money, afterBuy.Money); // кошелёк не тронут

        var bought = afterBuy.Items!.Single(i => i.ItemDefId == item.Id);
        var sell = await client.PostAsJsonAsync($"/api/characters/{id}/items/{bought.Id}/sell",
            new SellItemRequest(1, NetSuccesses: 3), Json.Options);
        sell.EnsureSuccessStatusCode();

        // Три успеха дают 75 % от цены (ROT-ECO-01), и вся выручка идёт в бюджет, а не в кошелёк.
        var proceeds = item.Price * 75 / 100;
        var afterSell = await SheetAsync(client, id);
        Assert.Equal(afterBuy.StartingPurchaseBudget + proceeds, afterSell.StartingPurchaseBudget);
        Assert.Equal(before.Money, afterSell.Money);
    }
}
