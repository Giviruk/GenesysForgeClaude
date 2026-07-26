using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>ROT-HA-02: параметры Paragon, Sixth Sense и Signature Weapon.</summary>
public class RotHeroicParameterTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private static CreateCharacterRequest RotRequest(ReferenceResponse reference, string name)
    {
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        return new CreateCharacterRequest(name, GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]);
    }

    /// <summary>Персонаж RoT с выбранной способностью по коду и заполненной личностью.</summary>
    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateWithAbilityAsync(string code)
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var resp = await client.PostAsJsonAsync("/api/characters/", RotRequest(reference, "Герой"), Json.Options);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];

        var ability = reference.HeroicAbilities.First(h => h.Code == code);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/heroic-ability", new SetHeroicAbilityRequest(ability.Id), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/heroic-identity",
            new SetHeroicIdentityRequest("Имя", HeroicOriginMode.Standard, HeroicOriginType.Destiny, null, null),
            Json.Options)).StatusCode);
        return (client, id, reference);
    }

    private static Task<HttpResponseMessage> SetConfigAsync(
        HttpClient client, Guid id, SetHeroicConfigurationRequest req) =>
        client.PutAsJsonAsync($"/api/characters/{id}/heroic-configuration", req, Json.Options);

    private static SetHeroicConfigurationRequest Weapon(
        SignatureWeaponProfile profile = SignatureWeaponProfile.OneHanded,
        WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Dwarven,
        string form = "Фамильный меч",
        WeaponFormTraits traits = WeaponFormTraits.Sword) =>
        new(null, null, profile, craftsmanship, form, traits);

    [Fact]
    public async Task Paragon_RequiresSkill_AndBlocksCompletionUntilChosen()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.paragon");

        var sheet = await SheetAsync(client, id);
        Assert.Equal(HeroicParameterKind.ParagonSkill, sheet.HeroicConfiguration!.Kind);
        Assert.True(sheet.HeroicConfigurationIncomplete);

        var blocked = await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("heroic.parameter.incomplete",
            (await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var skill = reference.Skills.First(s => !s.IsCustom);
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, new(skill.Id, null, null, null, null, null))).StatusCode);

        var after = await SheetAsync(client, id);
        Assert.Equal(skill.Id, after.HeroicConfiguration!.ParagonSkillDefId);
        Assert.Equal(skill.Name, after.HeroicConfiguration.ParagonSkillName);
        Assert.False(after.HeroicConfiguration.ParagonSkillMissing);
        Assert.True(after.HeroicConfiguration.Complete);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/complete-creation", null)).StatusCode);
    }

    [Fact]
    public async Task Paragon_ForeignSkillId_IsRejected()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.paragon");

        var resp = await SetConfigAsync(client, id, new(Guid.NewGuid(), null, null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.parameter.skill_not_available",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task SixthSense_StoresTypedSubject_NotAFreeCharacterNote()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.sixth-sense");

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, new(null, "  духи предков  ", null, null, null, null))).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(HeroicParameterKind.SixthSenseSubject, sheet.HeroicConfiguration!.Kind);
        Assert.Equal("духи предков", sheet.HeroicConfiguration.SixthSenseSubject);
        Assert.Null(sheet.HeroicConfiguration.ParagonSkillDefId);
    }

    [Fact]
    public async Task ForeignParameterFields_AreRejected_NotIgnored()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.sixth-sense");
        var skill = reference.Skills.First(s => !s.IsCustom);

        var resp = await SetConfigAsync(client, id, new(skill.Id, "духи", null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.parameter.foreign_field",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task AbilityWithoutParameter_RejectsConfiguration_AndCompletesFreely()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.unleash");

        var resp = await SetConfigAsync(client, id, new(null, "что-нибудь", null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.parameter.not_applicable",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(HeroicParameterKind.None, sheet.HeroicConfiguration!.Kind);
        Assert.False(sheet.HeroicConfigurationIncomplete);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/complete-creation", null)).StatusCode);
    }

    [Fact]
    public async Task SignatureWeapon_NumbersComeFromProfile_NotFromClient()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.TwoHanded,
                WeaponCraftsmanship.Elven, "Двуручный молот", WeaponFormTraits.BluntOrCrushing))).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal("Melee (Heavy)", weapon.SkillName);
        Assert.Equal("Brawn + 5", weapon.Damage);
        Assert.Equal(3, weapon.Crit);
        Assert.Equal("Engaged", weapon.RangeBand);
        Assert.Equal(3, weapon.Encumbrance);
        Assert.Equal(2, weapon.HardPoints);
        Assert.Contains(weapon.Qualities, q => q.Code == "knockdown");
        Assert.Contains(weapon.Qualities, q => q.Code == "superior");
        // Группа профиля проставлена сервером, чужие признаки отброшены.
        Assert.True(weapon.FormTraits.HasFlag(WeaponFormTraits.TwoHanded));
        Assert.False(weapon.FormTraits.HasFlag(WeaponFormTraits.Ranged));
    }

    [Fact]
    public async Task SignatureWeapon_BrawlProfile_CarriesDisorientRating()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.Brawl,
            WeaponCraftsmanship.Steel, "Наручи", WeaponFormTraits.BluntOrCrushing));

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal("Brawn + 2", weapon.Damage);
        Assert.Equal(4, weapon.Crit);
        Assert.Equal(3, weapon.Qualities.Single(q => q.Code == "disorient").Rating);
    }

    [Fact]
    public async Task SignatureWeapon_ImpossibleFormTraits_AreRejected()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        var resp = await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.Ranged,
            WeaponCraftsmanship.Steel, "Лук-меч", WeaponFormTraits.Sword));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.traits_conflict",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task ChangingAbilityDuringCreation_DropsParameterOfTheOldEffect()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.paragon");
        var skill = reference.Skills.First(s => !s.IsCustom);
        await SetConfigAsync(client, id, new(skill.Id, null, null, null, null, null));

        var other = reference.HeroicAbilities.First(h => h.Code == "rot.heroic.sixth-sense");
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync(
            $"/api/characters/{id}/heroic-ability", new SetHeroicAbilityRequest(other.Id), Json.Options)).StatusCode);

        var sheet = await SheetAsync(client, id);
        Assert.Equal(HeroicParameterKind.SixthSenseSubject, sheet.HeroicConfiguration!.Kind);
        Assert.Null(sheet.HeroicConfiguration.ParagonSkillDefId);
        Assert.True(sheet.HeroicConfigurationIncomplete);
    }

    [Fact]
    public async Task Parameter_IsImmutableAfterCompletion()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.sixth-sense");
        await SetConfigAsync(client, id, new(null, "мёртвые", null, null, null, null));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var resp = await SetConfigAsync(client, id, new(null, "животные", null, null, null, null));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.parameter.immutable",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal("мёртвые", (await SheetAsync(client, id)).HeroicConfiguration!.SixthSenseSubject);
    }

    [Fact]
    public async Task LostWeapon_IsReplacedByASingleActiveInstance()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon());
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var lost = await client.PostAsJsonAsync($"/api/characters/{id}/heroic-configuration/signature-weapon",
            new ReplaceSignatureWeaponRequest(true, null, null, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, lost.StatusCode);
        Assert.True((await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!.IsLost);

        var replaced = await client.PostAsJsonAsync($"/api/characters/{id}/heroic-configuration/signature-weapon",
            new ReplaceSignatureWeaponRequest(false, SignatureWeaponProfile.Ranged, WeaponCraftsmanship.Elven,
                "Эльфийский лук", WeaponFormTraits.BowOrCrossbow), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.False(weapon.IsLost);
        Assert.Equal(SignatureWeaponProfile.Ranged, weapon.Profile);
        Assert.Equal("8", weapon.Damage);
        Assert.Equal("Эльфийский лук", weapon.NarrativeForm);
    }

    [Fact]
    public async Task Parameter_SurvivesExportImport_AndDuplicate()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.paragon");
        var skill = reference.Skills.First(s => !s.IsCustom);
        await SetConfigAsync(client, id, new(skill.Id, null, null, null, null, null));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(skill.Name, export.Character.ParagonSkillName);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        var imported = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        var importedSheet = await SheetAsync(client, imported.CharacterId);
        Assert.Equal(skill.Id, importedSheet.HeroicConfiguration!.ParagonSkillDefId);
        Assert.False(importedSheet.HeroicConfigurationIncomplete);

        var dupResp = await client.PostAsync($"/api/characters/{id}/duplicate", null);
        var dupId = (await dupResp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        Assert.Equal(skill.Id, (await SheetAsync(client, dupId)).HeroicConfiguration!.ParagonSkillDefId);
    }

    [Fact]
    public async Task LegacyCharacterWithoutParameter_BlocksUpgrades_UntilChosenOnce()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.paragon");
        var skill = reference.Skills.First(s => !s.IsCustom);
        await SetConfigAsync(client, id, new(skill.Id, null, null, null, null, null));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);
        await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards", new AwardXpRequest(50, null), Json.Options);

        // Файл без параметра воспроизводит персонажа, созданного до ROT-HA-02.
        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        var legacy = export with
        {
            Character = export.Character with { ParagonSkillCode = null, ParagonSkillName = null },
        };
        var importResp = await client.PostAsJsonAsync("/api/characters/import", legacy, Json.Options);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(result.Warnings, w => w.Contains("Параметр героической способности"));

        var legacyId = result.CharacterId;
        Assert.True((await SheetAsync(client, legacyId)).HeroicConfigurationIncomplete);

        var blocked = await client.PutAsJsonAsync($"/api/characters/{legacyId}/heroic-upgrades",
            new SetHeroicUpgradesRequest(0, 1, 0, false, []), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("heroic.parameter.incomplete",
            (await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, legacyId, new(skill.Id, null, null, null, null, null))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PutAsJsonAsync($"/api/characters/{legacyId}/heroic-upgrades",
                new SetHeroicUpgradesRequest(0, 1, 0, false, []), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await SetConfigAsync(client, legacyId, new(skill.Id, null, null, null, null, null))).StatusCode);
    }
}
