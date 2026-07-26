using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-SPECIES-01: полный каталог 14 видов RoT. Проверяется каждое значение таблицы ТЗ и
/// типизированное правило каждой способности — не количество и не текст описания.
/// </summary>
public class RotSpeciesCatalogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Одна строка published-таблицы вида: характеристики, пороги, XP и silhouette.</summary>
    public sealed record SpeciesProfile(
        string Name, int Brawn, int Agility, int Intellect, int Cunning, int Willpower, int Presence,
        int WoundBase, int StrainBase, int StartingXp, int Silhouette)
    {
        // Имя в выводе теста вместо дампа всех полей.
        public override string ToString() => Name;
    }

    public static IEnumerable<SpeciesProfile> Expected =>
    [
        new("Human",             2, 2, 2, 2, 2, 2, 10, 10, 110, 1),
        new("Deep Elf",          2, 3, 2, 2, 1, 2,  9, 10,  90, 1),
        new("Free Cities Elf",   2, 3, 2, 2, 1, 2,  9, 10,  90, 1),
        new("Highborn Elf",      2, 3, 2, 2, 1, 2,  9, 10,  90, 1),
        new("Lowborn Elf",       2, 3, 2, 2, 1, 2,  9, 10,  90, 1),
        new("Dunwarr Dwarf",     2, 1, 2, 2, 3, 2, 11, 10,  90, 1),
        new("Forge Dwarf",       2, 1, 2, 2, 3, 2, 11, 10,  90, 1),
        new("Broken Plains Orc", 3, 2, 2, 2, 2, 1, 12,  8, 100, 1),
        new("Stone-Dweller Orc", 3, 2, 2, 2, 2, 1, 12,  8, 100, 1),
        new("Sunderlands Orc",   3, 2, 2, 2, 2, 1, 12,  8, 100, 1),
        new("Catfolk",           2, 2, 1, 3, 2, 2,  9,  8,  90, 1),
        new("Half-Catfolk",      2, 2, 2, 2, 2, 2, 10,  9, 100, 1),
        new("Burrow Gnome",      1, 2, 2, 3, 1, 3,  6, 11,  90, 0),
        new("Wanderer Gnome",    1, 2, 2, 3, 1, 3,  6, 11,  90, 0),
    ];

    private async Task<ReferenceResponse> ReferenceAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        return (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
    }

    public static TheoryData<SpeciesProfile> Profiles() => [.. Expected];

    [Theory]
    [MemberData(nameof(Profiles))]
    public async Task EverySpecies_MatchesItsPublishedProfile(SpeciesProfile expected)
    {
        var reference = await ReferenceAsync();
        var species = reference.Archetypes.Single(a => a.Name == expected.Name);

        Assert.Equal(expected, new SpeciesProfile(
            species.Name, species.Brawn, species.Agility, species.Intellect, species.Cunning,
            species.Willpower, species.Presence, species.WoundBase, species.StrainBase,
            species.StartingXp, species.Silhouette));
    }

    [Fact]
    public async Task CatalogContainsExactlyTheFourteenRotSpecies()
    {
        var reference = await ReferenceAsync();
        var builtIn = reference.Archetypes.Where(a => !a.IsCustom).Select(a => a.Name).OrderBy(n => n).ToList();
        var expected = Expected.Select(p => p.Name).Order().ToList();

        Assert.Equal(expected, builtIn);
    }

    /// <summary>Каждая способность несёт исполняемый тип правила, а не только текст.</summary>
    [Theory]
    [InlineData("Human", "rot.archetype.human.ability.1", SpeciesAbilityRuleKind.MoveStoryPointToPlayers)]
    [InlineData("Free Cities Elf", "rot.archetype.free-cities-elf.ability.1", SpeciesAbilityRuleKind.SetBaseDefense)]
    [InlineData("Lowborn Elf", "rot.archetype.lowborn-elf.ability.1", SpeciesAbilityRuleKind.SetBaseDefense)]
    [InlineData("Dunwarr Dwarf", "rot.archetype.dunwarr-dwarf.ability.1", SpeciesAbilityRuleKind.RemoveSetbackBySource)]
    [InlineData("Dunwarr Dwarf", "rot.archetype.dunwarr-dwarf.ability.2", SpeciesAbilityRuleKind.ForceCriticalInjuryRoll)]
    [InlineData("Forge Dwarf", "rot.archetype.forge-dwarf.ability.1", SpeciesAbilityRuleKind.AddSetbackWhenTargeted)]
    [InlineData("Forge Dwarf", "rot.archetype.forge-dwarf.ability.2", SpeciesAbilityRuleKind.ForceCriticalInjuryRoll)]
    [InlineData("Broken Plains Orc", "rot.archetype.broken-plains-orc.ability.1", SpeciesAbilityRuleKind.OptionalSetbackForDamage)]
    [InlineData("Stone-Dweller Orc", "rot.archetype.stone-dweller-orc.ability.1", SpeciesAbilityRuleKind.StrainThresholdRage)]
    [InlineData("Sunderlands Orc", "rot.archetype.sunderlands-orc.ability.1", SpeciesAbilityRuleKind.BoostAgainstMarkedTarget)]
    [InlineData("Catfolk", "rot.archetype.catfolk.ability.1", SpeciesAbilityRuleKind.NaturalWeapon)]
    [InlineData("Catfolk", "rot.archetype.catfolk.ability.2", SpeciesAbilityRuleKind.FreeSecondMoveManeuver)]
    [InlineData("Half-Catfolk", "rot.archetype.half-catfolk.ability.1", SpeciesAbilityRuleKind.ChooseOneAbility)]
    [InlineData("Burrow Gnome", "rot.archetype.burrow-gnome.ability.1", SpeciesAbilityRuleKind.SetSilhouette)]
    [InlineData("Burrow Gnome", "rot.archetype.burrow-gnome.ability.2", SpeciesAbilityRuleKind.BoostAgainstLargerSilhouette)]
    [InlineData("Wanderer Gnome", "rot.archetype.wanderer-gnome.ability.1", SpeciesAbilityRuleKind.SetSilhouette)]
    [InlineData("Wanderer Gnome", "rot.archetype.wanderer-gnome.ability.2", SpeciesAbilityRuleKind.ConjureMinorItem)]
    public async Task AbilityCarriesItsExecutableRuleKind(string species, string code, SpeciesAbilityRuleKind expected)
    {
        var reference = await ReferenceAsync();
        var ability = reference.Archetypes.Single(a => a.Name == species).Abilities.Single(x => x.Code == code);

        Assert.Equal(expected, ability.RuleKind);
    }

    [Fact]
    public async Task ActivatedAbilities_CarryCostAndUseLimits()
    {
        var reference = await ReferenceAsync();

        var readyForAdventure = reference.Archetypes.Single(a => a.Name == "Human").Abilities[0];
        Assert.Equal(1, readyForAdventure.UsesPerScope);
        Assert.Equal(AbilityUseScope.Session, readyForAdventure.UseScope);
        Assert.Equal(0, readyForAdventure.StoryPointCost); // перенос Story Point, а не трата

        var toughAsNails = reference.Archetypes.Single(a => a.Name == "Dunwarr Dwarf")
            .Abilities.Single(x => x.RuleKind == SpeciesAbilityRuleKind.ForceCriticalInjuryRoll);
        Assert.Equal(1, toughAsNails.UsesPerScope);
        Assert.Equal(AbilityUseScope.Session, toughAsNails.UseScope);
        Assert.Equal(1, toughAsNails.StoryPointCost);

        var tricksy = reference.Archetypes.Single(a => a.Name == "Wanderer Gnome")
            .Abilities.Single(x => x.RuleKind == SpeciesAbilityRuleKind.ConjureMinorItem);
        Assert.Equal(1, tricksy.UsesPerScope);
        Assert.Equal(AbilityUseScope.Encounter, tricksy.UseScope);
        Assert.Equal(1, tricksy.StoryPointCost);
    }

    [Fact]
    public async Task DarkVisionRemovesTwoSetbacksAndOnlyFromDarkness()
    {
        var reference = await ReferenceAsync();
        var darkVision = reference.Archetypes.Single(a => a.Name == "Dunwarr Dwarf")
            .Abilities.Single(x => x.RuleKind == SpeciesAbilityRuleKind.RemoveSetbackBySource);

        Assert.Equal(2, darkVision.RuleValue);
        Assert.Contains("darkness", darkVision.RuleParameters);
    }

    [Fact]
    public async Task HalfCatfolkChoiceOffersExactlyTheTwoCatfolkAbilities()
    {
        var reference = await ReferenceAsync();
        var choice = reference.Archetypes.Single(a => a.Name == "Half-Catfolk").Abilities
            .Single(x => x.RuleKind == SpeciesAbilityRuleKind.ChooseOneAbility);

        Assert.Equal(
            ["rot.archetype.catfolk.ability.1", "rot.archetype.catfolk.ability.2"],
            choice.ChoiceOptions);
    }

    // ---- Обязательный выбор при создании ----

    private async Task<(HttpClient Client, ArchetypeDto HalfCatfolk, CareerDto Career)> HalfCatfolkSetupAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        return (client, reference.Archetypes.Single(a => a.Name == "Half-Catfolk"), reference.Careers.First(c => !c.IsCustom));
    }

    [Fact]
    public async Task HalfCatfolk_WithoutChoice_IsRejected()
    {
        var (client, species, career) = await HalfCatfolkSetupAsync();

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Без выбора", GameSystem.RealmsOfTerrinoth, species.Id, career.Id, null), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("species.choice.required",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task HalfCatfolk_WithUnknownChoice_IsRejected()
    {
        var (client, species, career) = await HalfCatfolkSetupAsync();

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Чужой выбор", GameSystem.RealmsOfTerrinoth, species.Id, career.Id, null,
            SpeciesAbilityChoiceCode: "rot.archetype.human.ability.1"), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("species.choice.unknown_option",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task HalfCatfolk_WithValidChoice_IsStoredAndReportedComplete()
    {
        var (client, species, career) = await HalfCatfolkSetupAsync();

        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Когтистый", GameSystem.RealmsOfTerrinoth, species.Id, career.Id, null,
            SpeciesAbilityChoiceCode: "rot.archetype.catfolk.ability.1"), Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        Assert.Equal("rot.archetype.catfolk.ability.1", sheet.SpeciesAbilityChoiceCode);
        Assert.False(sheet.SpeciesChoiceIncomplete);
    }

    [Fact]
    public async Task SpeciesWithoutChoice_RejectsAChoiceCode()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var deepElf = reference.Archetypes.Single(a => a.Name == "Deep Elf");

        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Лишний выбор", GameSystem.RealmsOfTerrinoth, deepElf.Id, reference.Careers.First(c => !c.IsCustom).Id, null,
            SpeciesAbilityChoiceCode: "rot.archetype.catfolk.ability.1"), Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("species.choice.not_applicable",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    // ---- Nimble на реальном листе ----

    [Fact]
    public async Task NimbleSpecies_HasDefenceOneOnTheSheet_WithoutAnyArmour()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var lowbornElf = reference.Archetypes.Single(a => a.Name == "Lowborn Elf");

        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Проворный", GameSystem.RealmsOfTerrinoth, lowbornElf.Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        Assert.Equal(1, sheet.Derived.MeleeDefense);
        Assert.Equal(1, sheet.Derived.RangedDefense);
        Assert.Empty(sheet.Items); // защита пришла от вида, а не от снаряжения
    }

    [Fact]
    public async Task GnomeSheet_ReportsSilhouetteZero()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var gnome = reference.Archetypes.Single(a => a.Name == "Burrow Gnome");

        var created = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Гном", GameSystem.RealmsOfTerrinoth, gnome.Id,
            reference.Careers.First(c => !c.IsCustom).Id, null), Json.Options);
        var id = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        Assert.Equal(0, sheet.Silhouette);
    }
}
