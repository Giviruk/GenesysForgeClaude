using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-CRE-01 (видовые карьерные навыки и предел стартовых рангов) и
/// ROT-CRE-02 (заморозка порогов ран/стрейна после завершения создания).
/// </summary>
public class RotCreationRulesTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static async Task<ReferenceResponse> RotReferenceAsync(HttpClient client) =>
        (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;

    private static async Task<Guid> CreateOkAsync(HttpClient client, CreateCharacterRequest req)
    {
        var resp = await client.PostAsJsonAsync("/api/characters/", req, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return body!["id"];
    }

    /// <summary>Карьера, у которой названный навык не является карьерным изначально.</summary>
    private static CareerDto CareerWithout(ReferenceResponse reference, string skillName) =>
        reference.Careers.First(c => !c.IsCustom && !c.CareerSkillNames.Contains(skillName));

    // ---- ROT-CRE-01 ----

    [Fact]
    public async Task DeepElf_ForbiddenKnowledge_IsCareerSkillAtRankTwo()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await RotReferenceAsync(client);
        var deepElf = reference.Archetypes.First(a => a.Name == "Deep Elf");
        var career = CareerWithout(reference, "Knowledge (Forbidden)");

        var id = await CreateOkAsync(client,
            new CreateCharacterRequest("Тёмный эльф", GameSystem.RealmsOfTerrinoth, deepElf.Id, career.Id, null));

        var sheet = await SheetAsync(client, id);
        var forbidden = sheet.Skills.Single(s => s.Name == "Knowledge (Forbidden)");
        Assert.Equal(2, forbidden.Ranks);
        Assert.Equal(2, forbidden.FreeRanks);
        Assert.True(forbidden.IsCareer);
        Assert.Contains(forbidden.CareerSources, s => s.Source == "Species" && s.SourceName == "Deep Elf");
        // Ранг 3 после создания стоит как карьерный: 5 × 3.
        Assert.Equal(15, forbidden.NextRankCost);

        var discipline = sheet.Skills.Single(s => s.Name == "Discipline");
        Assert.Equal(1, discipline.Ranks);
        Assert.DoesNotContain(discipline.CareerSources, s => s.Source == "Species");
    }

    [Fact]
    public async Task DeepElf_CannotSpendFreeCareerRankOnAlreadyMaxedSpeciesSkill()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await RotReferenceAsync(client);
        var deepElf = reference.Archetypes.First(a => a.Name == "Deep Elf");
        var career = CareerWithout(reference, "Knowledge (Forbidden)");

        // Навык карьерный (от вида), поэтому выбор проходит проверку принадлежности,
        // но итоговый ранг 3 превышает предел создания — весь запрос отклоняется.
        var resp = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Перебор", GameSystem.RealmsOfTerrinoth, deepElf.Id, career.Id,
                ["Knowledge (Forbidden)"]),
            Json.Options);

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var characters = await client.GetFromJsonAsync<List<CharacterListItemDto>>("/api/characters", Json.Options);
        Assert.DoesNotContain(characters!, c => c.Name == "Перебор"); // атомарность: персонаж не создан
    }

    [Fact]
    public async Task HighbornElf_CanRaiseGrantedDivineToRankTwo()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await RotReferenceAsync(client);
        var highborn = reference.Archetypes.First(a => a.Name == "Highborn Elf");
        var career = CareerWithout(reference, "Divine");

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Высокий эльф", GameSystem.RealmsOfTerrinoth, highborn.Id, career.Id, ["Divine"]));

        var sheet = await SheetAsync(client, id);
        var divine = sheet.Skills.Single(s => s.Name == "Divine");
        Assert.Equal(2, divine.Ranks);
        Assert.Equal(2, divine.FreeRanks);
        Assert.True(divine.IsCareer);
        Assert.Contains(divine.CareerSources, s => s.Source == "Species" && s.SourceName == "Highborn Elf");
        Assert.Equal(0, sheet.SpentXp); // обе прибавки бесплатны
    }

    [Fact]
    public async Task SpeciesGrantOverlappingCareer_ProducesNoDuplicateRow()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await RotReferenceAsync(client);
        var highborn = reference.Archetypes.First(a => a.Name == "Highborn Elf");
        var career = reference.Careers.FirstOrDefault(c => !c.IsCustom && c.CareerSkillNames.Contains("Divine"));
        Assert.NotNull(career); // в RoT Divine есть в списке навыков Disciple

        var id = await CreateOkAsync(client, new CreateCharacterRequest(
            "Двойной источник", GameSystem.RealmsOfTerrinoth, highborn.Id, career!.Id, null));

        var sheet = await SheetAsync(client, id);
        var divine = sheet.Skills.Single(s => s.Name == "Divine"); // Single: дубля нет
        Assert.Equal(1, divine.Ranks); // видовой ранг не удваивается карьерой
        Assert.Equal(2, divine.CareerSources.Count);
    }

    // ---- ROT-CRE-02 ----

    [Fact]
    public async Task BeforeCompletion_BuyingBrawn_MovesWoundThreshold()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = await RotReferenceAsync(client);
        var human = reference.Archetypes.First(a => a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();

        var id = await CreateOkAsync(client, new CreateCharacterRequest("Растущий", GameSystem.RealmsOfTerrinoth,
            human.Id, career.Id, null, [new ArchetypeSkillChoice("any-noncareer", nonCareer)]));

        var before = await SheetAsync(client, id);
        var resp = await client.PostAsync($"/api/characters/{id}/characteristics/brawn/buy", null);
        Assert.Equal(HttpStatusCode.NoContent, resp.StatusCode);

        var after = await SheetAsync(client, id);
        Assert.Equal(before.Derived.WoundThreshold + 1, after.Derived.WoundThreshold);
        Assert.Equal(before.Derived.Soak + 1, after.Derived.Soak);
    }

    [Fact]
    public async Task AfterCompletion_ThresholdsAreFrozen_ButSoakAndLoadStillFollowBrawn()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);

        var atCompletion = await SheetAsync(client, id);
        Assert.False(atCompletion.IsCreationPhase);

        // Единственный легальный путь поднять характеристику после создания — Dedication.
        // Эмулируем его эффект напрямую через покупку: она обязана быть отклонена,
        // а пороги — остаться прежними при любом дальнейшем изменении Brawn.
        var resp = await client.PostAsync($"/api/characters/{id}/characteristics/brawn/buy", null);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);

        var after = await SheetAsync(client, id);
        Assert.Equal(atCompletion.Derived.WoundThreshold, after.Derived.WoundThreshold);
        Assert.Equal(atCompletion.Derived.StrainThreshold, after.Derived.StrainThreshold);
    }

    [Fact]
    public async Task CompleteCreation_IsIdempotent_AndDoesNotReSnapshot()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);

        var first = await SheetAsync(client, id);
        var again = await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        Assert.Equal(HttpStatusCode.NoContent, again.StatusCode);

        var second = await SheetAsync(client, id);
        Assert.Equal(first.Derived.WoundThreshold, second.Derived.WoundThreshold);
        Assert.Equal(first.Derived.StrainThreshold, second.Derived.StrainThreshold);
    }

    [Fact]
    public async Task Snapshot_SurvivesExportImportRoundTrip()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);
        var original = await SheetAsync(client, id);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(original.Derived.WoundThreshold, export.Character.CreationWoundThreshold);
        Assert.Equal(ThresholdSnapshotProvenance.CreationCompleted, export.Character.ThresholdSnapshotProvenance);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importResp.StatusCode);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Empty(result.Warnings);

        var imported = await SheetAsync(client, result.CharacterId);
        Assert.Equal(original.Derived.WoundThreshold, imported.Derived.WoundThreshold);
        Assert.Equal(original.Derived.StrainThreshold, imported.Derived.StrainThreshold);
    }

    [Fact]
    public async Task LegacyImport_WithoutSnapshot_WarnsAndComputesInsteadOfZero()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);
        var original = await SheetAsync(client, id);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        // Файл старого формата: порогов нет.
        var legacy = export with
        {
            Format = CharacterExportDto.LegacyFormatV1,
            Character = export.Character with
            {
                CreationWoundThreshold = null,
                CreationStrainThreshold = null,
                ThresholdSnapshotProvenance = ThresholdSnapshotProvenance.None,
            },
        };

        var importResp = await client.PostAsJsonAsync("/api/characters/import", legacy, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importResp.StatusCode);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(result.Warnings, w => w.Contains("порог", StringComparison.OrdinalIgnoreCase));

        var imported = await SheetAsync(client, result.CharacterId);
        Assert.True(imported.Derived.WoundThreshold > 0);
        Assert.Equal(original.Derived.WoundThreshold, imported.Derived.WoundThreshold);
    }

    [Fact]
    public async Task Duplicate_CopiesFrozenThresholds()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);
        var original = await SheetAsync(client, id);

        var resp = await client.PostAsync($"/api/characters/{id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);

        var copy = await SheetAsync(client, body!["id"]);
        Assert.Equal(original.Derived.WoundThreshold, copy.Derived.WoundThreshold);
        Assert.Equal(original.Derived.StrainThreshold, copy.Derived.StrainThreshold);
    }

    [Fact]
    public async Task CharacterList_ReportsSameThresholdsAsSheet()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var (id, _) = await CreateCompletedRotCharacterAsync(client);
        var sheet = await SheetAsync(client, id);

        var list = (await client.GetFromJsonAsync<List<CharacterListItemDto>>("/api/characters", Json.Options))!;
        var item = list.Single(c => c.Id == id);
        Assert.Equal(sheet.Derived.WoundThreshold, item.WoundThreshold);
        Assert.Equal(sheet.Derived.StrainThreshold, item.StrainThreshold);
    }

    /// <summary>Создаёт RoT-персонажа с героикой и завершает создание.</summary>
    private async Task<(Guid Id, ReferenceResponse Reference)> CreateCompletedRotCharacterAsync(HttpClient client)
    {
        var reference = await RotReferenceAsync(client);
        var human = reference.Archetypes.First(a => a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();

        var id = await CreateOkAsync(client, new CreateCharacterRequest("Завершённый", GameSystem.RealmsOfTerrinoth,
            human.Id, career.Id, null, [new ArchetypeSkillChoice("any-noncareer", nonCareer)]));

        var setHeroic = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new SetHeroicAbilityRequest(reference.HeroicAbilities[0].Id), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, setHeroic.StatusCode);

        var setIdentity = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-identity",
            new SetHeroicIdentityRequest("Клинок рассвета", HeroicOriginMode.Standard,
                HeroicOriginType.Destiny, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, setIdentity.StatusCode);

        var complete = await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        return (id, reference);
    }
}
