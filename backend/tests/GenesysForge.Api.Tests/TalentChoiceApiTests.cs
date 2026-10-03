using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class TalentChoiceApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task AnimalCompanion_RequiresAndPersistsApprovedCompanion()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Companion Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
            [career.CareerSkillNames[0]]));
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, 100, null, null));

        // Пирамида 3/2 открывает первый ранг таланта тира 3. Берём только таланты без выбора,
        // чтобы подготовка теста не подменяла проверяемый контракт.
        foreach (var tier in new[] { 1, 2 })
        {
            var count = tier == 1 ? 3 : 2;
            var fillers = reference.Talents.Where(t => t.Tier == tier && !t.IsRanked
                    && t.ChoiceKind == TalentChoiceKind.None
                    && string.IsNullOrEmpty(t.RequiresTalentCode)
                    && (t.ExcludesTalentCodes?.Count ?? 0) == 0)
                .Take(count).ToList();
            Assert.Equal(count, fillers.Count);
            foreach (var filler in fillers)
            {
                var response = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                    new BuyTalentRequest(filler.Id));
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        var companion = reference.Talents.Single(t => t.Name == "Animal Companion");
        Assert.Equal(TalentChoiceKind.AnimalCompanion, companion.ChoiceKind);
        async Task<NpcDetailDto> CreateCompanionAsync(
            string name, int silhouette, List<string>? tags = null)
        {
            var response = await client.PostAsJsonAsync("/api/npcs/", new NpcInput(
                name, GameSystem.GenesysCore, NpcKind.Minion, NpcRole.Skirmisher,
                "", "", 1, 3, 1, 2, 1, 1, 5, null, 1, 0, 0, silhouette, "",
                NpcVisibility.Private, null, [], [], [], [], [], tags ?? ["animal"]));
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        }

        var selectedCompanion = await CreateCompanionAsync("Серый сокол", 0);
        var tooLargeCompanion = await CreateCompanionAsync("Большой волк", 1);
        var humanoid = await CreateCompanionAsync("Городской стражник", 0, ["человек"]);
        var before = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;

        var missing = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id));
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode);
        var afterMissing = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.SpentXp, afterMissing.SpentXp);
        Assert.DoesNotContain(afterMissing.Talents!, t => t.TalentDefId == companion.Id);

        var nonAnimal = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id, Choices: [humanoid.Id.ToString()]));
        Assert.Equal(HttpStatusCode.BadRequest, nonAnimal.StatusCode);
        var afterNonAnimal = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.SpentXp, afterNonAnimal.SpentXp);
        Assert.DoesNotContain(afterNonAnimal.Talents!, t => t.TalentDefId == companion.Id);

        var tooLarge = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id, Choices: [tooLargeCompanion.Id.ToString()]));
        Assert.Equal(HttpStatusCode.BadRequest, tooLarge.StatusCode);
        var afterTooLarge = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        Assert.Equal(before.SpentXp, afterTooLarge.SpentXp);
        Assert.DoesNotContain(afterTooLarge.Talents!, t => t.TalentDefId == companion.Id);

        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(companion.Id, Choices: [selectedCompanion.Id.ToString()]));
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{id}", Json.Options))!;
        var owned = Assert.Single(sheet.Talents!, t => t.TalentDefId == companion.Id);
        Assert.Equal("zhivotnoe-sputnik", owned.LinkCode);
        var choice = Assert.Single(owned.Choices!);
        Assert.Equal(TalentChoiceKind.AnimalCompanion, choice.Kind);
        Assert.Equal(selectedCompanion.Id.ToString(), choice.Value);
        Assert.Equal(selectedCompanion.Name, choice.DisplayName);
    }

    [Fact]
    public async Task Reference_ExposesChoiceRulesForTheClient()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;

        var knack = reference.Talents.Single(t => t.LinkCode == "kvalifikatsiya");
        Assert.Equal(TalentChoiceKind.Skill, knack.ChoiceKind);
        Assert.Equal((1, 2), (knack.ChoiceCountFirstRank, knack.ChoiceCountNextRank));
        Assert.True(knack.ChoiceDistinctAcrossRanks);
        Assert.Equal([SkillKind.General, SkillKind.Knowledge, SkillKind.Social], knack.ChoiceAllowedSkillKinds!);

        var heroicWill = reference.Talents.Single(t => t.LinkCode == "geroicheskaya-volya");
        Assert.Equal(TalentChoiceKind.Characteristic, heroicWill.ChoiceKind);
        Assert.Equal(2, heroicWill.ChoiceCountFirstRank);
        Assert.Empty(heroicWill.ChoiceAllowedSkillKinds!);
    }

    [Fact]
    public async Task HeroicWill_RequiresTwoDistinctCharacteristics()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, reference) = await CreateCharacterWithPyramidAsync(client, 3, 2);
        var heroicWill = reference.Talents.Single(t => t.LinkCode == "geroicheskaya-volya");
        var before = await SheetAsync(client, id);

        foreach (var invalid in new List<string>[] { [], ["Willpower"], ["Willpower", "willpower"], ["Willpower", "42"] })
        {
            var rejected = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                new BuyTalentRequest(heroicWill.Id, Choices: invalid));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        var afterRejected = await SheetAsync(client, id);
        Assert.Equal(before.SpentXp, afterRejected.SpentXp);
        Assert.DoesNotContain(afterRejected.Talents!, t => t.TalentDefId == heroicWill.Id);

        // Клиент присылает характеристики в camelCase — сервер хранит каноническое имя enum.
        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(heroicWill.Id, Choices: ["willpower", "Brawn"]));
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var owned = Assert.Single((await SheetAsync(client, id)).Talents!, t => t.TalentDefId == heroicWill.Id);
        var chosen = owned.Choices!.OrderBy(c => c.Value, StringComparer.Ordinal).ToList();
        Assert.Equal(["Brawn", "Willpower"], chosen.Select(c => c.Value));
        Assert.Equal(["Мощь", "Воля"], chosen.Select(c => c.DisplayName));
        Assert.All(owned.Choices!, c => Assert.Equal(0, c.RankIndex));
    }

    [Fact]
    public async Task KnackForIt_TakesOneSkillThenTwoNewNonCombatSkills()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, reference) = await CreateCharacterWithPyramidAsync(client, 1);
        var knack = reference.Talents.Single(t => t.LinkCode == "kvalifikatsiya");
        var allowed = reference.Skills
            .Where(s => s.Kind is SkillKind.General or SkillKind.Knowledge or SkillKind.Social)
            .Take(3).ToList();
        var combat = reference.Skills.First(s => s.Kind == SkillKind.Combat);

        var forbidden = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(knack.Id, Choices: [combat.Name]));
        Assert.Equal(HttpStatusCode.BadRequest, forbidden.StatusCode);

        var first = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(knack.Id, Choices: [allowed[0].Name]));
        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);

        // Второй ранг: ровно два навыка, и выбранный ранее повторять нельзя.
        foreach (var invalid in new List<string>[] { [allowed[1].Name], [allowed[0].Name, allowed[1].Name] })
        {
            var rejected = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                new BuyTalentRequest(knack.Id, Choices: invalid));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }

        var second = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(knack.Id, Choices: [allowed[1].Name, allowed[2].Name]));
        Assert.Equal(HttpStatusCode.NoContent, second.StatusCode);

        var owned = Assert.Single((await SheetAsync(client, id)).Talents!, t => t.TalentDefId == knack.Id);
        Assert.Equal(2, owned.Ranks);
        var byValue = owned.Choices!.ToDictionary(c => c.Value);
        Assert.Equal(3, byValue.Count);
        Assert.Equal(0, byValue[allowed[0].Name].RankIndex);
        Assert.Equal(1, byValue[allowed[1].Name].RankIndex);
        Assert.Equal(1, byValue[allowed[2].Name].RankIndex);
        Assert.All(allowed, s => Assert.Equal(s.NameRu, byValue[s.Name].DisplayName));
    }

    [Fact]
    public async Task SignatureSpell_ValidatesConfigurationAgainstMagicCatalog()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, reference) = await CreateCharacterWithPyramidAsync(client, 2);
        var signature = reference.Talents.Single(t => t.LinkCode == "signature-spell");
        Assert.Equal(TalentChoiceKind.SpellConfiguration, signature.ChoiceKind);

        var spells = (await client.GetFromJsonAsync<List<SpellDto>>(
            "/api/spells/GenesysCore", Json.Options))!;
        var effects = spells.Where(s => s.Kind == SpellEntryKind.AdditionalEffect).ToList();
        var actions = spells.Where(s => s.Kind == SpellEntryKind.Effect).ToList();
        bool Offers(SpellDto action, SpellDto effect) =>
            effects.Any(e => e.ParentEffect == action.NameEn && e.NameEn == effect.NameEn);
        // Неповторяемый эффект без взаимоисключений, которого нет хотя бы у одного другого действия.
        var single = effects.First(e => !e.Repeatable && (e.Exclusions?.Count ?? 0) == 0
            && actions.Any(a => !Offers(a, e)));
        var action = actions.First(s => s.NameEn == single.ParentEffect);
        var otherAction = actions.First(a => !Offers(a, single));

        foreach (var invalid in new[]
        {
            action.NameEn,
            $"{action.NameEn}|{single.NameEn}|{single.NameEn}",
            $"{otherAction.NameEn}|{single.NameEn}",
            $"Unknown action|{single.NameEn}",
        })
        {
            var rejected = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                new BuyTalentRequest(signature.Id, Choices: [invalid]));
            Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        }
        Assert.DoesNotContain((await SheetAsync(client, id)).Talents!, t => t.TalentDefId == signature.Id);

        var buy = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
            new BuyTalentRequest(signature.Id, Choices: [$"{action.NameEn}|{single.NameEn}"]));
        Assert.Equal(HttpStatusCode.NoContent, buy.StatusCode);

        var owned = Assert.Single((await SheetAsync(client, id)).Talents!, t => t.TalentDefId == signature.Id);
        var choice = Assert.Single(owned.Choices!);
        Assert.Equal($"{action.NameEn}|{single.NameEn}", choice.Value);
        Assert.Equal($"{action.NameRu}: {single.NameRu}", choice.DisplayName);
    }

    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    /// <summary>
    /// Персонаж Genesys Core со 100 XP и пирамидой из неранговых талантов без выбора: по одному
    /// числу на тир, начиная с первого.
    /// </summary>
    private static async Task<(Guid Id, ReferenceResponse Reference)> CreateCharacterWithPyramidAsync(
        HttpClient client, params int[] countsByTier)
    {
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Choice Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
            [career.CareerSkillNames[0]]));
        var id = (await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PatchAsJsonAsync($"/api/characters/{id}",
            new UpdateCharacterRequest(null, 100, null, null));

        for (var tier = 1; tier <= countsByTier.Length; tier++)
        {
            var count = countsByTier[tier - 1];
            var fillers = reference.Talents.Where(t => t.Tier == tier && !t.IsRanked
                    && t.ChoiceKind == TalentChoiceKind.None
                    && string.IsNullOrEmpty(t.RequiresTalentCode)
                    && (t.ExcludesTalentCodes?.Count ?? 0) == 0)
                .Take(count).ToList();
            Assert.Equal(count, fillers.Count);
            foreach (var filler in fillers)
            {
                var response = await client.PostAsJsonAsync($"/api/characters/{id}/talents/buy",
                    new BuyTalentRequest(filler.Id));
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }
        return (id, reference);
    }
}
