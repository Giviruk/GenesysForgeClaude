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
    public async Task SecondArmor_CannotBeWorn_SoStealthIsPenalisedOnce()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = reference.Items.Single(i => i.Name == "Plate" && i.Kind == ItemKind.Armor);
        var chainmail = reference.Items.Single(i => i.Name == "Chainmail" && i.Kind == ItemKind.Armor);

        await AddAsync(client, id, plate.Id);
        // Вторую броню надеть нельзя вовсе (ROT-EQP-01) — двойного штрафа не бывает по построению.
        var second = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(chainmail.Id, 1, ItemState.Equipped, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        await AddAsync(client, id, chainmail.Id, ItemState.Backpack);

        var sheet = await SheetAsync(client, id);

        // Штраф ровно от надетых лат; кольчуга в рюкзаке даёт только вес.
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

    [Fact]
    public async Task CriticalInjury_AddsItsLongTermCheckModifierToTheSkillSheet()
    {
        var (client, id, _) = await CreateCharacterAsync();
        var add = await client.PostAsJsonAsync($"/api/characters/{id}/critical-injuries",
            new AddCriticalInjuryRequest("crit-ci_046_050", null, null, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);

        var sheet = await SheetAsync(client, id);
        var knowledge = Skill(sheet, "Knowledge (Lore)");
        Assert.Equal(1, knowledge.DifficultyDice);
        var source = Assert.Single(knowledge.SetbackSources!.Where(s => s.SourceType == "CriticalInjury"));
        Assert.Equal("Звон в ушах", source.SourceNameRu);
        Assert.Equal(1, source.Difficulty);

        // Эффект травмы ограничен двумя характеристиками и не меняет ловкость.
        Assert.Equal(0, Skill(sheet, "Stealth").DifficultyDice);
    }
}
