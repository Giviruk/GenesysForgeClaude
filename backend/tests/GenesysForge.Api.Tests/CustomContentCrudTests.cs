using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class CustomContentCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public CustomContentCrudTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Skill_Update_ChangesNameAndCharacteristic()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Sailing", CharacteristicType.Agility, SkillKind.General),
            Json.Options)).Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;

        var update = await client.PutAsJsonAsync($"/api/custom/skills/{created.Id}",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Navigation", CharacteristicType.Intellect, SkillKind.Knowledge),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.Contains(reference.Skills, s => s.Name == "Navigation" && s.Characteristic == CharacteristicType.Intellect);
        Assert.DoesNotContain(reference.Skills, s => s.Name == "Sailing");
    }

    [Fact]
    public async Task Skill_Delete_RemovesIt()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var created = (await (await client.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Cooking", CharacteristicType.Cunning, SkillKind.General),
            Json.Options)).Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;

        var del = await client.DeleteAsync($"/api/custom/skills/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.DoesNotContain(reference.Skills, s => s.Name == "Cooking");
    }

    [Fact]
    public async Task Skill_Delete_BlockedWhenUsedByCharacter()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var skill = (await (await client.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Falconry", CharacteristicType.Cunning, SkillKind.General),
            Json.Options)).Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Hunter", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var charId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PostAsync($"/api/characters/{charId}/skills/{skill.Id}/buy-rank", null);

        var del = await client.DeleteAsync($"/api/custom/skills/{skill.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, del.StatusCode);
        var error = await del.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options);
        Assert.Contains("используется", error!.Message);
    }

    [Fact]
    public async Task Talent_Update_And_Delete()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var talent = (await (await client.PostAsJsonAsync("/api/custom/talents",
            new CreateCustomTalentRequest(GameSystem.GenesysCore, "Lucky", 1, true, "Пассивный", "desc", 0, 1, 0, 0, 0,
                TalentCategory.General),
            Json.Options)).Content.ReadFromJsonAsync<TalentDefDto>(Json.Options))!;
        Assert.Equal(TalentCategory.General, talent.Category);

        var update = await client.PutAsJsonAsync($"/api/custom/talents/{talent.Id}",
            new CreateCustomTalentRequest(GameSystem.GenesysCore, "Very Lucky", 2, true, "Инцидент", "better", 0, 2, 0, 0, 0,
                TalentCategory.Social),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<TalentDefDto>(Json.Options))!;
        Assert.Equal("Very Lucky", updated.Name);
        Assert.Equal(2, updated.Tier);
        Assert.Equal(TalentCategory.Social, updated.Category);
        Assert.Equal(2, updated.StrainBonus);

        var del = await client.DeleteAsync($"/api/custom/talents/{talent.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task Item_Delete_BlockedWhenInInventory()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var item = (await (await client.PostAsJsonAsync("/api/custom/items",
            new CreateCustomItemRequest(GameSystem.GenesysCore, "Lucky Coin", ItemKind.Gear, 0, 0, 0, 0, 0, "", 1, 1),
            Json.Options)).Content.ReadFromJsonAsync<ItemDefDto>(Json.Options))!;

        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Owner", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null));
        var charId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        await client.PostAsJsonAsync($"/api/characters/{charId}/items", new AddItemRequest(item.Id, 1, ItemState.Carried), Json.Options);

        var del = await client.DeleteAsync($"/api/custom/items/{item.Id}");
        Assert.Equal(HttpStatusCode.BadRequest, del.StatusCode);
    }

    [Fact]
    public async Task Update_ForeignCustomContent_NotFound()
    {
        var owner = await _factory.CreateAuthorizedClientAsync();
        var skill = (await (await owner.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Secret", CharacteristicType.Brawn, SkillKind.General),
            Json.Options)).Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var update = await stranger.PutAsJsonAsync($"/api/custom/skills/{skill.Id}",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Hacked", CharacteristicType.Brawn, SkillKind.General),
            Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, update.StatusCode); // «не найден» для чужого
    }

    [Fact]
    public async Task HeroicAbility_Update_And_Delete()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var ability = (await (await client.PostAsJsonAsync("/api/custom/heroic-abilities",
            new CreateCustomHeroicAbilityRequest("Old Name", "old"),
            Json.Options)).Content.ReadFromJsonAsync<HeroicAbilityDto>(Json.Options))!;

        var update = await client.PutAsJsonAsync($"/api/custom/heroic-abilities/{ability.Id}",
            new CreateCustomHeroicAbilityRequest("New Name", "new desc"), Json.Options);
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = (await update.Content.ReadFromJsonAsync<HeroicAbilityDto>(Json.Options))!;
        Assert.Equal("New Name", updated.Name);

        var del = await client.DeleteAsync($"/api/custom/heroic-abilities/{ability.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }

    [Fact]
    public async Task ArchetypeAndCareer_Create_ArePrivate_AndCanCreateCharacter()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var careerSkills = reference.Skills.Take(4).Select(s => s.Name).ToList();

        var archetypeResponse = await client.PostAsJsonAsync("/api/custom/archetypes",
            new CreateCustomArchetypeRequest(GameSystem.GenesysCore, "Clockwork Folk", "Заводной народ",
                2, 3, 2, 2, 2, 1, 11, 9, 95, "Искусственный народ.", "Заводной механизм", "Не нуждается во сне."),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, archetypeResponse.StatusCode);
        var archetype = (await archetypeResponse.Content.ReadFromJsonAsync<ArchetypeDto>(Json.Options))!;
        Assert.True(archetype.IsCustom);
        Assert.Single(archetype.Abilities);

        var careerResponse = await client.PostAsJsonAsync("/api/custom/careers",
            new CreateCustomCareerRequest(GameSystem.GenesysCore, "Chronist", "Хронист",
                "Исследует прошлое.", careerSkills, 25, ""),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, careerResponse.StatusCode);
        var career = (await careerResponse.Content.ReadFromJsonAsync<CareerDto>(Json.Options))!;
        Assert.True(career.IsCustom);
        Assert.Equal(careerSkills, career.CareerSkillNames);

        var ownerRef = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.Contains(ownerRef.Archetypes, a => a.Id == archetype.Id && a.IsCustom);
        Assert.Contains(ownerRef.Careers, c => c.Id == career.Id && c.IsCustom);

        var stranger = await _factory.CreateAuthorizedClientAsync();
        var strangerRef = (await stranger.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.DoesNotContain(strangerRef.Archetypes, a => a.Id == archetype.Id);
        Assert.DoesNotContain(strangerRef.Careers, c => c.Id == career.Id);

        var created = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Homebrew Hero", GameSystem.GenesysCore, archetype.Id, career.Id,
                [.. careerSkills.Take(2)]), Json.Options);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var characterId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Equal("Clockwork Folk", sheet.Archetype.Name);
        Assert.Equal(3, sheet.Characteristics["agility"]);
        Assert.Equal(95, sheet.TotalXp);
        Assert.Equal(25, sheet.Money);
        foreach (var skill in careerSkills)
            Assert.Contains(sheet.Skills, s => s.Name == skill && s.IsCareer);

        var foreignCreate = await stranger.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Hacker", GameSystem.GenesysCore, archetype.Id, career.Id, null), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, foreignCreate.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/custom/archetypes/{archetype.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.DeleteAsync($"/api/custom/careers/{career.Id}")).StatusCode);
    }

    [Fact]
    public async Task ArchetypeAndCareer_Update_And_Delete_WhenUnused()
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var skillNames = reference.Skills.Take(2).Select(s => s.Name).ToList();

        var archetype = (await (await client.PostAsJsonAsync("/api/custom/archetypes",
            new CreateCustomArchetypeRequest(GameSystem.GenesysCore, "Old Species", null,
                2, 2, 2, 2, 2, 2, 10, 10, 100, "", "", ""),
            Json.Options)).Content.ReadFromJsonAsync<ArchetypeDto>(Json.Options))!;
        var career = (await (await client.PostAsJsonAsync("/api/custom/careers",
            new CreateCustomCareerRequest(GameSystem.GenesysCore, "Old Career", null, "", skillNames, 0, ""),
            Json.Options)).Content.ReadFromJsonAsync<CareerDto>(Json.Options))!;

        var updatedArchetype = await client.PutAsJsonAsync($"/api/custom/archetypes/{archetype.Id}",
            new CreateCustomArchetypeRequest(GameSystem.GenesysCore, "New Species", "Новый вид",
                1, 2, 3, 2, 2, 2, 9, 11, 110, "new", "Черта", "описание"),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, updatedArchetype.StatusCode);
        Assert.Equal("New Species", (await updatedArchetype.Content.ReadFromJsonAsync<ArchetypeDto>(Json.Options))!.Name);

        var updatedCareer = await client.PutAsJsonAsync($"/api/custom/careers/{career.Id}",
            new CreateCustomCareerRequest(GameSystem.GenesysCore, "New Career", "Новая карьера", "new", [skillNames[0]], 10, "1d10"),
            Json.Options);
        Assert.Equal(HttpStatusCode.OK, updatedCareer.StatusCode);
        Assert.Equal("New Career", (await updatedCareer.Content.ReadFromJsonAsync<CareerDto>(Json.Options))!.Name);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/custom/archetypes/{archetype.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/custom/careers/{career.Id}")).StatusCode);
    }
}
