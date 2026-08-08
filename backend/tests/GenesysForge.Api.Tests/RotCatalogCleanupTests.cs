using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-CLEAN-3.1 / 3.2 / 3.5: активный каталог RoT не предлагает игроку лишние карьеры, навыки,
/// таланты и предметы, но уже созданные персонажи ничего не теряют.
/// </summary>
public class RotCatalogCleanupTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly string[] RotCareerNames =
    [
        "Disciple", "Envoy", "Mage", "Primalist",
        "Scholar", "Scoundrel", "Scout", "Warrior",
    ];

    private static async Task<ReferenceResponse> ReferenceAsync(HttpClient client, GameSystem system) =>
        (await client.GetFromJsonAsync<ReferenceResponse>($"/api/reference/{system}", Json.Options))!;

    // ---- ROT-CLEAN-3.1: ровно восемь карьер ----

    [Fact]
    public async Task RotCareerSet_IsExactlyTheEightOfTheBook()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var careers = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Careers
            .Where(c => !c.IsCustom).Select(c => c.Name).OrderBy(x => x, StringComparer.Ordinal);

        // Сравнивается полное множество, а не только количество.
        Assert.Equal(RotCareerNames, careers);
    }

    // Knight и Runemaster в книге не карьеры: Runemaster — класс мага из Descent, а навык Runes
    // и без него есть у Scholar. Строки не удаляются, а гаснут — созданные персонажи их сохраняют.
    [Theory]
    [InlineData("Knight")]
    [InlineData("Runemaster")]
    public async Task CareerOutsideTheBook_IsNotOfferedInRot(string name)
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var careers = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Careers;

        Assert.DoesNotContain(careers, c => !c.IsCustom && c.Name == name);
    }

    // ---- ROT-CLEAN-3.2: Gunnery только в Core ----

    [Fact]
    public async Task Gunnery_IsAbsentFromRot_ButStaysAValidCoreSkill()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var rot = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Skills;
        var core = (await ReferenceAsync(client, GameSystem.GenesysCore)).Skills;

        // Обе системы проверяются в одном процессе: фильтр RoT не должен удалить контент Core.
        Assert.DoesNotContain(rot, s => s.Name == "Gunnery");
        Assert.Contains(core, s => s.Name == "Gunnery");
    }

    [Theory]
    [InlineData("knife")]
    [InlineData("revolver")]
    [InlineData("heavy-jacket")]
    [InlineData("painkiller")]
    [InlineData("vacuum-sealed")]
    [InlineData("rare-metals")]
    [InlineData("enhanced-servos")]
    [InlineData("telescopic-sight")]
    [InlineData("weapon-harness")]
    [InlineData("underslung-grenade-launcher")]
    [InlineData("underslung-shotgun")]
    [InlineData("underslung-flamethrower")]
    [InlineData("bipod")]
    [InlineData("tripod")]
    [InlineData("extended-barrel")]
    [InlineData("hair-trigger")]
    public async Task CoreItem_IsAbsentFromRot_ButStaysInCore(string code)
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var rot = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Items;
        var core = (await ReferenceAsync(client, GameSystem.GenesysCore)).Items;

        Assert.DoesNotContain(rot, i => i.Code.EndsWith($".{code}", StringComparison.Ordinal));
        Assert.Contains(core, i => i.Code.EndsWith($".{code}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RotItems_ContainOnlyTheApprovedCoreExceptions()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var rot = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Items
            .Where(i => !i.IsCustom).ToList();

        Assert.Equal(116, rot.Count);
        Assert.Equal(["backpack", "rope"], rot
            .Where(i => i.Source.StartsWith("Genesys Core Rulebook", StringComparison.Ordinal))
            .Select(i => i.Code[(i.Code.LastIndexOf('.') + 1)..])
            .OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public async Task NoBuiltInRotCareerReferencesGunnery()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var careers = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Careers.Where(c => !c.IsCustom);

        Assert.All(careers, c => Assert.DoesNotContain("Gunnery", c.CareerSkillNames));
    }

    [Fact]
    public async Task RotSheet_DoesNotListGunneryForANewCharacter()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth);
        var id = await CreateRotAsync(client, reference);

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

        Assert.DoesNotContain(sheet.Skills, s => s.Name == "Gunnery");
    }

    [Fact]
    public async Task CoreCharacter_CanStillBuyGunnery()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await ReferenceAsync(client, GameSystem.GenesysCore);
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Ядро", GameSystem.GenesysCore,
            reference.Archetypes.First(a => !a.IsCustom).Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        var gunnery = reference.Skills.Single(s => s.Name == "Gunnery");

        var buy = await client.PostAsync($"/api/characters/{id}/skills/{gunnery.Id}/buy-rank", null);

        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);
    }

    // ---- ROT-CLEAN-3.5: исключённые таланты ----

    [Theory]
    [InlineData("Rapid Reaction")]
    [InlineData("Surgeon")]
    [InlineData("Scathing Tirade")]
    [InlineData("Scathing Tirade (Improved)")]
    [InlineData("Scathing Tirade (Supreme)")]
    [InlineData("Just in Time!")]
    [InlineData("Indomitable")]
    [InlineData("Ruinous Repartee")]
    [InlineData("Attuned")]
    [InlineData("Counterspell")]
    [InlineData("Empowered Casting")]
    public async Task ExcludedTalent_IsNotOfferedInRot_ButRemainsInCore(string name)
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var rot = (await ReferenceAsync(client, GameSystem.RealmsOfTerrinoth)).Talents;
        var core = (await ReferenceAsync(client, GameSystem.GenesysCore)).Talents;

        Assert.DoesNotContain(rot, t => t.Name == name);
        Assert.Contains(core, t => t.Name == name);
    }

    private static async Task<Guid> CreateRotAsync(HttpClient client, ReferenceResponse reference)
    {
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Герой", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    }
}
