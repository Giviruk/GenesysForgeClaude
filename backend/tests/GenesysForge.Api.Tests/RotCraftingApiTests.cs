using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-CRAFT-01, ROT-ALCH-02 и ROT-CRAFT-MAGIC-01: изготовление, варка и зачарование от
/// предпросмотра до созданного экземпляра.
/// </summary>
public class RotCraftingApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid Id)> CreateAsync()
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
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, reference, id);
    }

    private static Task<HttpResponseMessage> PreviewAsync(
        HttpClient client, Guid id, CraftingProjectInput input) =>
        client.PostAsJsonAsync($"/api/characters/{id}/crafting/preview", input, Json.Options);

    private static async Task<Guid> StartAsync(HttpClient client, Guid id, CraftingProjectInput input)
    {
        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/crafting", input, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<CreatedInCharacterResponse>(Json.Options))!.Id;
    }

    private static Task<HttpResponseMessage> ResolveAsync(
        HttpClient client, Guid id, Guid projectId, CraftingResolveInput input) =>
        client.PostAsJsonAsync($"/api/characters/{id}/crafting/{projectId}/resolve", input, Json.Options);

    /// <summary>Обычное покупаемое оружие с известной ценой и редкостью.</summary>
    private static ItemDefDto Weapon(ReferenceResponse reference) =>
        reference.Items.First(i => i.Kind == ItemKind.Weapon && i.Purchasable && i.Price > 0 && i.Rarity > 0);

    private static ItemDefDto Potion(ReferenceResponse reference) =>
        reference.Items.First(i => i.Name == "Stamina Elixir");

    private static ItemDefDto Implement(ReferenceResponse reference, string code) =>
        reference.Items.Single(i => i.Implement?.Code == code);

    [Fact]
    public async Task Preview_ComputesDifficultyTimeAndCost_AndWritesNothing()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);

        var resp = await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var preview = (await resp.Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;

        Assert.Equal((weapon.Rarity!.Value + 1) / 2, preview.Difficulty);
        Assert.Equal(1 + weapon.Rarity!.Value, preview.Time);
        Assert.Equal("days", preview.TimeUnit);
        Assert.Equal((weapon.Price!.Value + 1) / 2, preview.ListedCost);
        Assert.Equal(preview.ListedCost, preview.Cost); // по умолчанию 100 %
        Assert.Equal("Mechanics", preview.SkillName);
        Assert.NotEmpty(preview.Spends);

        var projects = await client.GetFromJsonAsync<List<CraftingProjectDto>>(
            $"/api/characters/{id}/crafting", Json.Options);
        Assert.Empty(projects!);
    }

    [Fact]
    public async Task Crafting_UsesOrdinaryEquipmentMaterial_ForPriceCostAndResult()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);
        var effectivePrice = CraftsmanshipRules.Price(weapon.Price!.Value, WeaponCraftsmanship.Iron);
        var effectiveRarity = CraftsmanshipRules.Rarity(weapon.Rarity!.Value, WeaponCraftsmanship.Iron);
        var input = new CraftingProjectInput(
            weapon.Id, Craftsmanship: WeaponCraftsmanship.Iron);

        var preview = (await (await PreviewAsync(client, id, input))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(effectivePrice, preview.TargetPrice);
        Assert.Equal(effectiveRarity, preview.TargetRarity);
        Assert.Equal((effectivePrice + 1) / 2, preview.ListedCost);
        Assert.Equal((effectiveRarity + 1) / 2, preview.Difficulty);
        Assert.Equal(WeaponCraftsmanship.Iron, preview.Craftsmanship);

        var projectId = await StartAsync(client, id, input);
        var resolved = (await (await ResolveAsync(client, id, projectId, new CraftingResolveInput(1)))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == resolved.CreatedCharacterItemId);
        Assert.Equal(WeaponCraftsmanship.Iron, item.Craftsmanship);
        Assert.Equal(effectivePrice, item.Price);
    }

    [Fact]
    public async Task Crafting_UsesMagicImplementMaterial_ForPriceCostAndResult()
    {
        var (client, reference, id) = await CreateAsync();
        var staff = Implement(reference, "magic-staff");
        var input = new CraftingProjectInput(staff.Id, Material: ImplementMaterial.Willow);

        var preview = (await (await PreviewAsync(client, id, input))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(staff.Price * 2, preview.TargetPrice);
        Assert.Equal(staff.Rarity + 2, preview.TargetRarity);
        Assert.Equal((staff.Price * 2 + 1) / 2, preview.ListedCost);
        Assert.Equal(ImplementMaterial.Willow, preview.Material);

        var projectId = await StartAsync(client, id, input);
        var resolved = (await (await ResolveAsync(client, id, projectId, new CraftingResolveInput(1)))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == resolved.CreatedCharacterItemId);
        Assert.Equal(ImplementMaterial.Willow, item.Implement!.Material);
        Assert.Equal(staff.Price * 2, item.Price);
    }

    /// <summary>Доля и своя цена работают как при торговле — и записываются в проект.</summary>
    [Fact]
    public async Task Preview_AppliesPercentAndOwnPriceLikeTrade()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);
        var listed = (weapon.Price!.Value + 1) / 2;

        var half = (await (await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id, CostPercent: 50)))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(listed / 2, half.Cost);

        var own = (await (await PreviewAsync(client, id,
                new CraftingProjectInput(weapon.Id, CostOverride: 7, CostOverrideReason: "нашёл в лесу")))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(7, own.Cost);

        var noReason = await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id, CostOverride: 7));
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal("crafting.cost_reason_required",
            (await noReason.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var offStep = await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id, CostPercent: 60));
        Assert.Equal("trade.percent_step_invalid",
            (await offStep.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Успех создаёт предмет с меткой «создано персонажем» и описанием всех трат.</summary>
    [Fact]
    public async Task Resolve_Success_CreatesItemMarkedCraftedWithChoicesInDescription()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);
        var project = await StartAsync(client, id, new CraftingProjectInput(weapon.Id));

        var resp = await ResolveAsync(client, id, project, new CraftingResolveInput(
            NetSuccesses: 2, Triumphs: 1,
            Spends: [new CraftingSpendInput("craft-superior", 1, null, "triumph")]));
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var dto = (await resp.Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;
        Assert.Equal(CraftingProjectStatus.Resolved, dto.Status);
        Assert.NotNull(dto.CreatedCharacterItemId);

        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == dto.CreatedCharacterItemId);
        Assert.Equal(ItemProvenance.Crafted, item.Provenance);
        Assert.Contains("Изготовлено персонажем", item.CraftNote);
        Assert.Contains("Превосходное", item.CraftNote);
        // Качество от триумфа стоит в профиле экземпляра, а не только в тексте.
        Assert.Contains(item.AttackProfiles!.SelectMany(p => p.Qualities), q => q.Code == "superior");
    }

    [Fact]
    public async Task Resolve_Failure_CreatesNothingButStaysInHistory()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);
        var project = await StartAsync(client, id, new CraftingProjectInput(weapon.Id));

        var dto = (await (await ResolveAsync(client, id, project, new CraftingResolveInput(NetSuccesses: 0)))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;
        Assert.Null(dto.CreatedCharacterItemId);
        Assert.Contains("Провал", dto.Outcome);
        Assert.DoesNotContain((await SheetAsync(client, id)).Items!, i => i.Provenance == ItemProvenance.Crafted);
        Assert.Single((await client.GetFromJsonAsync<List<CraftingProjectDto>>(
            $"/api/characters/{id}/crafting", Json.Options))!);
    }

    /// <summary>Из одного броска второго предмета не получается.</summary>
    [Fact]
    public async Task Resolve_Twice_IsRejected()
    {
        var (client, reference, id) = await CreateAsync();
        var project = await StartAsync(client, id, new CraftingProjectInput(Weapon(reference).Id));
        Assert.Equal(HttpStatusCode.OK,
            (await ResolveAsync(client, id, project, new CraftingResolveInput(2))).StatusCode);

        var again = await ResolveAsync(client, id, project, new CraftingResolveInput(2));
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("crafting.project_not_draft",
            (await again.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Single((await SheetAsync(client, id)).Items!, i => i.Provenance == ItemProvenance.Crafted);
    }

    /// <summary>Ресурсы — только описание: кошелёк проекта не касается.</summary>
    [Fact]
    public async Task Crafting_NeverTouchesMoney()
    {
        var (client, reference, id) = await CreateAsync();
        var before = (await SheetAsync(client, id)).Money;

        var project = await StartAsync(client, id, new CraftingProjectInput(
            Weapon(reference).Id, Requirements: "кузница, слиток стали, мех"));
        await ResolveAsync(client, id, project, new CraftingResolveInput(3));

        Assert.Equal(before, (await SheetAsync(client, id)).Money);
    }

    [Fact]
    public async Task Potion_UsesAlchemyAndHours_AndExtraDoseRaisesQuantity()
    {
        var (client, reference, id) = await CreateAsync();
        var potion = Potion(reference);

        var preview = (await (await PreviewAsync(client, id,
                new CraftingProjectInput(potion.Id, Kind: CraftingKind.Potion)))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal("Alchemy", preview.SkillName);
        Assert.Equal("hours", preview.TimeUnit);
        Assert.All(preview.Spends, s => Assert.Equal(CraftingKind.Potion, s.Table));

        var project = await StartAsync(client, id, new CraftingProjectInput(potion.Id, Kind: CraftingKind.Potion));
        var dto = (await (await ResolveAsync(client, id, project, new CraftingResolveInput(
                NetSuccesses: 1, Advantages: 4,
                Spends: [new CraftingSpendInput("alch-extra-dose", 2, null, "advantage")])))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;

        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == dto.CreatedCharacterItemId);
        Assert.Equal(3, item.Quantity);
    }

    /// <summary>ROT-ALCH-01: ровно 12 рецептов и точные price/rarity/component cost.</summary>
    [Fact]
    public async Task PotionCatalog_HasExactPricesRaritiesAndComponentCosts()
    {
        var (client, reference, id) = await CreateAsync();
        var expected = new Dictionary<string, (int Price, int Rarity)>
        {
            ["acid-flask"] = (200, 6),
            ["bottled-courage"] = (25, 5),
            ["health-elixir"] = (25, 3),
            ["immunity-elixir"] = (100, 4),
            ["invisibility-potion"] = (1000, 9),
            ["poison"] = (200, 5),
            ["power-potion"] = (250, 6),
            ["protective-tonic"] = (125, 6),
            ["regeneration-elixir"] = (50, 4),
            ["smokebomb-vial"] = (25, 4),
            ["speed-potion"] = (200, 7),
            ["stamina-elixir"] = (50, 3),
        };
        var potions = reference.Items.Where(i => CraftingRules.IsPotion(i.Code)).ToList();

        Assert.Equal(expected.Count, potions.Count);
        foreach (var potion in potions)
        {
            var code = potion.Code[(potion.Code.LastIndexOf('.') + 1)..];
            var row = expected[code];
            Assert.Equal(row.Price, potion.Price);
            Assert.Equal(row.Rarity, potion.Rarity);

            var preview = (await (await PreviewAsync(client, id,
                    new CraftingProjectInput(potion.Id, Kind: CraftingKind.Potion)))
                .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
            Assert.Equal((row.Price + 1) / 2, preview.ListedCost);
            Assert.Equal((row.Rarity + 1) / 2, preview.Difficulty);
            Assert.Equal("Alchemy", preview.SkillName);
        }
    }

    [Fact]
    public async Task CraftingKind_RejectsWrongCatalogEntry()
    {
        var (client, reference, id) = await CreateAsync();
        var weaponAsPotion = await PreviewAsync(client, id,
            new CraftingProjectInput(Weapon(reference).Id, Kind: CraftingKind.Potion));
        Assert.Equal("crafting.target_not_potion",
            (await weaponAsPotion.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var potionAsItem = await PreviewAsync(client, id,
            new CraftingProjectInput(Potion(reference).Id, Kind: CraftingKind.Item));
        Assert.Equal("crafting.target_is_potion",
            (await potionAsItem.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task Crafting_RejectsShopServices()
    {
        var (client, reference, id) = await CreateAsync();
        var meal = reference.Items.Single(i => i.Code.EndsWith(".meal-tavern", StringComparison.Ordinal));

        var response = await PreviewAsync(client, id,
            new CraftingProjectInput(meal.Id, Kind: CraftingKind.Item));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("crafting.target_not_craftable",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task Enchantment_AcceptsOnlyACharactersMagicSkill()
    {
        var (client, reference, id) = await CreateAsync();
        var magic = reference.Skills.First(s => s.Kind == SkillKind.Magic && s.Name != "Arcana");
        var accepted = (await (await PreviewAsync(client, id, new CraftingProjectInput(
                Weapon(reference).Id, Kind: CraftingKind.Enchantment, SkillName: magic.Name)))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(magic.Name, accepted.SkillName);

        var rejected = await PreviewAsync(client, id, new CraftingProjectInput(
            Weapon(reference).Id, Kind: CraftingKind.Enchantment, SkillName: "Mechanics"));
        Assert.Equal("crafting.magic_skill_required",
            (await rejected.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Комбинированная доза проверяется по каталогу: редкость строго меньше.</summary>
    [Fact]
    public async Task Potion_CombineRequiresStrictlyLowerRarity()
    {
        var (client, reference, id) = await CreateAsync();
        var cheap = reference.Items.First(i => i.Name == "Stamina Elixir");   // редкость 3
        var costly = reference.Items.First(i => i.Name == "Invisibility Potion"); // редкость 9

        var bad = await StartAsync(client, id, new CraftingProjectInput(cheap.Id, Kind: CraftingKind.Potion));
        var rejected = await ResolveAsync(client, id, bad, new CraftingResolveInput(
            NetSuccesses: 1, Triumphs: 2,
            Spends: [new CraftingSpendInput("alch-combine", 1, costly.Id.ToString(), "triumph")]));
        Assert.Equal("crafting.combine_rarity",
            (await rejected.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var good = await StartAsync(client, id, new CraftingProjectInput(costly.Id, Kind: CraftingKind.Potion));
        Assert.Equal(HttpStatusCode.OK, (await ResolveAsync(client, id, good, new CraftingResolveInput(
            NetSuccesses: 1, Triumphs: 2,
            Spends: [new CraftingSpendInput("alch-combine", 1, cheap.Id.ToString(), "triumph")]))).StatusCode);
    }

    /// <summary>Уникальную реликвию обычным процессом не изготовить: у неё нет цены.</summary>
    [Fact]
    public async Task Relic_CannotBeCrafted()
    {
        var (client, reference, id) = await CreateAsync();
        var relic = reference.Items.First(i => i.Price is null && !i.Purchasable);

        var resp = await PreviewAsync(client, id, new CraftingProjectInput(relic.Id));
        Assert.Equal("crafting.target_priceless",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Зачарование начинается с превосходной основы и не создаёт вторую вещь.</summary>
    [Fact]
    public async Task Enchantment_NeedsSuperiorBase_AndUpgradesItInPlace()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);

        // Обычный экземпляр без «Превосходного» зачаровать нельзя.
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(weapon.Id, 1, ItemState.Carried, Free: true), Json.Options);
        var plain = (await SheetAsync(client, id)).Items!.Single(i => i.ItemDefId == weapon.Id);
        var rejected = await client.PostAsJsonAsync($"/api/characters/{id}/crafting",
            new CraftingProjectInput(weapon.Id, plain.Id, CraftingKind.Enchantment, Intent: "огонь"), Json.Options);
        Assert.Equal("crafting.base_not_superior",
            (await rejected.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        // Превосходная основа получается изготовлением на триумф — и она зачаровывается.
        var craft = await StartAsync(client, id, new CraftingProjectInput(weapon.Id));
        var crafted = (await (await ResolveAsync(client, id, craft, new CraftingResolveInput(
                NetSuccesses: 2, Triumphs: 1,
                Spends: [new CraftingSpendInput("craft-superior", 1, null, "triumph")])))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;

        var project = await StartAsync(client, id, new CraftingProjectInput(
            weapon.Id, crafted.CreatedCharacterItemId, CraftingKind.Enchantment,
            Intent: "клинок горит по слову владельца"));
        var itemsBefore = (await SheetAsync(client, id)).Items!.Count;
        var dto = (await (await ResolveAsync(client, id, project, new CraftingResolveInput(NetSuccesses: 2)))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;

        var sheet = await SheetAsync(client, id);
        Assert.Equal(itemsBefore, sheet.Items!.Count); // новой вещи не появилось
        Assert.Equal(crafted.CreatedCharacterItemId, dto.CreatedCharacterItemId);
        Assert.Contains("клинок горит", sheet.Items!.Single(i => i.Id == dto.CreatedCharacterItemId).CraftNote);
    }

    [Fact]
    public async Task Enchantment_RequiresAgreedIntent()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);
        var craft = await StartAsync(client, id, new CraftingProjectInput(weapon.Id));
        var crafted = (await (await ResolveAsync(client, id, craft, new CraftingResolveInput(
                NetSuccesses: 2, Triumphs: 1,
                Spends: [new CraftingSpendInput("craft-superior", 1, null, "triumph")])))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;

        var resp = await client.PostAsJsonAsync($"/api/characters/{id}/crafting",
            new CraftingProjectInput(weapon.Id, crafted.CreatedCharacterItemId, CraftingKind.Enchantment),
            Json.Options);
        Assert.Equal("crafting.intent_required",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Грубая работа Выживанием помечает экземпляр — ведущий сможет его сломать.</summary>
    [Fact]
    public async Task RoughSurvival_MarksInstanceAndUsesSurvival()
    {
        var (client, reference, id) = await CreateAsync();
        var project = await StartAsync(client, id,
            new CraftingProjectInput(Weapon(reference).Id, RoughSurvival: true));
        var dto = (await (await ResolveAsync(client, id, project, new CraftingResolveInput(2)))
            .Content.ReadFromJsonAsync<CraftingProjectDto>(Json.Options))!;

        Assert.Equal("Survival", dto.SkillName);
        var item = (await SheetAsync(client, id)).Items!.Single(i => i.Id == dto.CreatedCharacterItemId);
        Assert.Equal(ItemProvenance.RoughSurvival, item.Provenance);
        Assert.Contains("Грубая работа", item.CraftNote);
    }

    /// <summary>Изготовленный экземпляр не сливается с купленным: у него свои траты.</summary>
    [Fact]
    public async Task CraftedInstance_DoesNotStackWithBoughtOne()
    {
        var (client, reference, id) = await CreateAsync();
        var potion = Potion(reference);
        await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(potion.Id, 1, ItemState.Carried, Free: true), Json.Options);

        var project = await StartAsync(client, id, new CraftingProjectInput(potion.Id, Kind: CraftingKind.Potion));
        await ResolveAsync(client, id, project, new CraftingResolveInput(2));

        var rows = (await SheetAsync(client, id)).Items!.Where(i => i.ItemDefId == potion.Id).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Single(rows, r => r.Provenance == ItemProvenance.Crafted);
    }

    [Fact]
    public async Task Cancel_OnlyWorksBeforeResolve()
    {
        var (client, reference, id) = await CreateAsync();
        var project = await StartAsync(client, id, new CraftingProjectInput(Weapon(reference).Id));
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.DeleteAsync($"/api/characters/{id}/crafting/{project}")).StatusCode);

        var resolved = await StartAsync(client, id, new CraftingProjectInput(Weapon(reference).Id));
        await ResolveAsync(client, id, resolved, new CraftingResolveInput(2));
        var late = await client.DeleteAsync($"/api/characters/{id}/crafting/{resolved}");
        Assert.Equal(HttpStatusCode.BadRequest, late.StatusCode);
        Assert.Equal("crafting.project_not_draft",
            (await late.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    /// <summary>Изменённая сложность и время требуют причины — они попадают в историю.</summary>
    [Fact]
    public async Task Overrides_NeedReasons()
    {
        var (client, reference, id) = await CreateAsync();
        var weapon = Weapon(reference);

        Assert.Equal("crafting.difficulty_reason_required",
            (await (await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id, DifficultyOverride: 1)))
                .Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal("crafting.time_reason_required",
            (await (await PreviewAsync(client, id, new CraftingProjectInput(weapon.Id, TimeOverride: 2)))
                .Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var ok = (await (await PreviewAsync(client, id, new CraftingProjectInput(
                weapon.Id, DifficultyOverride: 4, DifficultyReason: "триумф прошлого проекта",
                TimeOverride: 2, TimeReason: "помогает подмастерье")))
            .Content.ReadFromJsonAsync<CraftingPreviewDto>(Json.Options))!;
        Assert.Equal(4, ok.Difficulty);
        Assert.Equal(2, ok.Time);
        Assert.NotEqual(ok.BaseDifficulty, ok.Difficulty);
    }

    /// <summary>Чужой лист чужими проектами не управляет.</summary>
    [Fact]
    public async Task Crafting_OfAnotherUsersCharacter_IsRejected()
    {
        var (_, reference, id) = await CreateAsync();
        var stranger = await factory.CreateAuthorizedClientAsync();
        var resp = await stranger.PostAsJsonAsync($"/api/characters/{id}/crafting",
            new CraftingProjectInput(Weapon(reference).Id), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }
}
