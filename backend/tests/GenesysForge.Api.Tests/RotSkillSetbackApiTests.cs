using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-ARM-01 + ROT-EQP-01 на листе: помехи от снаряжения и перегруза приезжают к каждому навыку,
/// а не остаются в описании предмета. Именно это число подставляет роллер.
/// </summary>
public class RotSkillSetbackApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
            "Латник", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<Guid> AddAsync(
        HttpClient client, Guid characterId, Guid itemDefId, ItemState state = ItemState.Equipped, int quantity = 1)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, quantity, state, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    [Fact]
    public async Task PlateArmor_AddsTwoSetbackToStealth_AndNothingToOtherSkills()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);

        var before = await SheetAsync(client, id);
        Assert.Equal(0, Skill(before, "Stealth").SetbackDice);

        await AddAsync(client, id, plate.Id);
        var after = await SheetAsync(client, id);

        var stealth = Skill(after, "Stealth");
        Assert.Equal(2, stealth.SetbackDice);
        var source = Assert.Single(stealth.SetbackSources!);
        Assert.Equal("Item", source.SourceType);
        Assert.Equal("Plate", source.SourceName);
        // Прочие проверки латы не трогают.
        Assert.Equal(0, Skill(after, "Vigilance").SetbackDice);
    }

    [Fact]
    public async Task ArmorPenalty_DisappearsWhenTheArmorIsTakenOff()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);
        var itemId = await AddAsync(client, id, plate.Id);

        var resp = await client.PatchAsJsonAsync($"/api/characters/{id}/items/{itemId}",
            new UpdateItemRequest(ItemState.Backpack, null), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(0, Skill(sheet, "Stealth").SetbackDice);
        Assert.Empty(Skill(sheet, "Stealth").SetbackSources!);
    }

    [Fact]
    public async Task InactiveWornArmor_DoesNotPenaliseStealthTwice()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);
        var chainmail = reference.Items.Single(i => i.Name == "Chainmail" && i.Kind == ItemKind.Armor);

        await AddAsync(client, id, plate.Id);
        await AddAsync(client, id, chainmail.Id);

        var sheet = await SheetAsync(client, id);

        // Активна первая надетая (латы): штраф ровно её, кольчуга даёт только вес (ROT-CMB-02).
        var stealth = Skill(sheet, "Stealth");
        Assert.Equal(2, stealth.SetbackDice);
        Assert.Equal(["Plate"], stealth.SetbackSources!.Select(s => s.SourceName));
    }

    [Fact]
    public async Task Overload_AddsSetbackToBrawnAndAgilityChecks_AndStacksWithArmor()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);
        // Тяжёлый предмет в рюкзаке: перегруз без изменения набора надетого.
        var heavy = reference.Items
            .Where(i => i.Kind != ItemKind.Armor && i.Encumbrance >= 4)
            .OrderByDescending(i => i.Encumbrance).First();

        await AddAsync(client, id, plate.Id);
        await AddAsync(client, id, heavy.Id, ItemState.Backpack, quantity: 10);

        var sheet = await SheetAsync(client, id);
        var overload = sheet.Derived.Encumbrance!.SetbackDice;
        Assert.True(overload > 0, "Предмет должен был создать перегруз.");

        // Скрытность — проверка Ловкости: складываются перегруз и штраф лат.
        var stealth = Skill(sheet, "Stealth");
        Assert.Equal(overload + 2, stealth.SetbackDice);
        Assert.Equal(["Item", "Encumbrance"], stealth.SetbackSources!.Select(s => s.SourceType));

        // Проверка Интеллекта перегрузом не штрафуется.
        Assert.Equal(0, Skill(sheet, "Knowledge (Lore)").SetbackDice);
    }
}
