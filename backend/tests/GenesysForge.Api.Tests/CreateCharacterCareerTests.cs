using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>U-13: стартовое снаряжение/деньги карьеры при создании персонажа.</summary>
public class CreateCharacterCareerTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CreateCharacterCareerTests(ApiFactory factory) => _factory = factory;

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, ReferenceResponse Reference)> SetupAsync()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        return (client, reference);
    }

    [Fact]
    public async Task FixedGear_AddedToInventory_AndMoneyRolled()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.NameRu == "Воин"); // 1d100 серебра, фикс-броня + 2 эликсира
        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Воин", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = await SheetAsync(client, id);
        Assert.InRange(sheet.Money, 1, 100); // 0 + 1d100
        // фиксированное снаряжение в инвентаре, включая стопку из 2 эликсиров
        Assert.NotEmpty(sheet.Items);
        Assert.Contains(sheet.Items, i => i.Quantity == 2);
    }

    [Fact]
    public async Task GearChoice_AppliesSelectedOptionItems()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.NameRu == "Воин");
        var fixedCodes = warrior.StartingGear.Where(g => !g.IsChoice).Select(g => g.ItemCode).ToHashSet();
        var group = warrior.StartingGear.First(g => g.IsChoice).ChoiceGroup;
        var opt0 = warrior.StartingGear.Where(g => g.IsChoice && g.ChoiceGroup == group && g.ChoiceOption == 0)
            .Select(g => g.ItemCode).ToHashSet();
        var expected = fixedCodes.Union(opt0).Count();

        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Воин-выбор", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: [new CareerGearChoice(group, 0)]), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = await SheetAsync(client, id);
        Assert.Equal(expected, sheet.Items.Count); // фикс + выбранный вариант
    }

    [Fact]
    public async Task GearChoice_InvalidOption_Rejected()
    {
        var (client, reference) = await SetupAsync();
        var warrior = reference.Careers.First(c => c.NameRu == "Воин");
        var group = warrior.StartingGear.First(g => g.IsChoice).ChoiceGroup;

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Плохой выбор", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, warrior.Id, null,
            CareerGearChoices: [new CareerGearChoice(group, 999)]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task FixedMoney_AddedOnTopOfRoll()
    {
        var (client, reference) = await SetupAsync();
        var envoy = reference.Careers.First(c => c.NameRu == "Посланник"); // 200 + 1d100
        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Посланник", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, envoy.Id, null), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = await SheetAsync(client, id);
        Assert.InRange(sheet.Money, 201, 300);
    }
}
