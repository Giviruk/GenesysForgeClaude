using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

/// <summary>
/// GEN-EQP-QUAL-01 на листе и в атаке: качества оружия меняют пул проверки и поглощение цели,
/// а не остаются надписью на карточке.
/// </summary>
public class RotQualityEffectApiTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<CharacterSheetDto> SheetAsync(HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;

    private async Task<(HttpClient Client, Guid Id, ReferenceResponse Reference)> CreateCharacterAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        var human = reference.Archetypes.First(a => !a.IsCustom && a.Name == "Human");
        var career = reference.Careers.First(c => !c.IsCustom);
        var nonCareer = reference.Skills.Where(s => !career.CareerSkillNames.Contains(s.Name))
            .Take(2).Select(s => s.Name).ToList();
        var resp = await client.PostAsJsonAsync("/api/characters/", new CreateCharacterRequest(
            "Оружейник", GameSystem.RealmsOfTerrinoth, human.Id, career.Id, null,
            [new ArchetypeSkillChoice("any-noncareer", nonCareer)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var id = (await resp.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        return (client, id, reference);
    }

    private static async Task AddAsync(HttpClient client, Guid characterId, Guid itemDefId) =>
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync(
            $"/api/characters/{characterId}/items",
            new AddItemRequest(itemDefId, 1, ItemState.Equipped, Free: true), Json.Options)).StatusCode);

    private static WeaponAttackProfileDto DefaultProfile(CharacterSheetDto sheet, string name) =>
        sheet.Items!.Single(i => i.Name == name).AttackProfiles!.Single(p => p.IsDefault);

    [Fact]
    public async Task AccurateGivesBoost_AndInaccurateGivesSetback()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Кинжал — Точное 1, большой щит — Неточное 2.
        await AddAsync(client, id, reference.Items.Single(i => i.Name == "Dagger" && i.Kind == ItemKind.Weapon).Id);
        await AddAsync(client, id,
            reference.Items.Single(i => i.Name == "Shield, Large" && i.Kind == ItemKind.Weapon).Id);

        var sheet = await SheetAsync(client, id);

        Assert.Equal(1, DefaultProfile(sheet, "Dagger").PoolModifiers!.Boost);
        Assert.Equal(2, DefaultProfile(sheet, "Shield, Large").PoolModifiers!.Setback);
    }

    [Fact]
    public async Task Cumbersome_RaisesDifficultyByTheMissingBrawn()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Башенный щит — Громоздкое 4; у человека Мощь 2, значит две ступени сложности.
        await AddAsync(client, id,
            reference.Items.Single(i => i.Name == "Shield, Bulwark" && i.Kind == ItemKind.Weapon).Id);

        var sheet = await SheetAsync(client, id);
        var brawn = sheet.Characteristics["brawn"];
        var mods = DefaultProfile(sheet, "Shield, Bulwark").PoolModifiers!;

        Assert.Equal(4 - brawn, mods.DifficultyIncrease);
        Assert.Contains(mods.Sources, s => s.NameEn == "Cumbersome" && s.Difficulty == 4 - brawn);
    }

    [Fact]
    public async Task Unwieldy_LooksAtAgility_AndDisappearsWhenItIsEnough()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Длинный лук — Сноровка 3; у человека Ловкость 2, значит одна ступень.
        await AddAsync(client, id, reference.Items.Single(i => i.Name == "Longbow" && i.Kind == ItemKind.Weapon).Id);

        var sheet = await SheetAsync(client, id);
        var agility = sheet.Characteristics["agility"];
        var mods = DefaultProfile(sheet, "Longbow").PoolModifiers!;

        Assert.Equal(3 - agility, mods.DifficultyIncrease);
        Assert.Contains(mods.Sources, s => s.NameEn == "Unwieldy");
    }

    [Fact]
    public async Task DescriptiveQualities_LeaveThePoolAlone()
    {
        var (client, id, reference) = await CreateCharacterAsync();
        // Булава без качеств вовсе — пул не трогает ничто.
        await AddAsync(client, id, reference.Items.Single(i => i.Name == "Mace" && i.Kind == ItemKind.Weapon).Id);

        var mods = DefaultProfile(await SheetAsync(client, id), "Mace").PoolModifiers!;

        Assert.Equal(0, mods.Boost + mods.Setback + mods.DifficultyIncrease);
        Assert.Empty(mods.Sources);
    }

    [Fact]
    public async Task PierceIgnoresSoak_AndReinforcedArmorStopsIt()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var pierced = await ResolveAsync(client, soak: 4, [new AttackQualityRequest("pierce", 2)]);
        var reinforced = await ResolveAsync(client, soak: 4, [new AttackQualityRequest("pierce", 2)],
            targetReinforced: true);

        // Урон 5 против поглощения 4: Проникающее 2 снимает два, укреплённая броня — ничего.
        Assert.Equal(2, pierced.Hits[0].TargetSoak);
        Assert.Equal(2, pierced.Hits[0].IgnoredSoak);
        Assert.Equal(3, pierced.TotalApplied);

        Assert.Equal(4, reinforced.Hits[0].TargetSoak);
        Assert.Equal(0, reinforced.Hits[0].IgnoredSoak);
        Assert.Equal(1, reinforced.TotalApplied);
    }

    [Fact]
    public async Task ViciousAddsToTheCriticalRoll_ButNotToDamage()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, soak: 0, [new AttackQualityRequest("vicious", 2)]);

        Assert.Equal(20, result.CriticalRollBonus);
        Assert.Equal(5, result.TotalApplied);
    }

    [Fact]
    public async Task UnknownQualityCode_IsIgnored_NotTreatedAsARule()
    {
        var client = await factory.CreateAuthorizedClientAsync();

        var result = await ResolveAsync(client, soak: 3, [new AttackQualityRequest("totally-made-up", 9)]);

        Assert.Equal(3, result.Hits[0].TargetSoak);
        Assert.Equal(0, result.CriticalRollBonus);
    }

    /// <summary>Атака с 1 успехом и базовым уроном 4: сырой урон 5.</summary>
    private static async Task<ResolveAttackResponse> ResolveAsync(
        HttpClient client, int soak, List<AttackQualityRequest> qualities, bool targetReinforced = false)
    {
        var resp = await client.PostAsJsonAsync("/api/combat/resolve-attack", new ResolveAttackRequest(
            NetSuccesses: 1, BaseDamage: 4, TargetSoak: soak, Qualities: qualities,
            TargetReinforced: targetReinforced), Json.Options);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<ResolveAttackResponse>(Json.Options))!;
    }
}
