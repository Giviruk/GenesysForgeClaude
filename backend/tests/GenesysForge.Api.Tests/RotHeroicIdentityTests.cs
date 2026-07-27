using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

/// <summary>ROT-HA-01: обязательные личное название и происхождение героической способности.</summary>
public class RotHeroicIdentityTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    /// <summary>Персонаж RoT с выбранной способностью, но ещё без личности.</summary>
    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateWithAbilityAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var resp = await client.PostAsJsonAsync("/api/characters/",
            RotRequest(reference, "Герой"), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var setAbility = await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new SetHeroicAbilityRequest(reference.HeroicAbilities[0].Id), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, setAbility.StatusCode);
        return (client, id, reference);
    }

    /// <summary>Запрос создания RoT-персонажа: Human требует выбора стартовых навыков (ROT-CRE-01).</summary>
    private static CreateCharacterRequest RotRequest(ReferenceResponse reference, string name)
    {
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        return new CreateCharacterRequest(name, GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]);
    }

    private static Task<HttpResponseMessage> SetIdentityAsync(
        HttpClient client, Guid id, string? name,
        HeroicOriginMode? mode = HeroicOriginMode.Standard,
        HeroicOriginType? primary = HeroicOriginType.Destiny,
        HeroicOriginType? secondary = null, string? narrative = null) =>
        client.PutAsJsonAsync($"/api/characters/{id}/heroic-identity",
            new SetHeroicIdentityRequest(name, mode, primary, secondary, narrative), Json.Options);

    [Fact]
    public async Task Completion_RequiresIdentity_AndReportsMachineReason()
    {
        var (client, id, _) = await CreateWithAbilityAsync();

        var sheet = await SheetAsync(client, id);
        Assert.True(sheet.HeroicIdentityIncomplete);
        Assert.False(sheet.HeroicIdentity!.Complete);

        var blocked = await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var error = await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("heroic.identity.incomplete", error!.ReasonCode);

        Assert.Equal(HttpStatusCode.NoContent, (await SetIdentityAsync(client, id, "Клинок рассвета")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/complete-creation", null)).StatusCode);

        var after = await SheetAsync(client, id);
        Assert.False(after.IsCreationPhase);
        Assert.False(after.HeroicIdentityIncomplete);
        Assert.Equal("Клинок рассвета", after.HeroicIdentity!.CustomName);
        Assert.Equal(HeroicOriginType.Destiny, after.HeroicIdentity.OriginPrimary);
    }

    [Fact]
    public async Task CustomName_IsSeparateFromPrimaryEffectName()
    {
        var (client, id, reference) = await CreateWithAbilityAsync();
        await SetIdentityAsync(client, id, "Дыхание бури");

        var sheet = await SheetAsync(client, id);
        Assert.Equal(reference.HeroicAbilities[0].Name, sheet.HeroicAbility!.Name);
        Assert.Equal("Дыхание бури", sheet.HeroicIdentity!.CustomName);
        Assert.NotEqual(sheet.HeroicAbility.Name, sheet.HeroicIdentity.CustomName);
    }

    [Fact]
    public async Task Identity_IsImmutableAfterCompletion()
    {
        var (client, id, _) = await CreateWithAbilityAsync();
        await SetIdentityAsync(client, id, "Первое имя");
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var change = await SetIdentityAsync(client, id, "Второе имя");
        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
        var error = await change.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("heroic.identity.immutable", error!.ReasonCode);

        var roll = await client.PostAsync($"/api/characters/{id}/heroic-identity/roll-origin", null);
        Assert.Equal(HttpStatusCode.BadRequest, roll.StatusCode);

        Assert.Equal("Первое имя", (await SheetAsync(client, id)).HeroicIdentity!.CustomName);
    }

    [Fact]
    public async Task Identity_RequiresAbilityFirst()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var resp = await client.PostAsJsonAsync("/api/characters/",
            RotRequest(reference, "Без способности"), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var set = await SetIdentityAsync(client, id, "Имя");
        Assert.Equal(HttpStatusCode.BadRequest, set.StatusCode);
        var error = await set.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("heroic.ability.required", error!.ReasonCode);

        // Личности нет вовсе, пока способность не выбрана — пустой блок вместо выдуманного.
        Assert.Null((await SheetAsync(client, id)).HeroicIdentity);
    }

    [Theory]
    [InlineData(null, HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null, "heroic.identity.name_required")]
    [InlineData("Имя", HeroicOriginMode.Standard, null, null, null, "heroic.identity.origin_required")]
    [InlineData("Имя", HeroicOriginMode.Standard, HeroicOriginType.Destiny, HeroicOriginType.Patron, null,
        "heroic.identity.origin_second_not_allowed")]
    [InlineData("Имя", HeroicOriginMode.DoubleStandard, HeroicOriginType.Destiny, null, null,
        "heroic.identity.origin_second_required")]
    [InlineData("Имя", HeroicOriginMode.Custom, null, null, null, "heroic.identity.narrative_required")]
    [InlineData("Имя", HeroicOriginMode.Custom, HeroicOriginType.Destiny, null, "Текст",
        "heroic.identity.origin_not_allowed")]
    public async Task InvalidIdentity_IsRejectedWholesale(
        string? name, HeroicOriginMode mode, HeroicOriginType? primary,
        HeroicOriginType? secondary, string? narrative, string expectedReason)
    {
        var (client, id, _) = await CreateWithAbilityAsync();

        var resp = await SetIdentityAsync(client, id, name, mode, primary, secondary, narrative);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var error = await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal(expectedReason, error!.ReasonCode);

        // Ни одно поле не записано: валидация проходит целиком до первой мутации.
        var sheet = await SheetAsync(client, id);
        Assert.Null(sheet.HeroicIdentity!.CustomName);
        Assert.Null(sheet.HeroicIdentity.OriginMode);
    }

    [Fact]
    public async Task RolledOrigin_IsServerSide_AndSurvivesNameUpdate()
    {
        var (client, id, _) = await CreateWithAbilityAsync();

        var resp = await client.PostAsync($"/api/characters/{id}/heroic-identity/roll-origin", null);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var roll = (await resp.Content.ReadFromJsonAsync<HeroicOriginRollDto>(Json.Options))!;

        Assert.NotEmpty(roll.Rolls);
        // Специальный «0» всегда разрешается в две обычные категории и сам происхождением не остаётся.
        Assert.DoesNotContain(0, roll.Rolls[^1..]);
        if (roll.Rolls.Contains(0))
        {
            Assert.Equal(HeroicOriginMode.DoubleStandard, roll.OriginMode);
            Assert.NotNull(roll.OriginSecondary);
        }
        else
        {
            Assert.Equal(HeroicOriginMode.Standard, roll.OriginMode);
            Assert.Null(roll.OriginSecondary);
            Assert.Single(roll.Rolls);
        }

        // Название задаётся отдельно и не затирает выпавшее происхождение вместе с гранями.
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetIdentityAsync(client, id, "Наследие", mode: null, primary: null)).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal("Наследие", sheet.HeroicIdentity!.CustomName);
        Assert.Equal(roll.OriginMode, sheet.HeroicIdentity.OriginMode);
        Assert.Equal(roll.OriginPrimary, sheet.HeroicIdentity.OriginPrimary);
        Assert.Equal(roll.OriginSecondary, sheet.HeroicIdentity.OriginSecondary);
        Assert.Equal(roll.Rolls, sheet.HeroicIdentity.OriginRolls);
        Assert.True(sheet.HeroicIdentity.Complete);
    }

    [Fact]
    public async Task ManualOrigin_ClearsRollsOfPreviousRoll()
    {
        var (client, id, _) = await CreateWithAbilityAsync();
        await client.PostAsync($"/api/characters/{id}/heroic-identity/roll-origin", null);

        await SetIdentityAsync(client, id, "Своё", HeroicOriginMode.Custom, null, null, "Клятва мести");

        var sheet = await SheetAsync(client, id);
        Assert.Equal(HeroicOriginMode.Custom, sheet.HeroicIdentity!.OriginMode);
        Assert.Equal("Клятва мести", sheet.HeroicIdentity.OriginNarrative);
        Assert.Null(sheet.HeroicIdentity.OriginPrimary);
        Assert.Empty(sheet.HeroicIdentity.OriginRolls);
    }

    [Fact]
    public async Task Identity_SurvivesExportImportRoundTrip_AndDuplicate()
    {
        var (client, id, _) = await CreateWithAbilityAsync();
        await SetIdentityAsync(client, id, "Дар предков", HeroicOriginMode.DoubleStandard,
            HeroicOriginType.Bloodline, HeroicOriginType.WildMagic);
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal("Дар предков", export.Character.HeroicCustomName);
        Assert.Equal(HeroicOriginMode.DoubleStandard, export.Character.HeroicOriginMode);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importResp.StatusCode);
        var imported = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        var importedSheet = await SheetAsync(client, imported.CharacterId);
        Assert.Equal("Дар предков", importedSheet.HeroicIdentity!.CustomName);
        Assert.Equal(HeroicOriginType.Bloodline, importedSheet.HeroicIdentity.OriginPrimary);
        Assert.Equal(HeroicOriginType.WildMagic, importedSheet.HeroicIdentity.OriginSecondary);
        Assert.False(importedSheet.HeroicIdentityIncomplete);

        var dupResp = await client.PostAsync($"/api/characters/{id}/duplicate", null);
        Assert.Equal(HttpStatusCode.Created, dupResp.StatusCode);
        var dupId = (await dupResp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        var dupSheet = await SheetAsync(client, dupId);
        Assert.Equal("Дар предков", dupSheet.HeroicIdentity!.CustomName);
        Assert.False(dupSheet.HeroicIdentityIncomplete);
    }

    [Fact]
    public async Task TamperedImport_LeavesIdentityEmpty_WithWarning()
    {
        var (client, id, _) = await CreateWithAbilityAsync();
        await SetIdentityAsync(client, id, "Оригинал");
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        // Собственное происхождение вместе с категорией таблицы — несовместимая пара.
        var tampered = export with
        {
            Character = export.Character with
            {
                HeroicOriginMode = HeroicOriginMode.Custom,
                HeroicOriginPrimary = HeroicOriginType.Patron,
                HeroicOriginNarrative = "Подделка",
            },
        };

        var resp = await client.PostAsJsonAsync("/api/characters/import", tampered, Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var imported = (await resp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(imported.Warnings, w => w.Contains("Личность героической способности"));

        var sheet = await SheetAsync(client, imported.CharacterId);
        Assert.True(sheet.HeroicIdentityIncomplete);
        Assert.Null(sheet.HeroicIdentity!.CustomName);
    }

    [Fact]
    public async Task LegacyCompletedCharacter_BlocksUpgrades_UntilOneTimeRepair()
    {
        var (client, id, reference) = await CreateWithAbilityAsync();
        var ability = reference.HeroicAbilities.First(h => h.Upgrades.Count == 2);
        await client.PutAsJsonAsync($"/api/characters/{id}/heroic-ability",
            new SetHeroicAbilityRequest(ability.Id), Json.Options);
        await SetIdentityAsync(client, id, "Временное имя");
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards", new AwardXpRequest(50, null), Json.Options);

        // Экспорт без личности воспроизводит старого персонажа, созданного до ROT-HA-01.
        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        var legacy = export with
        {
            Character = export.Character with
            {
                HeroicCustomName = null,
                HeroicOriginMode = null,
                HeroicOriginPrimary = null,
                HeroicOriginSecondary = null,
                HeroicOriginRolls = null,
            },
        };
        var importResp = await client.PostAsJsonAsync("/api/characters/import", legacy, Json.Options);
        var legacyId = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!.CharacterId;

        var legacySheet = await SheetAsync(client, legacyId);
        Assert.False(legacySheet.IsCreationPhase);
        Assert.True(legacySheet.HeroicIdentityIncomplete);

        var blocked = await client.PutAsJsonAsync($"/api/characters/{legacyId}/heroic-upgrades",
            new SetHeroicUpgradesRequest(1, 0, 0, false, []), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        var error = await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("heroic.identity.incomplete", error!.ReasonCode);

        // Однократный ремонт разрешён владельцу и сразу закрывает запись обратно.
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetIdentityAsync(client, legacyId, "Восстановленное имя")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/api/characters/{legacyId}/heroic-upgrades",
                new SetHeroicUpgradesRequest(1, 0, 0, false, []), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await SetIdentityAsync(client, legacyId, "Ещё раз")).StatusCode);
    }

    [Fact]
    public async Task Identity_IsRecordedInHistory()
    {
        var (client, id, _) = await CreateWithAbilityAsync();
        await client.PostAsync($"/api/characters/{id}/heroic-identity/roll-origin", null);
        await SetIdentityAsync(client, id, "След судьбы", mode: null, primary: null);

        var history = await client.GetFromJsonAsync<List<CharacterAuditEntryDto>>(
            $"/api/characters/{id}/audit", Json.Options);

        Assert.Contains(history!, e => e.Action == CharacterAuditAction.HeroicOriginRolled);
        Assert.Contains(history!, e => e.Action == CharacterAuditAction.HeroicIdentitySet
            && e.Summary.Contains("След судьбы"));
    }

    [Fact]
    public async Task GenesysCore_HasNoHeroicIdentity()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        var resp = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Ядро", GameSystem.GenesysCore,
                reference.Archetypes.First(a => !a.IsCustom).Id, reference.Careers.First(c => !c.IsCustom).Id, null),
            Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var set = await SetIdentityAsync(client, id, "Имя");
        Assert.Equal(HttpStatusCode.BadRequest, set.StatusCode);
        var error = await set.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Equal("heroic.system_not_supported", error!.ReasonCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/complete-creation", null)).StatusCode);
    }
}
