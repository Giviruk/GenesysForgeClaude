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
        WeaponFormTraits traits = WeaponFormTraits.Sword,
        Guid? baseAttachmentId = null) =>
        new(null, null, profile, craftsmanship, form, traits, baseAttachmentId);

    /// <summary>Улучшение «на любое оружие»: подходит любой форме, поэтому годится всем тестам.</summary>
    private static Guid AnyWeaponAttachment(ReferenceResponse reference) =>
        Attachment(reference, "runic-thunder");

    /// <summary>Код в каталоге с префиксом системы, поэтому ищем по хвосту — как в тестах улучшений.</summary>
    private static Guid Attachment(ReferenceResponse reference, string code) =>
        reference.Attachments!.Single(a => a.Code.EndsWith($".{code}", StringComparison.Ordinal)).Id;

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
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.TwoHanded,
                WeaponCraftsmanship.Elven, "Двуручный молот", WeaponFormTraits.BluntOrCrushing,
                AnyWeaponAttachment(reference)))).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal("Melee (Heavy)", weapon.SkillName);
        // Профиль даёт «Brawn + 5» и крит 3, эльфийская работа снимает по единице с обоих
        // (ROT-WPN-02): у именного оружия те же правила, что у любого другого.
        Assert.Equal("Brawn + 4", weapon.Damage);
        Assert.Equal(2, weapon.Crit);
        Assert.Equal("Engaged", weapon.RangeBand);
        Assert.Equal(3, weapon.Encumbrance);
        Assert.Equal(2, weapon.HardPoints);
        Assert.Contains(weapon.Qualities, q => q.Code == "knockdown");
        Assert.Contains(weapon.Qualities, q => q.Code == "superior");
        // Группа профиля проставлена сервером, чужие признаки отброшены.
        Assert.True(weapon.FormTraits.HasFlag(WeaponFormTraits.TwoHanded));
        Assert.False(weapon.FormTraits.HasFlag(WeaponFormTraits.Ranged));
    }

    [Theory]
    [InlineData(WeaponCraftsmanship.Iron)]
    [InlineData(WeaponCraftsmanship.Ancient)]
    public async Task CraftsmanshipOutsideTheAbility_IsRejected(WeaponCraftsmanship craftsmanship)
    {
        // Железа книга именному оружию не даёт вовсе, а древняя работа — награда за Improved
        // (ROT-HA-05), а не бесплатный выбор на старте.
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        var resp = await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            craftsmanship, "Клинок", WeaponFormTraits.Sword, AnyWeaponAttachment(reference)));

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.craftsmanship_not_allowed",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task DwarvenSignatureWeapon_HitsHarderAndWeighsMore()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
                WeaponCraftsmanship.Dwarven, "Клинок предков", WeaponFormTraits.Sword,
                AnyWeaponAttachment(reference)))).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        // Одноручный профиль — «Brawn + 3», крит 3, 2 слота; гномья работа даёт +1 урона и +1 веса.
        Assert.Equal("Brawn + 4", weapon.Damage);
        Assert.Equal(3, weapon.Crit);
        Assert.Equal(2, weapon.Encumbrance);
        Assert.Equal(SignatureWeaponImprovement.None, weapon.Improvement);
        Assert.DoesNotContain(weapon.Qualities, q => q.Code == "reinforced");
    }

    [Fact]
    public async Task SignatureWeapon_BrawlProfile_CarriesDisorientRating()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.Brawl,
            WeaponCraftsmanship.Steel, "Наручи", WeaponFormTraits.BluntOrCrushing,
            AnyWeaponAttachment(reference)));

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
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(baseAttachmentId: AnyWeaponAttachment(reference)));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var lost = await client.PostAsJsonAsync($"/api/characters/{id}/heroic-configuration/signature-weapon",
            new ReplaceSignatureWeaponRequest(true, null, null, null, null), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, lost.StatusCode);
        Assert.True((await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!.IsLost);

        var replaced = await client.PostAsJsonAsync($"/api/characters/{id}/heroic-configuration/signature-weapon",
            new ReplaceSignatureWeaponRequest(false, SignatureWeaponProfile.Ranged, WeaponCraftsmanship.Elven,
                "Эльфийский лук", WeaponFormTraits.BowOrCrossbow, AnyWeaponAttachment(reference)), Json.Options);
        Assert.Equal(HttpStatusCode.NoContent, replaced.StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.False(weapon.IsLost);
        Assert.Equal(SignatureWeaponProfile.Ranged, weapon.Profile);
        // Дальнобойный профиль — урон 8, эльфийская работа уменьшает его на единицу (ROT-WPN-02).
        Assert.Equal("7", weapon.Damage);
        Assert.Equal("Эльфийский лук", weapon.NarrativeForm);
    }

    // ── ROT-HA-02: базовое улучшение именного оружия ──

    [Fact]
    public async Task SignatureWeapon_WithoutBaseAttachment_IsIncomplete()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        var resp = await SetConfigAsync(client, id, Weapon());
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.attachment_required",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.True((await SheetAsync(client, id)).HeroicConfigurationIncomplete);
    }

    [Fact]
    public async Task BaseAttachment_ChangesDamageCritAndQualities_ButNotHardPoints()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        // Острое лезвие даёт Проникающее 2 и снимает единицу крита; клинок подтверждён формой.
        Assert.Equal(HttpStatusCode.NoContent,
            (await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
                WeaponCraftsmanship.Steel, "Фамильный меч", WeaponFormTraits.Sword,
                Attachment(reference, "razor-edge")))).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.EndsWith(".razor-edge", weapon.BaseAttachment!.Code, StringComparison.Ordinal);
        Assert.Equal(2, weapon.Qualities.Single(q => q.Code == "pierce").Rating);
        Assert.Contains(weapon.Qualities, q => q.Code == "superior");
        Assert.Equal(2, weapon.Crit);
        // Слоты и вес героической копии улучшение не трогает: оно временное и ничего не занимает.
        Assert.Equal(2, weapon.HardPoints);
        Assert.Equal(1, weapon.Encumbrance);
    }

    [Fact]
    public async Task BaseAttachment_IncompatibleWithTheForm_IsRejected()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        // Взрывной снаряд требует дальнобойной формы, а профиль ближний.
        var resp = await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            WeaponCraftsmanship.Steel, "Фамильный меч", WeaponFormTraits.Sword,
            Attachment(reference, "explosive-missile")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.attachment_incompatible",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task BaseAttachment_GivingAQualityTheProfileAlreadyHas_IsRejected()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        // Превосходная модификация выдаёт Превосходное, а оно есть у всех четырёх профилей.
        var resp = await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            WeaponCraftsmanship.Steel, "Фамильный меч", WeaponFormTraits.Sword,
            Attachment(reference, "superior-weapon-customization")));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.attachment_redundant",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task BaseAttachment_ForeignId_IsRejected()
    {
        var (client, id, _) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");

        var resp = await SetConfigAsync(client, id, Weapon(baseAttachmentId: Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.attachment_not_available",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task BaseAttachment_SurvivesExportImport_ByCode()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            WeaponCraftsmanship.Steel, "Фамильный меч", WeaponFormTraits.Sword,
            Attachment(reference, "razor-edge")));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.EndsWith(".razor-edge", export.Character.SignatureWeaponBaseAttachmentCode!, StringComparison.Ordinal);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        var imported = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        var sheet = await SheetAsync(client, imported.CharacterId);
        Assert.EndsWith(".razor-edge",
            sheet.HeroicConfiguration!.SignatureWeapon!.BaseAttachment!.Code, StringComparison.Ordinal);
        Assert.False(sheet.HeroicConfigurationIncomplete);
    }

    [Fact]
    public async Task ImportedWeapon_WithTamperedAttachmentCode_LosesIt_WithAWarning()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            WeaponCraftsmanship.Steel, "Фамильный меч", WeaponFormTraits.Sword,
            Attachment(reference, "razor-edge")));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        // Подменённый в файле код требует дальнобойной формы — импорт не должен его принять.
        var tampered = export with
        {
            Character = export.Character with
            {
                SignatureWeaponBaseAttachmentCode = export.Character.SignatureWeaponBaseAttachmentCode!
                    .Replace("razor-edge", "explosive-missile", StringComparison.Ordinal),
            },
        };

        var importResp = await client.PostAsJsonAsync("/api/characters/import", tampered, Json.Options);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(result.Warnings, w => w.Contains("Базовое улучшение"));

        var sheet = await SheetAsync(client, result.CharacterId);
        Assert.Null(sheet.HeroicConfiguration!.SignatureWeapon!.BaseAttachment);
        // Оружие без базового улучшения не собрано: параметр остаётся незавершённым.
        Assert.True(sheet.HeroicConfigurationIncomplete);
    }

    // ── ROT-HA-05: Improved и Supreme именного оружия ──

    /// <summary>Готовое к улучшениям оружие: параметр выбран, создание завершено, XP начислен.</summary>
    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> WeaponReadyForUpgradesAsync(
        int xp, WeaponCraftsmanship craftsmanship = WeaponCraftsmanship.Steel)
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        Assert.Equal(HttpStatusCode.NoContent, (await SetConfigAsync(client, id, Weapon(
            SignatureWeaponProfile.OneHanded, craftsmanship, "Фамильный меч", WeaponFormTraits.Sword,
            AnyWeaponAttachment(reference)))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await client.PostAsync($"/api/characters/{id}/complete-creation", null)).StatusCode);
        await client.PostAsJsonAsync($"/api/characters/{id}/xp-awards", new AwardXpRequest(xp, null), Json.Options);
        return (client, id, reference);
    }

    private static Task<HttpResponseMessage> BuyPowerAsync(HttpClient client, Guid id, int rank) =>
        client.PutAsJsonAsync($"/api/characters/{id}/heroic-upgrades",
            new SetHeroicUpgradesRequest(rank, 0, 0, false, []), Json.Options);

    private static Task<HttpResponseMessage> SetWeaponUpgradesAsync(
        HttpClient client, Guid id, SignatureWeaponImprovement? improvement = null, Guid? supreme = null) =>
        client.PostAsJsonAsync($"/api/characters/{id}/heroic-configuration/signature-weapon/upgrades",
            new SetSignatureWeaponUpgradesRequest(improvement, supreme), Json.Options);

    [Fact]
    public async Task Improved_AncientCraftsmanship_ReplacesTheOldOne_AndCostsAHardPoint()
    {
        // Гномья работа при создании: +1 урона. Древняя за Improved заменяет её целиком,
        // а не прибавляется — урон считается от чисел профиля, а не от гномьих.
        var (client, id, _) = await WeaponReadyForUpgradesAsync(50, WeaponCraftsmanship.Dwarven);
        Assert.Equal(HttpStatusCode.NoContent, (await BuyPowerAsync(client, id, 1)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Ancient)).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal(SignatureWeaponImprovement.Ancient, weapon.Improvement);
        Assert.Equal("Brawn + 4", weapon.Damage);
        Assert.Equal(2, weapon.Crit);
        Assert.Equal(1, weapon.HardPoints);
        Assert.Contains(weapon.Qualities, q => q.Code == "reinforced");
        // Выбор при создании остаётся в записи: заменяются числа, а не история выбора.
        Assert.Equal(WeaponCraftsmanship.Dwarven, weapon.Craftsmanship);
    }

    [Fact]
    public async Task Improved_Reinforced_AddsTheQuality_WithoutTouchingNumbers()
    {
        var (client, id, _) = await WeaponReadyForUpgradesAsync(50);
        await BuyPowerAsync(client, id, 1);

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Reinforced)).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Contains(weapon.Qualities, q => q.Code == "reinforced");
        Assert.Equal("Brawn + 3", weapon.Damage);
        Assert.Equal(3, weapon.Crit);
        Assert.Equal(2, weapon.HardPoints);
    }

    [Fact]
    public async Task Improved_ChoiceIsMadeOnce_AndBlocksFurtherPurchasesUntilMade()
    {
        var (client, id, _) = await WeaponReadyForUpgradesAsync(150);
        await BuyPowerAsync(client, id, 1);

        // Пока выбор не сделан, покупать дальше нельзя.
        var blocked = await BuyPowerAsync(client, id, 2);
        Assert.Equal(HttpStatusCode.BadRequest, blocked.StatusCode);
        Assert.Equal("heroic.weapon.upgrade_incomplete",
            (await blocked.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);

        await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Reinforced);

        // Сделанный выбор не переигрывается.
        var again = await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Ancient);
        Assert.Equal(HttpStatusCode.BadRequest, again.StatusCode);
        Assert.Equal("heroic.weapon.improvement_immutable",
            (await again.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task Improved_CannotBeChosenBeforeItIsBought()
    {
        var (client, id, _) = await WeaponReadyForUpgradesAsync(50);

        var resp = await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Ancient);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.improvement_not_bought",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task Supreme_AddsTwoHardPoints_AndInstallsOneFreeAttachment()
    {
        var (client, id, reference) = await WeaponReadyForUpgradesAsync(150);
        await BuyPowerAsync(client, id, 1);
        await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Reinforced);
        Assert.Equal(HttpStatusCode.NoContent, (await BuyPowerAsync(client, id, 2)).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent,
            (await SetWeaponUpgradesAsync(client, id, supreme: Attachment(reference, "razor-edge"))).StatusCode);

        var weapon = (await SheetAsync(client, id)).HeroicConfiguration!.SignatureWeapon!;
        Assert.EndsWith(".razor-edge", weapon.SupremeAttachment!.Code, StringComparison.Ordinal);
        // Два слота профиля плюс два за Supreme, минус один занятый бесплатным улучшением.
        Assert.Equal(3, weapon.HardPoints);
        // Установленное улучшение считается по-настоящему: Проникающее 2 и минус единица крита.
        Assert.Equal(2, weapon.Qualities.Single(q => q.Code == "pierce").Rating);
        Assert.Equal(2, weapon.Crit);
    }

    [Fact]
    public async Task Supreme_RejectsTooRareAttachment()
    {
        var (client, id, reference) = await WeaponReadyForUpgradesAsync(150);
        await BuyPowerAsync(client, id, 1);
        await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Reinforced);
        await BuyPowerAsync(client, id, 2);

        // Руна клинков — редкость 10, выше предела бесплатного улучшения.
        var resp = await SetWeaponUpgradesAsync(client, id, supreme: Attachment(reference, "rune-of-blades"));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Equal("heroic.weapon.attachment_too_rare",
            (await resp.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
    }

    [Fact]
    public async Task WeaponUpgrades_SurviveExportImport()
    {
        var (client, id, reference) = await WeaponReadyForUpgradesAsync(150);
        await BuyPowerAsync(client, id, 1);
        await SetWeaponUpgradesAsync(client, id, SignatureWeaponImprovement.Ancient);
        await BuyPowerAsync(client, id, 2);
        await SetWeaponUpgradesAsync(client, id, supreme: Attachment(reference, "serrated-edge"));

        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        Assert.Equal(SignatureWeaponImprovement.Ancient, export.Character.SignatureWeaponImprovement);
        Assert.EndsWith(".serrated-edge",
            export.Character.SignatureWeaponSupremeAttachmentCode!, StringComparison.Ordinal);

        var importResp = await client.PostAsJsonAsync("/api/characters/import", export, Json.Options);
        var imported = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        var weapon = (await SheetAsync(client, imported.CharacterId)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal(SignatureWeaponImprovement.Ancient, weapon.Improvement);
        Assert.EndsWith(".serrated-edge", weapon.SupremeAttachment!.Code, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ImportedWeapon_WithCraftsmanshipOutsideTheAbility_IsRepairedWithAWarning()
    {
        var (client, id, reference) = await CreateWithAbilityAsync("rot.heroic.signature-weapon");
        await SetConfigAsync(client, id, Weapon(SignatureWeaponProfile.OneHanded,
            WeaponCraftsmanship.Dwarven, "Фамильный меч", WeaponFormTraits.Sword,
            AnyWeaponAttachment(reference)));
        await client.PostAsync($"/api/characters/{id}/complete-creation", null);

        // Файл персонажа, созданного до правила: железная работа именного оружия.
        var export = (await client.GetFromJsonAsync<CharacterExportDto>(
            $"/api/characters/{id}/export", Json.Options))!;
        var legacy = export with
        {
            Character = export.Character with { SignatureWeaponCraftsmanship = WeaponCraftsmanship.Iron },
        };

        var importResp = await client.PostAsJsonAsync("/api/characters/import", legacy, Json.Options);
        var result = (await importResp.Content.ReadFromJsonAsync<ImportCharacterResult>(Json.Options))!;
        Assert.Contains(result.Warnings, w => w.Contains("Качество изготовления именного оружия"));

        var weapon = (await SheetAsync(client, result.CharacterId)).HeroicConfiguration!.SignatureWeapon!;
        Assert.Equal(WeaponCraftsmanship.Steel, weapon.Craftsmanship);
        Assert.False(weapon.CraftsmanshipOutOfRules);
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
