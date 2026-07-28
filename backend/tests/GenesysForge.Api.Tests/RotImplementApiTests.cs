using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-MAG-IMP-01 и ROT-MAG-MAT-01 на листе: материал экземпляра меняет цену и редкость, инструмент
/// приезжает вместе со своей механикой, а фолиант и палочка не работают, пока их не настроил ведущий.
/// </summary>
public class RotImplementApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

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
            "Чародейка", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static ItemDefDto Implement(ReferenceResponse reference, string code) =>
        reference.Items.Single(i => i.Implement?.Code == code);

    private static async Task<Guid> AddAsync(
        HttpClient client, Guid characterId, Guid itemDefId,
        ImplementMaterial material = ImplementMaterial.Oak, bool free = true)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, ItemState.Equipped, Free: free, Material: material),
            Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static int Funds(CharacterSheetDto sheet) => sheet.Money + sheet.StartingPurchaseBudget;

    private static Task SetMoneyAsync(HttpClient client, Guid characterId, int money) =>
        client.PatchAsJsonAsync($"/api/characters/{characterId}",
            new UpdateCharacterRequest(null, null, null, null, money), Json.Options);

    // ── Каталог и материал ──

    [Fact]
    public async Task Catalogue_KeepsAllSixImplementsWithBookNumbers()
    {
        var (_, _, reference) = await CreateCharacterAsync();
        foreach (var spec in ImplementRules.All)
        {
            var def = Implement(reference, spec.Code);
            Assert.Equal(spec.Price, def.Price);
            Assert.Equal(spec.Rarity, def.Rarity);
            Assert.Equal(spec.Encumbrance, def.Encumbrance);
        }
    }

    [Theory]
    [InlineData(ImplementMaterial.Oak, 400, 6)]
    [InlineData(ImplementMaterial.Bone, 600, 8)]
    [InlineData(ImplementMaterial.Hazel, 600, 7)]
    [InlineData(ImplementMaterial.Willow, 800, 8)]
    [InlineData(ImplementMaterial.Yew, 600, 7)]
    public async Task Material_ChangesInstancePriceAndRarity(
        ImplementMaterial material, int price, int rarity)
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var staff = Implement(reference, "magic-staff");
        var itemId = await AddAsync(client, id, staff.Id, material);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId);
        Assert.Equal(material, item.Implement!.Material);
        Assert.Equal(price, item.Price);
        Assert.Equal(rarity, item.Rarity);
    }

    [Fact]
    public async Task Purchase_ChargesTheMaterialPrice_NotTheCataloguePrice()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var staff = Implement(reference, "magic-staff");
        await SetMoneyAsync(client, id, 2000);
        var before = Funds(await SheetAsync(client, id));

        await AddAsync(client, id, staff.Id, ImplementMaterial.Willow, free: false);

        // Ива — вдвое дороже каталожной цены, и списывает сервер именно её.
        Assert.Equal(before - staff.Price * 2, Funds(await SheetAsync(client, id)));
    }

    [Fact]
    public async Task Material_OnAnOrdinaryItem_IsRejected()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = reference.Items.Single(i => i.Name == "Sword" && i.Kind == ItemKind.Weapon);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(sword.Id, 1, ItemState.Equipped, Free: true,
                Material: ImplementMaterial.Willow), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Empty((await SheetAsync(client, id)).Items);
    }

    [Fact]
    public async Task OrdinaryItem_CarriesNoImplementBlock()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = reference.Items.Single(i => i.Name == "Sword" && i.Kind == ItemKind.Weapon);
        var itemId = await AddAsync(client, id, sword.Id);

        Assert.Null((await SheetAsync(client, id)).Items.Single(i => i.Id == itemId).Implement);
    }

    // ── Механика инструмента на листе ──

    [Fact]
    public async Task Implement_CarriesItsMechanicsWithTheSheet()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var scepterId = await AddAsync(client, id, Implement(reference, "magic-scepter").Id);

        var implement = (await SheetAsync(client, id)).Items.Single(i => i.Id == scepterId).Implement!;
        Assert.Equal("magic-scepter", implement.Code);
        Assert.Equal(2, implement.AttackDamageBonus);
        Assert.Equal(1, implement.BoostDice);
        Assert.Contains("Close Combat", implement.DiscountEffects);
        Assert.False(implement.Pending); // скипетр настраивать не нужно
    }

    [Fact]
    public async Task MusicalInstrument_NamesItsOwnMagicSkill()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddAsync(client, id, Implement(reference, "musical-instrument").Id);

        var implement = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId).Implement!;
        Assert.Equal("Verse", implement.RequiredMagicSkill);
        Assert.Contains("Additional Target", implement.DiscountEffects);
    }

    // ── Настройка ведущим ──

    [Fact]
    public async Task TomeAndWand_ArrivePending_AndWorkOnlyAfterTheGmConfiguresThem()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var tomeId = await AddAsync(client, id, Implement(reference, "magic-tome").Id);

        var pending = (await SheetAsync(client, id)).Items.Single(i => i.Id == tomeId).Implement!;
        Assert.True(pending.Pending);
        Assert.Empty(pending.ChosenEffects);
        Assert.Equal(2, pending.ChoiceCount);
        Assert.Equal(3, pending.ChoiceMaxIncreaseSum);

        var resp = await client.PutAsJsonAsync($"/api/characters/{id}/items/{tomeId}/implement",
            new SetImplementConfigurationRequest(["Range", "Additional Target"]), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var configured = (await SheetAsync(client, id)).Items.Single(i => i.Id == tomeId).Implement!;
        Assert.False(configured.Pending);
        Assert.Equal(["Range", "Additional Target"], configured.ChosenEffects);
    }

    [Fact]
    public async Task Wand_RefusesAnEffectThatDoesNotCostExactlyOne()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var wandId = await AddAsync(client, id, Implement(reference, "magic-wand").Id);

        // «Усиленный» стоит +2 — палочка такой не берёт.
        var refused = await client.PutAsJsonAsync($"/api/characters/{id}/items/{wandId}/implement",
            new SetImplementConfigurationRequest(["Empowered"]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        var ok = await client.PutAsJsonAsync($"/api/characters/{id}/items/{wandId}/implement",
            new SetImplementConfigurationRequest(["Range"]), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
        Assert.True((await SheetAsync(client, id)).Items
            .Single(i => i.Id == wandId).Implement!.ChosenEffects.Contains("Range"));
    }

    [Fact]
    public async Task Tome_BudgetOverflow_NeedsTheGmsReason()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var tomeId = await AddAsync(client, id, Implement(reference, "magic-tome").Id);

        // Два эффекта по +2 — четыре против рекомендованных трёх.
        var refused = await client.PutAsJsonAsync($"/api/characters/{id}/items/{tomeId}/implement",
            new SetImplementConfigurationRequest(["Empowered", "Poisonous"]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, refused.StatusCode);

        var allowed = await client.PutAsJsonAsync($"/api/characters/{id}/items/{tomeId}/implement",
            new SetImplementConfigurationRequest(["Empowered", "Poisonous"], "решение ведущего"),
            Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);
    }

    [Fact]
    public async Task UnknownEffectCode_IsRejected()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var tomeId = await AddAsync(client, id, Implement(reference, "magic-tome").Id);

        var resp = await client.PutAsJsonAsync($"/api/characters/{id}/items/{tomeId}/implement",
            new SetImplementConfigurationRequest(["Совершенно свой эффект"]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Configuring_AnOrdinaryItem_IsRejected()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = reference.Items.Single(i => i.Name == "Sword" && i.Kind == ItemKind.Weapon);
        var itemId = await AddAsync(client, id, sword.Id);

        var resp = await client.PutAsJsonAsync($"/api/characters/{id}/items/{itemId}/implement",
            new SetImplementConfigurationRequest(["Range"]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Configuration_IsRecordedInTheHistory()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var wandId = await AddAsync(client, id, Implement(reference, "magic-wand").Id);
        await client.PutAsJsonAsync($"/api/characters/{id}/items/{wandId}/implement",
            new SetImplementConfigurationRequest(["Fire"]), Json.Options);

        var audit = (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options))!;
        Assert.Contains(audit, a => a.Action == CharacterAuditAction.ImplementConfigured);
    }

    /// <summary>
    /// Скидка священного символа считается по структурному признаку «эффект только для Веры»,
    /// а не по разбору описания: справочник обязан этот признак отдавать.
    /// </summary>
    [Fact]
    public async Task SpellReference_MarksEffectsRestrictedToASingleSkill()
    {
        var (client, _, reference) = await CreateCharacterAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>(
            "/api/spells/RealmsOfTerrinoth", Json.Options))!;

        var sanctuary = spells.Single(s => s.NameEn == "Sanctuary" && s.ParentEffect == "Barrier");
        Assert.Equal("Divine", sanctuary.RestrictedSkill);
        var reflective = spells.Single(s => s.NameEn == "Reflective" && s.ParentEffect == "Barrier");
        Assert.Equal("Arcana", reflective.RestrictedSkill);
        // Обычный эффект доступен нескольким навыкам и признака не несёт.
        Assert.Equal("", spells.First(s => s.NameEn == "Range").RestrictedSkill);

        // Икона объявляет именно этот вид скидки, и её навык — Вера.
        var icon = Implement(reference, "holy-icon");
        Assert.Equal(ImplementDiscountKind.RestrictedSkillDiscount, icon.Implement!.Discount);
        Assert.Equal("Divine", icon.Implement.RequiredMagicSkill);
    }

    // ── Перенос между файлами ──

    [Fact]
    public async Task MaterialAndChoices_SurviveExportAndImport()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var tomeId = await AddAsync(client, id, Implement(reference, "magic-tome").Id,
            ImplementMaterial.Willow);
        await client.PutAsJsonAsync($"/api/characters/{id}/items/{tomeId}/implement",
            new SetImplementConfigurationRequest(["Range"]), Json.Options);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(ImplementMaterial.Willow, export.Character.Items!.Single().Material);

        var imported = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        var result = (await imported.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;

        var copy = (await SheetAsync(client, result.CharacterId)).Items.Single();
        Assert.Equal(ImplementMaterial.Willow, copy.Implement!.Material);
        Assert.Equal(["Range"], copy.Implement.ChosenEffects);
        Assert.False(copy.Implement.Pending);
    }
}
