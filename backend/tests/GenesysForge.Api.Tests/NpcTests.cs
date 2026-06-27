using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class NpcTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public NpcTests(ApiFactory factory) => _factory = factory;

    private static NpcInput SampleInput(string name = "Гоблин-разведчик", NpcKind kind = NpcKind.Rival) => new(
        name, GameSystem.RealmsOfTerrinoth, kind, NpcRole.Skirmisher, "Юркий враг", "homebrew",
        Brawn: 2, Agility: 3, Intellect: 2, Cunning: 3, Willpower: 2, Presence: 2,
        WoundThreshold: 12, StrainThreshold: kind == NpcKind.Minion ? null : 10,
        Soak: 3, MeleeDefense: 1, RangedDefense: 0,
        Silhouette: 1, Tactics: "",
        Visibility: NpcVisibility.Private, CampaignId: null,
        Skills: [new NpcSkillDto("Ближний бой", 2)],
        Abilities: [new NpcAbilityDto("Засада", "Добавляет преимущество при внезапной атаке")],
        Attacks: null,
        Talents: ["Быстрый"], Equipment: ["Кинжал"], Tags: ["гоблин", "лес"]);

    [Fact]
    public async Task Create_Get_AppearsInLibrary()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var resp = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.True(npc.IsMine);
        Assert.Equal("Засада", npc.Abilities[0].Name);

        var list = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.Contains(list, n => n.Id == npc.Id && n.Name == "Гоблин-разведчик");
    }

    [Fact]
    public async Task Filters_BySystemAndKindAndSearch()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        await gm.PostAsJsonAsync("/api/npcs/", SampleInput("Орк-вожак", NpcKind.Nemesis), Json.Options);
        await gm.PostAsJsonAsync("/api/npcs/", SampleInput("Крыса", NpcKind.Minion) with { StrainThreshold = null }, Json.Options);

        var minions = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/?kind=minion", Json.Options))!;
        Assert.All(minions, n => Assert.Equal(NpcKind.Minion, n.Kind));

        var found = (await gm.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/?search=орк", Json.Options))!;
        Assert.Contains(found, n => n.Name == "Орк-вожак");
        Assert.DoesNotContain(found, n => n.Name == "Крыса");
    }

    [Fact]
    public async Task QuickDraft_IsDeterministic_AndValid()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var req = new QuickDraftRequest(GameSystem.GenesysCore, NpcKind.Nemesis, NpcRole.Brute,
            NpcPowerLevel.Strong, null, NpcCombatStyle.Melee, null);

        var a = await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options);
        var b = await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options);
        var npcA = (await a.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var npcB = (await b.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        // одинаковые входы → одинаковые статы (детерминизм)
        Assert.Equal(npcA.Brawn, npcB.Brawn);
        Assert.Equal(npcA.Soak, npcB.Soak);
        Assert.Equal(npcA.WoundThreshold, npcB.WoundThreshold);
        Assert.NotNull(npcA.StrainThreshold); // Nemesis обязан иметь
        Assert.NotEmpty(npcA.Skills);
    }

    [Fact]
    public async Task QuickDraft_StartingSkillAndWeapon_AreFromCatalog()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var req = new QuickDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Rival, NpcRole.Brute,
            NpcPowerLevel.Standard, null, NpcCombatStyle.Melee, "Орк");

        var resp = await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var reference = (await gm.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;

        string Label(string nameRu, string name) => string.IsNullOrWhiteSpace(nameRu) ? name : nameRu;
        var skillLabels = reference.Skills.Select(s => Label(s.NameRu, s.Name)).ToHashSet();
        var weaponLabels = reference.Items.Where(i => i.Kind == ItemKind.Weapon)
            .Select(i => Label(i.NameRu, i.Name)).ToHashSet();

        // Стартовый навык — реальная запись каталога; оружие — структурная атака (а не свободный текст).
        Assert.Contains(npc.Skills[0].Name, skillLabels);
        Assert.NotEmpty(npc.Attacks);
        var attack = npc.Attacks[0];
        Assert.Contains(attack.Name, weaponLabels);
        Assert.NotEmpty(attack.Damage); // боевые статы перенесены из предмета каталога

        // Навык оружия согласован с навыком NPC: оружие распознаётся и считает пул.
        var weapon = reference.Items.Single(i => Label(i.NameRu, i.Name) == attack.Name);
        var baseName = (string s) => System.Text.RegularExpressions.Regex.Replace(s, @"\s*\(.*\)\s*", "").Trim();
        var weaponSkill = reference.Skills.First(s => Label(s.NameRu, s.Name) == npc.Skills[0].Name);
        Assert.Equal(baseName(weaponSkill.Name), baseName(weapon.SkillName));
        Assert.Equal(weapon.SkillName, attack.SkillName);
    }

    [Fact]
    public async Task QuickDraft_PowerLevel_AddsArmorAndSkills_AndSoakReflectsArmor()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        async Task<NpcDetailDto> Draft(NpcPowerLevel lvl) =>
            (await (await gm.PostAsJsonAsync("/api/npcs/quick-draft",
                new QuickDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Rival, NpcRole.Brute,
                    lvl, null, NpcCombatStyle.Melee, "Орк"), Json.Options))
                .Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var reference = (await gm.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        string Label(string nameRu, string name) => string.IsNullOrWhiteSpace(nameRu) ? name : nameRu;
        var armorByLabel = reference.Items.Where(i => i.Kind == ItemKind.Armor)
            .ToDictionary(i => Label(i.NameRu, i.Name), i => i.SoakBonus);

        var weak = await Draft(NpcPowerLevel.Weak);
        var elite = await Draft(NpcPowerLevel.Elite);

        // Слабый: без доспеха поглощение = Мощи (нет фантомного бонуса).
        Assert.Equal(weak.Brawn, weak.Soak);

        // Элитный: появился доспех в снаряжении, и Soak = Мощь + бонус доспеха.
        var armorEntry = elite.Equipment.FirstOrDefault(e => armorByLabel.ContainsKey(e));
        Assert.NotNull(armorEntry);
        Assert.Equal(elite.Brawn + armorByLabel[armorEntry!], elite.Soak);

        // Выше уровень силы — больше навыков.
        Assert.True(elite.Skills.Count > weak.Skills.Count);
    }

    [Fact]
    public async Task Nemesis_WithoutStrain_IsRejected()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var bad = SampleInput("Босс", NpcKind.Nemesis) with { StrainThreshold = null };
        var resp = await gm.PostAsJsonAsync("/api/npcs/", bad, Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Duplicate_CreatesIndependentCopy()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var dup = await gm.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, dup.StatusCode);
        var copy = (await dup.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.NotEqual(npc.Id, copy.Id);
        Assert.Contains("копия", copy.Name);
        Assert.Equal(npc.Skills.Count, copy.Skills.Count);
    }

    [Fact]
    public async Task Attack_RoundTrips_AndResolvesCatalogQuality()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput() with
        {
            Attacks =
            [
                new NpcAttackDto("Длинный меч", "Melee (Heavy)", "+3", "2", "Вплотную", "режущая",
                    [new NpcAttackQualityDto("accurate", "", 2)]),
            ],
        };

        var resp = await gm.PostAsJsonAsync("/api/npcs/", input, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var attack = Assert.Single(npc.Attacks);
        Assert.Equal("Длинный меч", attack.Name);
        Assert.Equal("+3", attack.Damage);
        Assert.Equal("2", attack.Critical);
        var quality = Assert.Single(attack.Qualities);
        Assert.Equal("accurate", quality.QualityCode);
        Assert.False(string.IsNullOrEmpty(quality.NameRu)); // имя подтянуто из справочника
        Assert.Equal(2, quality.Rating);                    // рейтинговое качество сохраняет рейтинг
    }

    [Fact]
    public async Task Duplicate_CopiesAttacks()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput() with
        {
            Attacks = [new NpcAttackDto("Когти", "Brawl", "+1", "4", "Вплотную", "", [])],
        };
        var created = await gm.PostAsJsonAsync("/api/npcs/", input, Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var dup = await gm.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        var copy = (await dup.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        var attack = Assert.Single(copy.Attacks);
        Assert.Equal("Когти", attack.Name);
        Assert.Equal("+1", attack.Damage);
    }

    [Fact]
    public async Task Attack_SourceWeapon_RoundTrips()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput() with
        {
            Attacks = [new NpcAttackDto("Длинный меч", "Melee (Heavy)", "+3", "2", "Вплотную", "", [],
                SourceWeapon: "Длинный меч")],
        };
        var resp = await gm.PostAsJsonAsync("/api/npcs/", input, Json.Options);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal("Длинный меч", Assert.Single(npc.Attacks).SourceWeapon);

        // Duplicate сохраняет привязку к источнику.
        var dup = await gm.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        var copy = (await dup.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal("Длинный меч", Assert.Single(copy.Attacks).SourceWeapon);
    }

    [Fact]
    public async Task HighDefense_Warning_SavesWithWarnings()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var resp = await gm.PostAsJsonAsync("/api/npcs/", SampleInput() with { MeleeDefense = 5 }, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Contains(npc.Warnings, w => w.Contains("Защита"));
    }

    [Fact]
    public async Task ExcessiveDefense_Error_IsRejected()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var resp = await gm.PostAsJsonAsync("/api/npcs/", SampleInput() with { RangedDefense = 7 }, Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Minion_SkillRanks_AreNormalizedToZero()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput("Гоблин-рой", NpcKind.Minion) with
        {
            Skills = [new NpcSkillDto("Ближний бой", 3)],
        };
        var resp = await gm.PostAsJsonAsync("/api/npcs/", input, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.All(npc.Skills, s => Assert.Equal(0, s.Ranks)); // групповые навыки без рангов
    }

    [Fact]
    public async Task Silhouette_And_Tactics_RoundTrip()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput("Дракон", NpcKind.Nemesis) with
        {
            Silhouette = 3, Tactics = "Дышит огнём по площади", WoundThreshold = 40, StrainThreshold = 15,
        };
        var resp = await gm.PostAsJsonAsync("/api/npcs/", input, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal(3, npc.Silhouette);
        Assert.Equal("Дышит огнём по площади", npc.Tactics);
    }

    [Fact]
    public async Task QuickDraft_RoT_UsesOnlyRotCatalogSkills()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var req = new QuickDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Nemesis, NpcRole.Caster,
            NpcPowerLevel.Elite, null, NpcCombatStyle.Magic, "Маг", MagicSkill: null, Environment: "башня");
        var npc = (await (await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options))
            .Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var reference = (await gm.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        string Label(string nameRu, string name) => string.IsNullOrWhiteSpace(nameRu) ? name : nameRu;
        var rotSkills = reference.Skills.Select(s => Label(s.NameRu, s.Name)).ToHashSet();

        // Все навыки сгенерированного RoT-NPC — из каталога RoT (без Core-only вроде Computers/Driving).
        Assert.All(npc.Skills, s => Assert.Contains(s.Name, rotSkills));
        Assert.Contains("башня", npc.Tags);
    }

    [Fact]
    public async Task QuickDraft_CreatureTemplate_HasNaturalAttackAndTag()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var req = new QuickDraftRequest(GameSystem.RealmsOfTerrinoth, NpcKind.Rival, NpcRole.Monster,
            NpcPowerLevel.Standard, null, NpcCombatStyle.Melee, "Волк", Template: CreatureTemplate.Beast);
        var npc = (await (await gm.PostAsJsonAsync("/api/npcs/quick-draft", req, Json.Options))
            .Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        Assert.Contains("зверь", npc.Tags);
        Assert.NotEmpty(npc.Attacks);              // природная атака, не каталожное оружие
        Assert.Empty(npc.Equipment);
        Assert.All(npc.Attacks, a => Assert.False(string.IsNullOrWhiteSpace(a.SkillName)));
    }

    [Fact]
    public async Task ApplyTemplate_AddsCreatureTagAbilityAttack_AndIsIdempotent()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var input = SampleInput("Скелет", NpcKind.Rival) with { Tags = [], Attacks = [], Abilities = [] };

        var resp = await gm.PostAsJsonAsync("/api/npcs/apply-template",
            new ApplyTemplateRequest(input, CreatureTemplate.Undead), Json.Options);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var npc = (await resp.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        Assert.Contains("нежить", npc.Tags);
        Assert.NotEmpty(npc.Attacks);                                   // природная атака
        Assert.Contains(npc.Abilities, a => a.Name.Contains("Ужас"));   // тематическая способность

        // Повторное применение того же шаблона к результату ничего не дублирует (идемпотентно).
        var input2 = input with
        {
            Tags = [.. npc.Tags],
            Attacks = [.. npc.Attacks],
            Abilities = [.. npc.Abilities.Select(a => new NpcAbilityDto(a.Name, a.Description))],
        };
        var resp2 = await gm.PostAsJsonAsync("/api/npcs/apply-template",
            new ApplyTemplateRequest(input2, CreatureTemplate.Undead), Json.Options);
        var npc2 = (await resp2.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal(npc.Tags.Count(t => t == "нежить"), npc2.Tags.Count(t => t == "нежить"));
        Assert.Equal(npc.Attacks.Count, npc2.Attacks.Count);
    }

    [Fact]
    public async Task PrivateNpc_NotVisibleToOtherUsers()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var resp = await stranger.GetAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode); // не найден

        var list = (await stranger.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.DoesNotContain(list, n => n.Id == npc.Id);
    }

    [Fact]
    public async Task Update_And_Delete_OwnerOnly()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var created = await gm.PostAsJsonAsync("/api/npcs/", SampleInput(), Json.Options);
        var npc = (await created.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var forbidden = await stranger.PutAsJsonAsync($"/api/npcs/{npc.Id}",
            SampleInput("Взлом"), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);

        var updated = await gm.PutAsJsonAsync($"/api/npcs/{npc.Id}", SampleInput("Гоблин-ветеран"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var detail = (await updated.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Equal("Гоблин-ветеран", detail.Name);

        var del = await gm.DeleteAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        var gone = await gm.GetAsync($"/api/npcs/{npc.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, gone.StatusCode);
    }
}
