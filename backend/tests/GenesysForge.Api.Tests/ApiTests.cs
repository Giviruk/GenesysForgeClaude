using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class AuthTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public AuthTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Register_Login_Succeeds()
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("auth-test@test.local", "password123", "Tester"));
        Assert.Equal(HttpStatusCode.OK, reg.StatusCode);

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("auth-test@test.local", "password123"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>(Json.Options);
        Assert.False(string.IsNullOrEmpty(auth!.Token));
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("auth-test2@test.local", "password123", "Tester"));
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("auth-test2@test.local", "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task DuplicateEmail_Returns409()
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@test.local", "password123", "Tester"));
        var second = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("dup@test.local", "password123", "Tester2"));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/characters/");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Health_reports_database_connectivity()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        Assert.Equal("ok", body!["status"]);
        Assert.Equal("ok", body["database"]);
    }
}

public class CharacterFlowTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CharacterFlowTests(ApiFactory factory) => _factory = factory;

    private async Task<(HttpClient Client, ReferenceResponse Reference, Guid CharacterId)> CreateCharacterAsync(
        GameSystem system, string? careerName = null)
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>($"/api/reference/{system}", Json.Options))!;
        var archetype = reference.Archetypes[0];
        var career = careerName is null ? reference.Careers[0] : reference.Careers.First(c => c.Name == careerName);

        var create = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Test Hero", system, archetype.Id, career.Id,
                [.. career.CareerSkillNames.Take(2)]));
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return (client, reference, body!["id"]);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    [Fact]
    public async Task CreateCharacter_SheetHasDerivedStats_AndFreeSkillRanks()
    {
        var (client, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var sheet = await SheetAsync(client, id);

        Assert.Equal("Test Hero", sheet.Name);
        Assert.Equal(GameSystem.GenesysCore, sheet.System);
        // Производные характеристики соответствуют формулам
        Assert.Equal(sheet.Archetype.WoundBase + sheet.Characteristics["brawn"], sheet.Derived.WoundThreshold);
        Assert.Equal(sheet.Archetype.StrainBase + sheet.Characteristics["willpower"], sheet.Derived.StrainThreshold);
        Assert.Equal(sheet.Characteristics["brawn"], sheet.Derived.Soak);
        Assert.Equal(5 + sheet.Characteristics["brawn"], sheet.Derived.EncumbranceThreshold);
        // Два бесплатных карьерных ранга, XP не потрачен
        Assert.Equal(2, sheet.Skills.Count(s => s.Ranks == 1));
        Assert.Equal(0, sheet.SpentXp);
        // Карьерные навыки помечены
        Assert.Equal(8, sheet.Skills.Count(s => s.IsCareer));
    }

    [Fact]
    public async Task BuySkillRank_SpendsXp_AndUpgradesDicePool()
    {
        var (client, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var sheet = await SheetAsync(client, id);
        var skill = sheet.Skills.First(s => s.IsCareer && s.Ranks == 0);

        var buy = await client.PostAsync($"/api/characters/{id}/skills/{skill.SkillDefId}/buy-rank", null);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var updated = await SheetAsync(client, id);
        var updatedSkill = updated.Skills.First(s => s.SkillDefId == skill.SkillDefId);
        Assert.Equal(1, updatedSkill.Ranks);
        Assert.Equal(5, updated.SpentXp); // карьерный, 1-й ранг = 5 XP
        Assert.Equal(1, updatedSkill.Pool.Proficiency);
    }

    [Fact]
    public async Task BuySkillRank_AboveCreationCap_Rejected()
    {
        var (client, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var sheet = await SheetAsync(client, id);
        var skill = sheet.Skills.First(s => s.Ranks == 1); // бесплатный ранг

        await client.PostAsync($"/api/characters/{id}/skills/{skill.SkillDefId}/buy-rank", null); // ранг 2 — ок
        var third = await client.PostAsync($"/api/characters/{id}/skills/{skill.SkillDefId}/buy-rank", null);
        Assert.Equal(HttpStatusCode.BadRequest, third.StatusCode); // при создании макс. 2
    }

    [Fact]
    public async Task TalentPyramid_EnforcedOverApi()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var tier1 = reference.Talents.Where(t => t.Tier == 1 && !t.IsRanked).Take(2).ToList();
        var tier2 = reference.Talents.First(t => t.Tier == 2);

        // Тир 2 без талантов тира 1 — отказ
        var premature = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(tier2.Id));
        Assert.Equal(HttpStatusCode.BadRequest, premature.StatusCode);

        // Два таланта тира 1 — теперь тир 2 можно
        foreach (var t in tier1)
        {
            var r = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(t.Id));
            Assert.Equal(HttpStatusCode.NoContent, r.StatusCode);
        }
        var allowed = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(tier2.Id));
        Assert.Equal(HttpStatusCode.NoContent, allowed.StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(2, sheet.TalentTierCounts[1]);
        Assert.Equal(1, sheet.TalentTierCounts[2]);
        Assert.Equal(5 + 5 + 10, sheet.SpentXp);
    }

    [Fact]
    public async Task RankedTalent_GritIncreasesStrainThreshold()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var before = await SheetAsync(client, id);
        var grit = reference.Talents.First(t => t.Name == "Упорство"); // Grit: +1 порог стрейна за ранг

        await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy", new BuyTalentRequest(grit.Id));
        var after = await SheetAsync(client, id);

        Assert.Equal(before.Derived.StrainThreshold + 1, after.Derived.StrainThreshold);
        // лист отдаёт пассивные бонусы таланта — для детального отображения на клиенте
        var owned = after.Talents.First(t => t.Name == "Упорство");
        Assert.Equal(1, owned.StrainBonus);
        Assert.Equal(0, owned.WoundBonus);
    }

    [Fact]
    public async Task Terrinoth_HasSystemSpecificTalents_AbsentFromGenesysCore()
    {
        var terrinoth = await _factory.CreateAuthorizedClientAsync();
        var trefs = (await terrinoth.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var core = await _factory.CreateAuthorizedClientAsync();
        var crefs = (await core.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;

        // фэнтези-таланты присутствуют у Terrinoth и отсутствуют у Genesys Core
        Assert.Contains(trefs.Talents, t => t.Setting.HasFlag(GenesysSetting.Fantasy));
        Assert.DoesNotContain(crefs.Talents, t => t.Setting.HasFlag(GenesysSetting.Fantasy));
        // общие таланты (для любого сеттинга) по-прежнему доступны в обеих системах
        Assert.Contains(trefs.Talents, t => t.Name == "Упорство");
        Assert.Contains(crefs.Talents, t => t.Name == "Упорство");
        // у Terrinoth талантов строго больше, чем у Core (общие + специфичные)
        Assert.True(trefs.Talents.Count > crefs.Talents.Count);
    }

    [Fact]
    public async Task Inventory_EquipArmor_RecalculatesSoakDefenseAndLoad()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.RealmsOfTerrinoth);
        var before = await SheetAsync(client, id);
        var plate = reference.Items.First(i => i.Name == "Plate");

        // Добавить в рюкзак: бонусов нет, вес полный
        var add = await client.PostAsJsonAsync($"/api/characters/{id}/items",
            new AddItemRequest(plate.Id, 1, ItemState.Backpack));
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
        var inBackpack = await SheetAsync(client, id);
        Assert.Equal(before.Derived.Soak, inBackpack.Derived.Soak);
        Assert.Equal(plate.Encumbrance, inBackpack.Derived.EncumbranceLoad);

        // Надеть: +поглощение, +защита, вес −3
        var itemId = inBackpack.Items[0].Id;
        await client.PatchAsJsonAsync($"/api/characters/{id}/items/{itemId}", new UpdateItemRequest(ItemState.Equipped, null), Json.Options);
        var equipped = await SheetAsync(client, id);
        Assert.Equal(before.Derived.Soak + plate.SoakBonus, equipped.Derived.Soak);
        Assert.Equal(plate.MeleeDefense, equipped.Derived.MeleeDefense);
        Assert.Equal(plate.Encumbrance - 3, equipped.Derived.EncumbranceLoad);
    }

    [Fact]
    public async Task Inventory_EquippedBackpack_RaisesEncumbranceThreshold()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.RealmsOfTerrinoth);
        var before = await SheetAsync(client, id);
        var backpack = reference.Items.First(i => i.Name == "Backpack");

        await client.PostAsJsonAsync($"/api/characters/{id}/items", new AddItemRequest(backpack.Id, 1, ItemState.Equipped));
        var after = await SheetAsync(client, id);
        Assert.Equal(before.Derived.EncumbranceThreshold + 4, after.Derived.EncumbranceThreshold);
    }

    [Fact]
    public async Task HeroicAbility_OnlyForTerrinoth()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.RealmsOfTerrinoth);
        Assert.NotEmpty(reference.HeroicAbilities);
        var ability = reference.HeroicAbilities[0];

        var set = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new SetHeroicAbilityRequest(ability.Id));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        var sheet = await SheetAsync(client, id);
        Assert.Equal(ability.Name, sheet.HeroicAbility!.Name);

        // Для Genesys Core — отказ
        var (coreClient, _, coreId) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var coreSet = await coreClient.PutAsJsonAsync($"/api/characters/{coreId}/heroic-ability",
            new SetHeroicAbilityRequest(ability.Id));
        Assert.Equal(HttpStatusCode.BadRequest, coreSet.StatusCode);
    }

    [Fact]
    public async Task HeroicAbility_HasUpgrades_AndUpgradePointsRespected()
    {
        var (client, reference, id) = await CreateCharacterAsync(GameSystem.RealmsOfTerrinoth);
        var ability = reference.HeroicAbilities.First(h => h.Upgrades.Count == 2);
        // Каждая встроенная способность несёт Improved (1 очко) и Supreme (2 очка).
        Assert.Equal(1, ability.Upgrades[0].Level);
        Assert.Equal(1, ability.Upgrades[0].Cost);
        Assert.Equal(2, ability.Upgrades[1].Level);
        Assert.Equal(2, ability.Upgrades[1].Cost);

        await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability", new SetHeroicAbilityRequest(ability.Id));

        // На создании доступно ровно 1 стартовое очко.
        var sheet = await SheetAsync(client, id);
        Assert.Equal(1, sheet.HeroicUpgradePointsTotal);
        Assert.Equal(0, sheet.HeroicUpgradeRank);

        // Improved (стоимость 1) — по карману.
        var improved = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-upgrade",
            new SetHeroicUpgradeRankRequest(1));
        Assert.Equal(HttpStatusCode.NoContent, improved.StatusCode);
        sheet = await SheetAsync(client, id);
        Assert.Equal(1, sheet.HeroicUpgradeRank);
        Assert.Equal(1, sheet.HeroicUpgradePointsSpent);

        // Supreme требует суммарно 3 очка — на создании нельзя.
        var supreme = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-upgrade",
            new SetHeroicUpgradeRankRequest(2));
        Assert.Equal(HttpStatusCode.BadRequest, supreme.StatusCode);

        // Смена способности обнуляет купленный ранг.
        var other = reference.HeroicAbilities.First(h => h.Id != ability.Id);
        await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability", new SetHeroicAbilityRequest(other.Id));
        sheet = await SheetAsync(client, id);
        Assert.Equal(0, sheet.HeroicUpgradeRank);
    }

    [Fact]
    public async Task BuyCharacteristic_OnlyDuringCreation()
    {
        var (client, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var before = await SheetAsync(client, id);

        // фронтенд шлёт camelCase — маршрут обязан принимать без учёта регистра
        var buy = await client.PostAsync($"/api/characters/{id}/characteristics/agility/buy", null);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        var after = await SheetAsync(client, id);
        Assert.Equal(before.Characteristics["agility"] + 1, after.Characteristics["agility"]);
        Assert.Equal((before.Characteristics["agility"] + 1) * 10, after.SpentXp - before.SpentXp);

        // Завершить создание — повышение характеристик заблокировано
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        var locked = await client.PostAsync($"/api/characters/{id}/characteristics/Agility/buy", null);
        Assert.Equal(HttpStatusCode.BadRequest, locked.StatusCode);
    }

    [Fact]
    public async Task BuyCharacteristic_UnknownName_Returns400WithMessage()
    {
        var (client, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var response = await client.PostAsync($"/api/characters/{id}/characteristics/strength/buy", null);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Contains("Неизвестная характеристика", error!.Message);
    }

    [Fact]
    public async Task ForeignCharacter_NotAccessible()
    {
        var (_, _, id) = await CreateCharacterAsync(GameSystem.GenesysCore);
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var response = await stranger.GetAsync($"/api/characters/{id}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); // «не найден»
    }
}

public class CustomContentTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CustomContentTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CustomSkill_AppearsInReference_AndOnSheet()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var create = await client.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.RealmsOfTerrinoth, "Sailing", CharacteristicType.Agility, SkillKind.General),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var sailing = reference.Skills.First(s => s.Name == "Sailing");
        Assert.True(sailing.IsCustom);

        // Кастомный навык другому пользователю не виден
        var other = await _factory.CreateAuthorizedClientAsync();
        var otherRef = (await other.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        Assert.DoesNotContain(otherRef.Skills, s => s.Name == "Sailing");

        // Навык появляется на листе и его ранг можно купить
        var archetype = reference.Archetypes[0];
        var career = reference.Careers[0];
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Sailor", GameSystem.RealmsOfTerrinoth, archetype.Id, career.Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var buy = await client.PostAsync($"/api/characters/{id}/skills/{sailing.Id}/buy-rank", null);
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        var onSheet = sheet.Skills.First(s => s.Name == "Sailing");
        Assert.Equal(1, onSheet.Ranks);
        Assert.False(onSheet.IsCareer);
    }

    [Fact]
    public async Task CustomTalent_InvalidTier_Rejected()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var response = await client.PostAsJsonAsync("/api/custom/talents",
            new CreateCustomTalentRequest(GameSystem.GenesysCore, "Broken", 6, false, "", "", 0, 0, 0, 0, 0),
            Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CustomItem_WorksInInventoryWithRecalculation()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var create = await client.PostAsJsonAsync("/api/custom/items",
            new CreateCustomItemRequest(GameSystem.GenesysCore, "Dwarven Cuirass", ItemKind.Armor,
                5, 3, 1, 1, 0, "Кастомная броня", 0, 5),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var item = (await create.Content.ReadFromJsonAsync<ItemDefDto>(Json.Options))!;

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Tank", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        await client.PostAsJsonAsync($"/api/characters/{id}/items", new AddItemRequest(item.Id, 1, ItemState.Equipped), Json.Options);
        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        Assert.Equal(sheet.Characteristics["brawn"] + 3, sheet.Derived.Soak);
        Assert.Equal(1, sheet.Derived.MeleeDefense);
    }

    [Fact]
    public async Task CustomHeroicAbility_CanBeAssigned()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var create = await client.PostAsJsonAsync("/api/custom/heroic-abilities",
            new CreateCustomHeroicAbilityRequest("Гнев Предков", "Раз в сессию призовите духов предков."));
        var ability = (await create.Content.ReadFromJsonAsync<HeroicAbilityDto>(Json.Options))!;

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Шаман", GameSystem.RealmsOfTerrinoth, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var set = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new SetHeroicAbilityRequest(ability.Id));
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);
        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        Assert.Equal("Гнев Предков", sheet.HeroicAbility!.Name);
    }
}
