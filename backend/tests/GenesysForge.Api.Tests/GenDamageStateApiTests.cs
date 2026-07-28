using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// GEN-EQP-DMG-01 на листе: состояние повреждения экземпляра меняется отдельным действием, ремонт
/// идёт по кнопке без броска (решение владельца), а последствия состояния считает сервер —
/// поглощение, защита, вес контейнера и пул атаки.
/// </summary>
public class GenDamageStateApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
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
            "Оружейник", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task<Guid> AddItemAsync(
        HttpClient client, Guid characterId, Guid itemDefId,
        ItemState state = ItemState.Equipped,
        WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Steel)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, state, Free: true, Craftsmanship: craftsmanship),
            Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }

    private static Task<HttpResponseMessage> SetStateAsync(
        HttpClient client, Guid characterId, Guid itemId, ItemDamageState state, string? reason = null) =>
        client.PutAsJsonAsync($"/api/characters/{characterId}/items/{itemId}/damage-state",
            new SetItemDamageStateRequest(state, reason), Json.Options);

    private static Task<HttpResponseMessage> RepairAsync(
        HttpClient client, Guid characterId, Guid itemId, RepairItemRequest? req = null) =>
        client.PostAsJsonAsync($"/api/characters/{characterId}/items/{itemId}/repair",
            req ?? new RepairItemRequest(), Json.Options);

    private static Task SetMoneyAsync(HttpClient client, Guid characterId, int money) =>
        client.PatchAsJsonAsync($"/api/characters/{characterId}",
            new UpdateCharacterRequest(null, null, null, null, money), Json.Options);

    /// <summary>
    /// Чем персонаж может заплатить: в фазе создания сначала тратится бюджет стартовых покупок,
    /// поэтому проверять надо общую сумму, а не один кошелёк.
    /// </summary>
    private static int Funds(CharacterSheetDto sheet) => sheet.Money + sheet.StartingPurchaseBudget;

    private static ItemDefDto Item(ReferenceResponse reference, string name, ItemKind kind) =>
        reference.Items.Single(i => i.Name == name && i.Kind == kind);

    // ── Состояние меняется отдельным действием ──

    [Fact]
    public async Task NewItem_IsUndamaged_AndCarriesTheRepairMemo()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var itemId = await AddItemAsync(client, id, sword.Id);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId);

        Assert.Equal(ItemDamageState.Undamaged, item.DamageState);
        Assert.True(item.IsUsable);
        // Памятка приезжает всегда, а не только у сломанного: её читают заранее.
        Assert.NotNull(item.Repair);
        Assert.False(item.Repair!.CanRepair);
        Assert.Equal("Mechanics", item.Repair.SkillName);
    }

    [Theory]
    [InlineData(ItemDamageState.Minor, 1, 25)]
    [InlineData(ItemDamageState.Moderate, 2, 50)]
    [InlineData(ItemDamageState.Major, 3, 100)]
    public async Task DamagedItem_CarriesDifficultyTimeAndMaterialsInTheMemo(
        ItemDamageState state, int difficulty, int percent)
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var itemId = await AddItemAsync(client, id, sword.Id);

        Assert.Equal(HttpStatusCode.NoContent, (await SetStateAsync(client, id, itemId, state)).StatusCode);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId);
        Assert.Equal(state, item.DamageState);
        Assert.True(item.Repair!.CanRepair);
        Assert.Equal(difficulty, item.Repair.Difficulty);
        Assert.Equal(percent, item.Repair.MaterialPercent);
        // Время — один-два часа на каждую ступень базовой сложности.
        Assert.Equal(difficulty, item.Repair.HoursMin);
        Assert.Equal(difficulty * 2, item.Repair.HoursMax);
        // Материалы считаются от цены экземпляра, а не от строки каталога.
        Assert.Equal((int)Math.Ceiling(sword.Price * percent / 100.0), item.Repair.MaterialCost);
    }

    [Fact]
    public async Task MaterialCost_FollowsTheInstancePrice_NotTheCatalogue()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        // Древняя работа стоит в двадцать раз дороже — и материалы дорожают вместе с ней.
        var itemId = await AddItemAsync(client, id, sword.Id, craftsmanship: WeaponCraftsmanship.Ancient);
        await SetStateAsync(client, id, itemId, ItemDamageState.Major);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId);
        Assert.Equal(sword.Price * 20, item.Price);
        Assert.Equal(sword.Price * 20, item.Repair!.MaterialCost);
    }

    [Fact]
    public async Task UnknownState_IsRejected()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);

        var resp = await SetStateAsync(client, id, itemId, (ItemDamageState)42);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task StateChange_IsRecordedInTheHistory()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetStateAsync(client, id, itemId, ItemDamageState.Moderate, "Разрушающее в бою");

        var audit = (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options))!;
        Assert.Contains(audit, a => a.Action == CharacterAuditAction.ItemDamageStateChanged);
    }

    // ── Что состояние делает с числами листа ──

    [Fact]
    public async Task MinorAndModerateArmor_KeepsSoakAndDefense()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var itemId = await AddItemAsync(client, id, plate.Id);

        foreach (var state in new[] { ItemDamageState.Minor, ItemDamageState.Moderate })
        {
            await SetStateAsync(client, id, itemId, state);
            var sheet = await SheetAsync(client, id);
            var item = sheet.Items.Single(i => i.Id == itemId);
            Assert.True(item.IsUsable);
            Assert.Equal(plate.SoakBonus, item.SoakBonus);
        }
    }

    [Fact]
    public async Task MajorArmor_LosesSoakAndDefense_ButKeepsItsWeight()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var plate = Item(reference, "Plate", ItemKind.Armor);
        var itemId = await AddItemAsync(client, id, plate.Id);

        var before = await SheetAsync(client, id);
        var soakBefore = before.Derived.Soak;
        var loadBefore = before.Derived.EncumbranceLoad;
        Assert.True(before.Items.Single(i => i.Id == itemId).SoakBonus > 0);

        await SetStateAsync(client, id, itemId, ItemDamageState.Major);

        var after = await SheetAsync(client, id);
        var item = after.Items.Single(i => i.Id == itemId);
        Assert.False(item.IsUsable);
        Assert.Equal(0, item.SoakBonus);
        Assert.Equal(0, item.MeleeDefense);
        Assert.Equal(0, item.RangedDefense);
        Assert.Equal(soakBefore - plate.SoakBonus, after.Derived.Soak);
        // Разбитые латы никуда не делись: их по-прежнему таскают на себе.
        Assert.Equal(loadBefore, after.Derived.EncumbranceLoad);
        // Потеря объяснена в разборе поправок, а не показана голым нулём.
        Assert.Contains(item.Adjustments!, a => a.Stage == ItemStatStage.DamageState && a.Field == "soak");
    }

    [Fact]
    public async Task MajorArmor_StopsBeingTheActiveArmor()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Plate", ItemKind.Armor).Id);
        Assert.Equal(itemId, (await SheetAsync(client, id)).ActiveArmorCharacterItemId);

        await SetStateAsync(client, id, itemId, ItemDamageState.Major);

        var sheet = await SheetAsync(client, id);
        Assert.Null(sheet.ActiveArmorCharacterItemId);
        Assert.False(sheet.Items.Single(i => i.Id == itemId).IsActiveArmor);
    }

    [Fact]
    public async Task MajorContainer_LosesItsEncumbranceBonus_ButNotItsContents()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var container = reference.Items.FirstOrDefault(i => i.EncumbranceThresholdBonus > 0);
        Assert.NotNull(container);
        var itemId = await AddItemAsync(client, id, container!.Id);

        var before = await SheetAsync(client, id);
        var thresholdBefore = before.Derived.EncumbranceThreshold;

        await SetStateAsync(client, id, itemId, ItemDamageState.Major);

        var after = await SheetAsync(client, id);
        var item = after.Items.Single(i => i.Id == itemId);
        Assert.Equal(0, item.EncumbranceThresholdBonus);
        Assert.Equal(thresholdBefore - container.EncumbranceThresholdBonus,
            after.Derived.EncumbranceThreshold);
        // Сам мешок остался в инвентаре: содержимое не теряется вместе с бонусом.
        Assert.Equal(before.Items.Count, after.Items.Count);
    }

    // ── Пул атаки ──

    [Fact]
    public async Task MinorWeapon_AddsOneSetbackToTheAttackPool()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetStateAsync(client, id, itemId, ItemDamageState.Minor);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId);
        var pool = item.AttackProfiles!.Single(p => p.IsDefault).PoolModifiers!;

        Assert.Equal(1, pool.Setback);
        Assert.Equal(0, pool.DifficultyIncrease);
        Assert.Contains(pool.Sources, s => s.NameRu.Contains("повреждение", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ModerateWeapon_RaisesTheDifficultyOnce()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetStateAsync(client, id, itemId, ItemDamageState.Moderate);

        var pool = (await SheetAsync(client, id)).Items.Single(i => i.Id == itemId)
            .AttackProfiles!.Single(p => p.IsDefault).PoolModifiers!;

        Assert.Equal(0, pool.Setback);
        Assert.Equal(1, pool.DifficultyIncrease);
    }

    // ── Ремонт по кнопке ──

    [Fact]
    public async Task Repair_ChargesMaterials_AndMakesTheItemWholeAgain()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var itemId = await AddItemAsync(client, id, sword.Id);
        await SetMoneyAsync(client, id, 1000);
        await SetStateAsync(client, id, itemId, ItemDamageState.Moderate);

        var expected = (int)Math.Ceiling(sword.Price * 0.5);
        var before = Funds(await SheetAsync(client, id));
        Assert.Equal(HttpStatusCode.NoContent, (await RepairAsync(client, id, itemId)).StatusCode);

        var sheet = await SheetAsync(client, id);
        var item = sheet.Items.Single(i => i.Id == itemId);
        Assert.Equal(ItemDamageState.Undamaged, item.DamageState);
        Assert.True(item.IsUsable);
        Assert.Equal(before - expected, Funds(sheet));
        Assert.Equal(0, item.AttackProfiles!.Single(p => p.IsDefault).PoolModifiers!.Setback);
    }

    [Fact]
    public async Task Repair_TakesTenPercentOffPerNetAdvantage()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var itemId = await AddItemAsync(client, id, sword.Id);
        await SetMoneyAsync(client, id, 1000);
        await SetStateAsync(client, id, itemId, ItemDamageState.Major);

        var before = Funds(await SheetAsync(client, id));
        Assert.Equal(HttpStatusCode.NoContent,
            (await RepairAsync(client, id, itemId, new RepairItemRequest(NetAdvantages: 2))).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(before - (int)Math.Ceiling(sword.Price * 0.8), Funds(sheet));
    }

    [Fact]
    public async Task FreeRepair_ChargesNothing()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetMoneyAsync(client, id, 50);
        await SetStateAsync(client, id, itemId, ItemDamageState.Major);
        var before = Funds(await SheetAsync(client, id));

        Assert.Equal(HttpStatusCode.NoContent,
            (await RepairAsync(client, id, itemId, new RepairItemRequest(Free: true))).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(before, Funds(sheet));
        Assert.Equal(ItemDamageState.Undamaged, sheet.Items.Single(i => i.Id == itemId).DamageState);
    }

    [Fact]
    public async Task Repair_WithoutEnoughMoney_ChangesNothing()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Древние латы стоят в двадцать раз дороже обычных: материалы на серьёзный ремонт
        // заведомо не по карману начинающему герою.
        var itemId = await AddItemAsync(client, id, Item(reference, "Plate", ItemKind.Armor).Id,
            craftsmanship: WeaponCraftsmanship.Ancient);
        await SetStateAsync(client, id, itemId, ItemDamageState.Major);
        var before = Funds(await SheetAsync(client, id));

        var resp = await RepairAsync(client, id, itemId);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(before, Funds(sheet));
        Assert.Equal(ItemDamageState.Major, sheet.Items.Single(i => i.Id == itemId).DamageState);
        Assert.False(sheet.Items.Single(i => i.Id == itemId).Repair!.Affordable);
    }

    [Fact]
    public async Task UndamagedAndDestroyed_CannotBeRepaired()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetMoneyAsync(client, id, 1000);

        var before = Funds(await SheetAsync(client, id));
        Assert.Equal(HttpStatusCode.BadRequest, (await RepairAsync(client, id, itemId)).StatusCode);

        await SetStateAsync(client, id, itemId, ItemDamageState.Destroyed);
        Assert.Equal(HttpStatusCode.BadRequest, (await RepairAsync(client, id, itemId)).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(before, Funds(sheet));
        var item = sheet.Items.Single(i => i.Id == itemId);
        Assert.Equal(ItemDamageState.Destroyed, item.DamageState);
        Assert.False(item.Repair!.CanRepair);
    }

    [Fact]
    public async Task CostOverride_NeedsAReason()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetMoneyAsync(client, id, 1000);
        await SetStateAsync(client, id, itemId, ItemDamageState.Minor);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await RepairAsync(client, id, itemId, new RepairItemRequest(CostOverride: 5))).StatusCode);

        var before = Funds(await SheetAsync(client, id));
        Assert.Equal(HttpStatusCode.NoContent, (await RepairAsync(client, id, itemId,
            new RepairItemRequest(CostOverride: 5, OverrideReason: "мастерская гильдии"))).StatusCode);
        Assert.Equal(before - 5, Funds(await SheetAsync(client, id)));
    }

    [Fact]
    public async Task Repair_IsRecordedInTheHistory()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetMoneyAsync(client, id, 1000);
        await SetStateAsync(client, id, itemId, ItemDamageState.Minor);
        await RepairAsync(client, id, itemId);

        var audit = (await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options))!;
        Assert.Contains(audit, a => a.Action == CharacterAuditAction.ItemRepaired);
    }

    // ── Улучшения: собственное состояние ──

    [Fact]
    public async Task BrokenAttachment_StopsWorking_ButKeepsTheSlot()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var sword = Item(reference, "Sword", ItemKind.Weapon);
        var hostId = await AddItemAsync(client, id, sword.Id);
        var razor = reference.Attachments!.Single(a => a.Code.EndsWith(".razor-edge", StringComparison.Ordinal));
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/attachments",
            new BuyAttachmentRequest(razor.Id, Free: true), Json.Options);
        var attachmentId = (await buy.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PostAsJsonAsync($"/api/characters/{id}/attachments/install",
            new InstallAttachmentRequest(attachmentId, hostId), Json.Options);

        var installed = (await SheetAsync(client, id)).Items.Single(i => i.Id == hostId);
        Assert.Contains(installed.AttackProfiles!.Single(p => p.IsDefault).Qualities,
            q => q.Code == "pierce");
        var usedSlots = installed.UsedHardPoints;

        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/attachments/{attachmentId}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Major), Json.Options)).StatusCode);

        var after = (await SheetAsync(client, id)).Items.Single(i => i.Id == hostId);
        Assert.DoesNotContain(after.AttackProfiles!.Single(p => p.IsDefault).Qualities,
            q => q.Code == "pierce");
        // Слот освобождает снятие, а не поломка.
        Assert.Equal(usedSlots, after.UsedHardPoints);
        Assert.False(after.Attachments!.Single(a => a.Id == attachmentId).IsUsable);
    }

    [Fact]
    public async Task BrokenHost_SilencesItsAttachments()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var hostId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        var razor = reference.Attachments!.Single(a => a.Code.EndsWith(".razor-edge", StringComparison.Ordinal));
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/attachments",
            new BuyAttachmentRequest(razor.Id, Free: true), Json.Options);
        var attachmentId = (await buy.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PostAsJsonAsync($"/api/characters/{id}/attachments/install",
            new InstallAttachmentRequest(attachmentId, hostId), Json.Options);

        await SetStateAsync(client, id, hostId, ItemDamageState.Major);

        var item = (await SheetAsync(client, id)).Items.Single(i => i.Id == hostId);
        Assert.DoesNotContain(item.AttackProfiles!.Single(p => p.IsDefault).Qualities,
            q => q.Code == "pierce");
    }

    [Fact]
    public async Task PricelessAttachment_NeedsAGmQuoteToBeRepaired()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var priceless = reference.Attachments!.First(a => a.Price is null);
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/attachments",
            new BuyAttachmentRequest(priceless.Id, Free: true), Json.Options);
        var attachmentId = (await buy.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await SetMoneyAsync(client, id, 1000);
        await client.PutAsJsonAsync($"/api/characters/{id}/attachments/{attachmentId}/damage-state",
            new SetItemDamageStateRequest(ItemDamageState.Minor), Json.Options);

        // Обычной цены нет — сервер отказывает и просит цену ведущего, а памятка это показывает.
        var sheet = await SheetAsync(client, id);
        Assert.Null(sheet.Attachments!.Single(a => a.Id == attachmentId).Repair!.MaterialCost);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync(
            $"/api/characters/{id}/attachments/{attachmentId}/repair",
            new RepairItemRequest(), Json.Options)).StatusCode);

        var before = Funds(await SheetAsync(client, id));
        Assert.Equal(HttpStatusCode.NoContent, (await client.PostAsJsonAsync(
            $"/api/characters/{id}/attachments/{attachmentId}/repair",
            new RepairItemRequest(CostOverride: 300, OverrideReason: "цена ведущего"), Json.Options)).StatusCode);
        Assert.Equal(before - 300, Funds(await SheetAsync(client, id)));
    }

    // ── Перенос между файлами ──

    [Fact]
    public async Task DamageState_SurvivesExportAndImport()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        var itemId = await AddItemAsync(client, id, Item(reference, "Sword", ItemKind.Weapon).Id);
        await SetStateAsync(client, id, itemId, ItemDamageState.Moderate);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(ItemDamageState.Moderate, export.Character.Items!.Single().DamageState);

        var imported = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        Assert.Equal(HttpStatusCode.Created, imported.StatusCode);
        var result = (await imported.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;

        var sheet = await SheetAsync(client, result.CharacterId);
        Assert.Equal(ItemDamageState.Moderate, sheet.Items.Single().DamageState);
    }
}
