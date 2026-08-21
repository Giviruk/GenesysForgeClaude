using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-EQP-ATT-01…03: улучшения как собственный тип контента, слоты, совместимость и эффекты
/// на листе. Установка идёт без броска (решение владельца) — проверяются именно правила, а не бросок.
/// </summary>
public class RotAttachmentApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
            "Мастеровой", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<Guid> AddItemAsync(
        HttpClient client, Guid characterId, Guid itemDefId, ItemState state = ItemState.Equipped)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, state, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static async Task<Guid> BuyAttachmentAsync(
        HttpClient client, Guid characterId, Guid attachmentDefId)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/attachments",
            new BuyAttachmentRequest(attachmentDefId, Free: true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static Task<HttpResponseMessage> InstallAsync(
        HttpClient client, Guid characterId, Guid attachmentId, Guid hostId, string? reason = null) =>
        client.PostAsJsonAsync($"/api/characters/{characterId}/attachments/install",
            new InstallAttachmentRequest(attachmentId, hostId, reason), Json.Options);

    private static AttachmentDefDto Attachment(ReferenceResponse reference, string code) =>
        reference.Attachments!.Single(a => a.Code.EndsWith($".{code}", StringComparison.Ordinal));

    private static ItemDefDto Item(ReferenceResponse reference, string name, ItemKind kind) =>
        reference.Items.Single(i => i.Name == name && i.Kind == kind);

    // ── Каталог ──

    [Fact]
    public async Task Catalog_HasAllTwentyOneAttachments_AndTheyAreNotGearAnyMore()
    {
        var (_, _, reference) = await CreateCharacterAsync();

        Assert.Equal(21, reference.Attachments!.Count);
        // Улучшения ушли из каталога снаряжения: покупать их «как мешок» больше нельзя.
        foreach (var code in new[] { "spikes", "razor-edge", "gilded", "balanced-hilt" })
            Assert.DoesNotContain(reference.Items, i => i.Name.Contains(code, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    // code, слоты, цена (null — бесценно), редкость, чары, вид носителя
    [InlineData("balanced-hilt", 1, 1000, 6, false, ItemKind.Weapon)]
    [InlineData("razor-edge", 1, 1250, 6, false, ItemKind.Weapon)]
    [InlineData("serrated-edge", 1, 75, 2, false, ItemKind.Weapon)]
    [InlineData("weighted-head", 1, 250, 2, false, ItemKind.Weapon)]
    [InlineData("recurve-limbs", 1, 300, 4, false, ItemKind.Weapon)]
    [InlineData("explosive-missile", 1, 1250, 7, false, ItemKind.Weapon)]
    [InlineData("duelist-cross-guard", 1, 800, 5, false, ItemKind.Weapon)]
    [InlineData("superior-weapon-customization", 1, 750, 7, false, ItemKind.Weapon)]
    [InlineData("runic-flame", 1, 2000, 8, true, ItemKind.Weapon)]
    [InlineData("runic-frost", 1, 1750, 8, true, ItemKind.Weapon)]
    [InlineData("runic-thunder", 2, 2000, 8, true, ItemKind.Weapon)]
    [InlineData("rune-of-blades", 1, null, 10, true, ItemKind.Weapon)]
    [InlineData("rune-of-severing", 2, null, 10, true, ItemKind.Weapon)]
    [InlineData("ynfernael-corruption", 1, null, 8, true, ItemKind.Weapon)]
    [InlineData("deflective-plating", 1, 450, 4, false, ItemKind.Armor)]
    [InlineData("gilded", 0, 1500, 5, false, ItemKind.Armor)]
    [InlineData("intimidating-visage", 0, 125, 3, false, ItemKind.Armor)]
    [InlineData("ironbound-rune", 2, null, 10, true, ItemKind.Armor)]
    [InlineData("reinforced-plating", 2, 8000, 7, false, ItemKind.Armor)]
    [InlineData("spikes", 1, 600, 4, false, ItemKind.Armor)]
    [InlineData("twilight-rune", 1, null, 10, true, ItemKind.Armor)]
    public async Task Catalog_MatchesTheBookTable(
        string code, int hardPoints, int? price, int rarity, bool enchantment, ItemKind host)
    {
        var (_, _, reference) = await CreateCharacterAsync();
        var def = Attachment(reference, code);

        Assert.Equal(hardPoints, def.HardPointCost);
        Assert.Equal(price, def.Price);
        Assert.Equal(rarity, def.Rarity);
        Assert.Equal(enchantment, def.IsEnchantment);
        Assert.Equal(host, def.HostKind);
        Assert.NotEmpty(def.Source);
    }

    [Fact]
    public async Task Spikes_CarryTheOfficialErrataHardPoint()
    {
        var (_, _, reference) = await CreateCharacterAsync();
        var spikes = Attachment(reference, "spikes");
        Assert.Equal(1, spikes.HardPointCost);
        Assert.Contains("errata", spikes.Source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Descriptions_DoNotRepeat_AndMatchTheirMechanics()
    {
        var (_, _, reference) = await CreateCharacterAsync();

        // Одинаковый текст у двух записей — верный признак копипасты: так «Острое лезвие» долго
        // носило описание пилообразного и обещало Vicious вместо Проникающего.
        var duplicates = reference.Attachments!
            .GroupBy(a => a.Description, StringComparer.Ordinal)
            .Where(g => !string.IsNullOrWhiteSpace(g.Key) && g.Count() > 1)
            .Select(g => string.Join(", ", g.Select(a => a.Code)))
            .ToList();
        Assert.Empty(duplicates);

        var razor = Attachment(reference, "razor-edge");
        Assert.Contains(razor.Effects, e => e.QualityCode == "pierce");
        Assert.Contains(razor.Effects, e => e.Kind == AttachmentEffectKind.CritReduction);
        Assert.Contains("Проникающее", razor.Description);
        Assert.DoesNotContain("Vicious", razor.Description, StringComparison.OrdinalIgnoreCase);

        var serrated = Attachment(reference, "serrated-edge");
        Assert.Equal("vicious", Assert.Single(serrated.Effects).QualityCode);

        var explosive = Attachment(reference, "explosive-missile");
        Assert.Contains("Взрыв", explosive.Description, StringComparison.Ordinal);
        Assert.Contains("5", explosive.Description, StringComparison.Ordinal);
        Assert.Contains(explosive.Effects, e => e.QualityCode == "blast" && e.Value == 5);
    }

    // ── Установка ──

    [Fact]
    public async Task Install_TakesSlotsAndAppliesEffects()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var razor = await BuyAttachmentAsync(client, id, Attachment(reference, "razor-edge").Id);

        var before = await SheetAsync(client, id);
        var beforeItem = before.Items!.Single(i => i.Id == hostId);
        Assert.Equal(0, beforeItem.UsedHardPoints);
        Assert.Equal(2, beforeItem.AttackProfiles!.Single(p => p.IsDefault).Crit);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, razor, hostId)).StatusCode);

        var after = await SheetAsync(client, id);
        var item = after.Items!.Single(i => i.Id == hostId);
        Assert.Equal(1, item.UsedHardPoints);
        Assert.Single(item.Attachments!);
        // Бритвенная кромка: Проникающее 2 у оружия без него и крит на единицу меньше.
        var profile = item.AttackProfiles!.Single(p => p.IsDefault);
        Assert.Equal(1, profile.Crit);
        Assert.Contains(profile.Qualities, q => q.Code == "pierce" && q.Rating == 2);
        // Экземпляр перестал лежать в запасе — он теперь принадлежит предмету.
        Assert.Equal(hostId, after.Attachments!.Single(a => a.Id == razor).HostCharacterItemId);
    }

    [Fact]
    public async Task WeightedHead_RaisesDamage_AndGrantsCumbersome()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var mace = Item(reference, "Mace", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, mace.Id);
        var head = await BuyAttachmentAsync(client, id, Attachment(reference, "weighted-head").Id);

        var before = await SheetAsync(client, id);
        var baseDamage = before.Items!.Single(i => i.Id == hostId)
            .AttackProfiles!.Single(p => p.IsDefault).BaseDamage;

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, head, hostId)).StatusCode);

        var profile = (await SheetAsync(client, id)).Items!.Single(i => i.Id == hostId)
            .AttackProfiles!.Single(p => p.IsDefault);
        Assert.Equal(baseDamage + 2, profile.BaseDamage);
        Assert.Contains(profile.Qualities, q => q.Code == "cumbersome" && q.Rating == 2);
    }

    [Fact]
    public async Task DeflectivePlating_RaisesRangedDefense_OnlyWhileArmorIsActive()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var hostId = await AddItemAsync(client, id, plate.Id);
        var plating = await BuyAttachmentAsync(client, id, Attachment(reference, "deflective-plating").Id);

        var before = await SheetAsync(client, id);
        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, plating, hostId)).StatusCode);
        var after = await SheetAsync(client, id);

        Assert.Equal(before.Derived.RangedDefense + 1, after.Derived.RangedDefense);
        Assert.Equal(before.Derived.MeleeDefense, after.Derived.MeleeDefense);

        // Снятая броня не даёт ни своей защиты, ни защиты улучшения — правило одно на обе.
        await client.PatchAsJsonAsync($"/api/characters/{id}/items/{hostId}",
            new UpdateItemRequest(ItemState.Carried, null), Json.Options);
        var unworn = await SheetAsync(client, id);
        Assert.Equal(0, unworn.Derived.RangedDefense);
        Assert.Equal(0, unworn.Derived.MeleeDefense);
    }

    [Fact]
    public async Task Gilded_AddsBoostDiceToSocialSkills()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var hostId = await AddItemAsync(client, id, plate.Id);
        var gilded = await BuyAttachmentAsync(client, id, Attachment(reference, "gilded").Id);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, gilded, hostId)).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(1, sheet.Skills.Single(s => s.Name == "Charm").BoostDice);
        Assert.Equal(1, sheet.Skills.Single(s => s.Name == "Negotiation").BoostDice);
        Assert.Equal(1, sheet.Skills.Single(s => s.Name == "Leadership").BoostDice);
        Assert.Equal(0, sheet.Skills.Single(s => s.Name == "Stealth").BoostDice);
        // Позолота слотов не занимает: у неё нулевая стоимость.
        Assert.Equal(0, sheet.Items!.Single(i => i.Id == hostId).UsedHardPoints);
    }

    [Fact]
    public async Task ReinforcedPlating_MakesPlateReinforced_AndHeavier()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var hostId = await AddItemAsync(client, id, plate.Id);
        var plating = await BuyAttachmentAsync(client, id, Attachment(reference, "reinforced-plating").Id);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, plating, hostId)).StatusCode);

        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == hostId);
        Assert.True(item.Reinforced);
        Assert.Equal(plate.Encumbrance + 1, item.Encumbrance);
    }

    [Fact]
    public async Task Install_RejectsIncompatibleHost()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var mace = Item(reference, "Mace", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, mace.Id);
        // Бритвенная кромка — для клинкового оружия; булава не подходит.
        var razor = await BuyAttachmentAsync(client, id, Attachment(reference, "razor-edge").Id);

        var resp = await InstallAsync(client, id, razor, hostId);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Install_RejectsSecondCopyOfTheSameAttachment()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var def = Attachment(reference, "serrated-edge").Id;
        var first = await BuyAttachmentAsync(client, id, def);
        var second = await BuyAttachmentAsync(client, id, def);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, first, hostId)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await InstallAsync(client, id, second, hostId)).StatusCode);
    }

    [Fact]
    public async Task Install_RejectsWhenSlotsRunOut()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // У меча один слот по таблице: второе улучшение уже не влезет.
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var first = await BuyAttachmentAsync(client, id, Attachment(reference, "serrated-edge").Id);
        var second = await BuyAttachmentAsync(client, id, Attachment(reference, "razor-edge").Id);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, first, hostId)).StatusCode);
        var resp = await InstallAsync(client, id, second, hostId);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Enchantment_NeedsMagicRank_OrAnExplicitReason()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Руна рассечения занимает два слота — у меча он один, поэтому носитель двуручный.
        var greatsword = Item(reference, "Greatsword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, greatsword.Id);
        var rune = await BuyAttachmentAsync(client, id, Attachment(reference, "rune-of-severing").Id);

        Assert.Equal(HttpStatusCode.BadRequest, (await InstallAsync(client, id, rune, hostId)).StatusCode);

        var withReason = await InstallAsync(client, id, rune, hostId, "помог городской чародей");
        Assert.Equal(HttpStatusCode.NoContent, withReason.StatusCode);

        var sheet = await SheetAsync(client, id);
        var installed = sheet.Attachments!.Single(a => a.Id == rune);
        Assert.Equal("помог городской чародей", installed.Note);
        // Руна рассечения — «не ниже 5», а не «плюс пять».
        var profile = sheet.Items!.Single(i => i.Id == hostId).AttackProfiles!.Single(p => p.IsDefault);
        Assert.Contains(profile.Qualities, q => q.Code == "vicious" && q.Rating == 5);
    }

    // ── Снятие ──

    [Fact]
    public async Task Detach_ReturnsTheSameInstance_AndFreesTheSlot()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var edge = await BuyAttachmentAsync(client, id, Attachment(reference, "serrated-edge").Id);
        await InstallAsync(client, id, edge, hostId);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/attachments/{edge}/detach",
            new DetachAttachmentRequest(DetachOutcome.Returned), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(0, sheet.Items!.Single(i => i.Id == hostId).UsedHardPoints);
        Assert.Null(sheet.Attachments!.Single(a => a.Id == edge).HostCharacterItemId);
        // Качество, которое давало улучшение, исчезло вместе с ним.
        Assert.DoesNotContain(
            sheet.Items!.Single(i => i.Id == hostId).AttackProfiles!.Single(p => p.IsDefault).Qualities,
            q => q.Code == "vicious");
    }

    [Fact]
    public async Task Detach_WithDestroyedOutcome_RemovesTheInstance()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var edge = await BuyAttachmentAsync(client, id, Attachment(reference, "serrated-edge").Id);
        await InstallAsync(client, id, edge, hostId);

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/attachments/{edge}/detach",
            new DetachAttachmentRequest(DetachOutcome.Destroyed), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.DoesNotContain(sheet.Attachments!, a => a.Id == edge);
    }

    // ── Покупка ──

    [Fact]
    public async Task Buy_ChargesTheCatalogPrice_AndPricelessNeedsTheGm()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var edge = Attachment(reference, "serrated-edge");

        var before = await SheetAsync(client, id);
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/attachments",
            new BuyAttachmentRequest(edge.Id), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);

        var after = await SheetAsync(client, id);
        var spent = before.Money - after.Money;
        Assert.Equal(edge.Price, spent);

        // Бесценная руна обычной покупкой не берётся.
        var rune = Attachment(reference, "twilight-rune");
        var priceless = await client.PostAsJsonAsync($"/api/characters/{id}/attachments",
            new BuyAttachmentRequest(rune.Id), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, priceless.StatusCode);
    }

    [Fact]
    public async Task UnexecutableRules_StayVisibleOnTheItem()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var hostId = await AddItemAsync(client, id, plate.Id);
        var spikes = await BuyAttachmentAsync(client, id, Attachment(reference, "spikes").Id);

        Assert.Equal(HttpStatusCode.NoContent, (await InstallAsync(client, id, spikes, hostId)).StatusCode);

        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == hostId);
        // Шипам нужен рантайм столкновения: правило показывается, а не исполняется молча.
        Assert.NotEmpty(item.AttachmentNotes!);
    }
}
