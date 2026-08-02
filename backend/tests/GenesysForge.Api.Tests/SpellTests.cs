using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class SpellTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public SpellTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Unauthorized_WithoutToken()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/spells/GenesysCore");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSystem_Returns400()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var response = await client.GetAsync("/api/spells/Nonsense");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GenesysCore_ReturnsEffectsAndModifiers_NoTerrinothOnlySkills()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        Assert.NotEmpty(spells);
        Assert.Contains(spells, s => s.Kind == SpellEntryKind.Effect);
        Assert.Contains(spells, s => s.Kind == SpellEntryKind.AdditionalEffect);
        // Genesys Core: только Arcana/Divine/Primal, без Runes/Verse
        Assert.DoesNotContain(spells, s => s.MagicSkill is "Runes" or "Verse");
        Assert.Contains(spells, s => s.MagicSkill == "Arcana");
    }

    [Fact]
    public async Task Terrinoth_AddsRunesAndVerse()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        Assert.Contains(spells, s => s.MagicSkill == "Runes" && s.Kind == SpellEntryKind.Effect);
        Assert.Contains(spells, s => s.MagicSkill == "Verse" && s.Kind == SpellEntryKind.Effect);
    }

    [Fact]
    public async Task BaseEffects_FollowSkillAvailabilityMatrix()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        bool Available(string skill, string effect) =>
            spells.Any(s => s.Kind == SpellEntryKind.Effect && s.MagicSkill == skill && s.NameEn == effect);

        // По матрице: Verse не имеет Attack; Arcana не имеет Augment и Heal; Primal не имеет Barrier и Curse.
        Assert.False(Available("Verse", "Attack"));
        Assert.False(Available("Arcana", "Augment"));
        Assert.False(Available("Arcana", "Heal"));
        Assert.False(Available("Primal", "Barrier"));
        Assert.False(Available("Runes", "Conjure"));
        Assert.False(Available("Primal", "Curse"));
        // Доступные комбинации присутствуют
        Assert.True(Available("Arcana", "Attack"));
        Assert.True(Available("Verse", "Heal"));
        Assert.True(Available("Runes", "Augment"));
        // Utility доступна всем навыкам; Curse — всем кроме Primal
        foreach (var skill in new[] { "Arcana", "Divine", "Primal", "Runes", "Verse" })
            Assert.True(Available(skill, "Utility"), $"{skill} should have Utility");
        foreach (var skill in new[] { "Arcana", "Divine", "Runes", "Verse" })
            Assert.True(Available(skill, "Curse"), $"{skill} should have Curse");
        // EPG-заклинания присутствуют в обеих системах
        Assert.True(Available("Arcana", "Mask"));
        Assert.True(Available("Arcana", "Predict"));
        Assert.True(Available("Primal", "Transform"));
    }

    [Fact]
    public async Task AdditionalEffects_BelongToAnExistingBaseEffect()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        var baseEffectCodes = spells.Where(s => s.Kind == SpellEntryKind.Effect)
            .Select(s => s.NameEn).ToHashSet();
        var additional = spells.Where(s => s.Kind == SpellEntryKind.AdditionalEffect).ToList();

        Assert.NotEmpty(additional);
        Assert.All(additional, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.ParentEffect));      // привязан к базовому
            Assert.Contains(m.ParentEffect, baseEffectCodes);            // который существует
        });
        // у Attack есть свои доп. эффекты
        Assert.Contains(additional, m => m.ParentEffect == "Attack");
    }

    // ── ROT-MAG-01: доступность приходит с сервера полем, а не собирается клиентом ──

    [Fact]
    public async Task EveryEntry_CarriesItsAllowedSkills()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        Assert.All(spells, s =>
        {
            Assert.NotNull(s.AllowedSkills);
            Assert.NotEmpty(s.AllowedSkills!);
            // У базовой записи собственное направление обязано входить в её же список доступности.
            if (s.Kind == SpellEntryKind.Effect) Assert.Contains(s.MagicSkill, s.AllowedSkills!);
        });
    }

    [Fact]
    public async Task AdditionalEffect_InheritsAvailabilityOfItsAction_AndItsOwnRestriction()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        SpellDto Effect(string parent, string code) => spells.Single(s =>
            s.Kind == SpellEntryKind.AdditionalEffect && s.ParentEffect == parent && s.NameEn == code);

        // «Рок» — только Магия, хотя само Проклятье доступно ещё трём направлениям.
        Assert.Equal(["Arcana"], Effect("Curse", "Doom").AllowedSkills);
        Assert.Equal("Arcana", Effect("Curse", "Doom").RestrictedSkill);
        // «Неудача» ограничений не имеет и наследует всю строку Проклятья.
        Assert.Equal(["Arcana", "Divine", "Runes", "Verse"], Effect("Curse", "Misfortune").AllowedSkills);
        Assert.Equal("", Effect("Curse", "Misfortune").RestrictedSkill);
    }

    [Fact]
    public async Task GenesysCore_NeverMentionsTerrinothSkillsInAvailability()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        // Строка матрицы сужается до навыков системы: в Core Рун и Песни нет вообще.
        Assert.All(spells, s => Assert.DoesNotContain(s.AllowedSkills!,
            skill => skill is "Runes" or "Verse"));
        Assert.Equal(["Arcana", "Divine", "Primal"],
            spells.Single(s => s.Kind == SpellEntryKind.Effect && s.NameEn == "Utility"
                && s.MagicSkill == "Arcana").AllowedSkills);
    }

    [Fact]
    public async Task EpgEntries_AreMarkedOptional_AndRotEntriesAreNot()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        foreach (var epg in new[] { "Mask", "Predict", "Transform" })
        {
            Assert.All(spells.Where(s => s.Kind == SpellEntryKind.Effect && s.NameEn == epg),
                s => Assert.True(s.IsOptional));
            // Пометка спускается и на дополнительные эффекты опционального действия.
            Assert.All(spells.Where(s => s.ParentEffect == epg), s => Assert.True(s.IsOptional));
        }

        Assert.All(spells.Where(s => s.ParentEffect == "Attack" || s.NameEn == "Attack"),
            s => Assert.False(s.IsOptional));
    }

    [Fact]
    public async Task RestrictionsAndConflicts_LiveInFields_NotInDescriptions()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        SpellDto Effect(string parent, string code) => spells.Single(s =>
            s.Kind == SpellEntryKind.AdditionalEffect && s.ParentEffect == parent && s.NameEn == code);

        Assert.Contains("Additional Target", Effect("Curse", "Despair").Exclusions!);
        Assert.Contains("Additional Target", Effect("Curse", "Paralyzed").Exclusions!);
        Assert.Contains("Despair", Effect("Curse", "Additional Target").Exclusions!);

        // Ни ограничение по навыку, ни несочетаемость больше не дублируются текстом описания.
        Assert.All(spells, s =>
        {
            Assert.DoesNotContain("Только Вера", s.Description);
            Assert.DoesNotContain("Только Магия", s.Description);
            Assert.DoesNotContain("Только Природа", s.Description);
            Assert.DoesNotContain("Нельзя сочетать", s.Description);
            Assert.DoesNotContain("только Divine", s.SafeDescription);
            Assert.DoesNotContain("Divine only", s.DescriptionEn);
        });
    }

    [Fact]
    public async Task DifficultyIncrease_MatchesThePrintedString()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        var attack = spells.First(s => s.Kind == SpellEntryKind.Effect && s.NameEn == "Attack");
        Assert.Equal("1 (Easy)", attack.Difficulty);
        Assert.Equal(1, attack.DifficultyIncrease);

        var empowered = spells.Single(s => s.ParentEffect == "Attack" && s.NameEn == "Empowered");
        Assert.Equal("+2", empowered.Difficulty);
        Assert.Equal(2, empowered.DifficultyIncrease);
    }

    // ── ROT-MAG-12: состав Призыва и запрет стака Усиления ──

    [Fact]
    public async Task Conjure_StatesDisposition_Duration_And_TheSummonLevels()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        // Призванное существо не союзник по умолчанию — без этого «Призыв союзника» за +1
        // выглядит покупкой ни за что.
        // Базовое действие лежит по строке на каждое доступное направление, поэтому First.
        var conjure = spells.First(s => s.Kind == SpellEntryKind.Effect && s.NameEn == "Conjure");
        Assert.Contains("не подчиняется", conjure.Description);
        Assert.Contains("враждебным", conjure.Description);
        Assert.Contains("до конца его следующего хода", conjure.Description);
        Assert.Contains("концентрации", conjure.Description);

        SpellDto Effect(string code) => spells.Single(s => s.ParentEffect == "Conjure" && s.NameEn == code);

        Assert.Contains("дружественно", Effect("Summon Ally").Description);
        Assert.Contains("силуэта не больше 1", Effect("Medium Summon").Description);
        Assert.Contains("силуэта не больше 3", Effect("Grand Summon").Description);
    }

    [Fact]
    public async Task AdditionalSummon_CostsTwoAdvantage_NotOne()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        var summon = spells.Single(s => s.ParentEffect == "Conjure" && s.NameEn == "Additional Summon");
        Assert.Contains("2 преимущества", summon.Description);
        Assert.DoesNotContain("1 преимущество", summon.Description);
        Assert.Contains("2 advantages", summon.DescriptionEn);
    }

    [Fact]
    public async Task Augment_SaysOnlyOneAugmentAffectsATarget()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        var augment = spells.First(s => s.Kind == SpellEntryKind.Effect && s.NameEn == "Augment");
        Assert.Contains("только одно усиление", augment.Description);
        Assert.Contains("отклоняется", augment.Description);
        Assert.Contains("только одно усиление", augment.SafeDescription);

        // Правило про две цели, а не про сочетание эффектов внутри одного заклинания:
        // структурная несочетаемость к Усилению не добавляется.
        Assert.Empty(augment.Exclusions!);
    }

    [Fact]
    public async Task MoveEffect_IsGone_ItsSurvivingTwinIsManipulative()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        Assert.DoesNotContain(spells, s => s.ParentEffect == "Attack" && s.NameEn == "Move");
        Assert.Contains(spells, s => s.ParentEffect == "Attack" && s.NameEn == "Manipulative");
    }

    // ── ROT-MAG-04: Быстрота и Ускорение не путаются местами ──

    [Fact]
    public async Task Haste_IsTheSecondManeuver_AndSwift_IsTerrain()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/RealmsOfTerrinoth", Json.Options))!;

        SpellDto Effect(string code) => spells.Single(s =>
            s.Kind == SpellEntryKind.AdditionalEffect && s.ParentEffect == "Augment" && s.NameEn == code);

        var haste = Effect("Haste");
        Assert.Equal("Ускорение", haste.NameRu);
        Assert.Equal(1, haste.DifficultyIncrease);
        Assert.Contains("второй манёвр", haste.Description);
        Assert.Contains("без получения усталости", haste.Description);
        // Общий предел двух манёвров эффект не снимает — это отдельное правило, а не следствие.
        Assert.Contains("лимит двух манёвров", haste.Description);
        Assert.Contains("maneuver", haste.DescriptionEn, StringComparison.OrdinalIgnoreCase);

        var swift = Effect("Swift");
        Assert.Equal("Быстрота", swift.NameRu);
        Assert.Equal(1, swift.DifficultyIncrease);
        Assert.Contains("пересечённой местности", swift.Description);
        Assert.Contains("обездвижены", swift.Description);
        Assert.Contains("terrain", swift.DescriptionEn, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Haste_AndSwift_DoNotBorrowEachOthersEffect()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        SpellDto Effect(string code) => spells.Single(s =>
            s.Kind == SpellEntryKind.AdditionalEffect && s.ParentEffect == "Augment" && s.NameEn == code);

        // Перекрёстного эффекта нет ни в одной локали: ошибка была именно в том, что тексты
        // стояли под чужими кодами.
        var haste = Effect("Haste");
        Assert.DoesNotContain("местност", haste.Description);
        Assert.DoesNotContain("обездвиж", haste.Description);
        Assert.DoesNotContain("terrain", haste.DescriptionEn, StringComparison.OrdinalIgnoreCase);

        var swift = Effect("Swift");
        Assert.DoesNotContain("манёвр", swift.Description);
        Assert.DoesNotContain("maneuver", swift.DescriptionEn, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Spells_HaveRussianNames_SafeDescription_AndSource()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var spells = (await client.GetFromJsonAsync<List<SpellDto>>("/api/spells/GenesysCore", Json.Options))!;

        Assert.All(spells, s =>
        {
            Assert.False(string.IsNullOrWhiteSpace(s.NameRu));   // русское название
            Assert.False(string.IsNullOrWhiteSpace(s.SafeDescription)); // безопасное описание для public
            Assert.False(string.IsNullOrWhiteSpace(s.Source));   // ссылка на источник доступна
        });
    }
}
