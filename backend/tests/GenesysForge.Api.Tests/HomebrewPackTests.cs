using System.Net;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;

namespace GenesysForge.Api.Tests;

public class HomebrewPackTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public HomebrewPackTests(ApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Import_Toggle_Export_And_SharedImport()
    {
        var owner = await _factory.CreateAuthorizedClientAsync();
        var document = new HomebrewPackExportDto(
            "genesysforge.homebrew-pack.v1",
            "Airships",
            "User-authored airship options.",
            GameSystem.GenesysCore,
            [new HomebrewSkillDto("airships.skill.sky-sailing", "Sky Sailing", "Небесное мореходство",
                CharacteristicType.Agility, SkillKind.General, "Operate airships.", "Operate airships.", "User")],
            [new HomebrewTalentDto("airships.talent.sky-captain", "Sky Captain", "Небесный капитан", 2, false,
                "Пассивный", "Lead an airship crew.", "Lead an airship crew.", "User", 0, 1, 0, 0, 0, TalentCategory.Social)],
            null, null, null, null);

        var importedResponse = await owner.PostAsJsonAsync("/api/homebrew-packs/import", document, Json.Options);
        Assert.Equal(HttpStatusCode.Created, importedResponse.StatusCode);
        var imported = (await importedResponse.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(2, imported.EntryCount);

        var reference = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var skill = Assert.Single(reference.Skills, s => s.Name == "Sky Sailing");
        var talent = Assert.Single(reference.Talents, t => t.Name == "Sky Captain");
        Assert.Equal(TalentCategory.Social, talent.Category);

        var exported = (await owner.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{imported.Id}/export", Json.Options))!;
        Assert.Equal("genesysforge.homebrew-pack.v1", exported.Format);
        Assert.Equal("airships.skill.sky-sailing", exported.Skills![0].Code);
        Assert.Equal(TalentCategory.Social, exported.Talents![0].Category);

        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.PutAsJsonAsync($"/api/homebrew-packs/{imported.Id}/default",
                new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
        var hidden = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.DoesNotContain(hidden.Skills, s => s.Id == skill.Id);
        Assert.DoesNotContain(hidden.Talents, t => t.Id == talent.Id);

        var builtIn = hidden;
        var created = await owner.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Pilot", GameSystem.GenesysCore, builtIn.Archetypes[0].Id, builtIn.Careers[0].Id, null),
            Json.Options);
        var characterId = (await created.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        var hiddenForCharacter = (await owner.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/GenesysCore?characterId={characterId}", Json.Options))!;
        Assert.DoesNotContain(hiddenForCharacter.Skills, s => s.Id == skill.Id);

        Assert.Equal(HttpStatusCode.NoContent,
            (await owner.PutAsJsonAsync($"/api/characters/{characterId}/homebrew-packs/{imported.Id}",
                new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        var visibleForCharacter = (await owner.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/GenesysCore?characterId={characterId}", Json.Options))!;
        Assert.Contains(visibleForCharacter.Skills, s => s.Id == skill.Id);

        var share = (await (await owner.PostAsync($"/api/homebrew-packs/{imported.Id}/share", null))
            .Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        var stranger = await _factory.CreateAuthorizedClientAsync();
        var importedSharedResponse = await stranger.PostAsync($"/api/homebrew-packs/shared/{share.Token}/import", null);
        Assert.Equal(HttpStatusCode.Created, importedSharedResponse.StatusCode);
        var strangerRef = (await stranger.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        Assert.Contains(strangerRef.Skills, s => s.Name == "Sky Sailing");
    }
    [Fact]
    public async Task CampaignRejectsPersonalPacks_UntilGmImportsAndEnablesCopy_AndKeepsOwnedRanks()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var player = await _factory.CreateAuthorizedClientAsync();
        var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Content isolation", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var skill = (await (await player.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Personal Sky Sailing", CharacteristicType.Agility, SkillKind.General), Json.Options))
            .Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;
        var personalPack = Assert.Single((await player.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        var reference = (await player.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var characterId = (await (await player.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Pilot", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null), Json.Options))
            .Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsync($"/api/characters/{characterId}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options)).StatusCode);

        var contextPath = $"/api/reference/GenesysCore?characterId={characterId}";
        var campaignRef = (await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!;
        Assert.DoesNotContain(campaignRef.Skills, s => s.Id == skill.Id);
        Assert.DoesNotContain((await player.GetFromJsonAsync<ReferenceResponse>(
            $"/api/reference/GenesysCore?campaignId={campaign.Id}", Json.Options))!.Skills, s => s.Id == skill.Id);
        var retained = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Contains(retained.Skills, s => s.SkillDefId == skill.Id && s.Ranks == 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync($"/api/characters/{characterId}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(retained.SpentXp, (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!.SpentXp);
        Assert.Equal(HttpStatusCode.NoContent, (await player.PutAsJsonAsync($"/api/characters/{characterId}/homebrew-packs/{personalPack.Id}",
            new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        Assert.DoesNotContain((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == skill.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{personalPack.Id}",
            new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);

        var share = (await (await player.PostAsync($"/api/homebrew-packs/{personalPack.Id}/share", null))
            .Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        var imported = (await (await gm.PostAsync($"/api/homebrew-packs/shared/{share.Token}/import", null))
            .Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{imported.Id}",
            new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        var enabled = (await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!;
        var sharedSkill = Assert.Single(enabled.Skills, s => s.Name == skill.Name);
        Assert.NotEqual(skill.Id, sharedSkill.Id);
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsync($"/api/characters/{characterId}/skills/{sharedSkill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{imported.Id}",
            new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
        Assert.DoesNotContain((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == sharedSkill.Id);
        var afterDisable = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Contains(afterDisable.Skills, s => s.SkillDefId == sharedSkill.Id && s.Ranks == 1);
        Assert.Contains(afterDisable.Skills, s => s.SkillDefId == skill.Id && s.Ranks == 1);
        Assert.Equal(retained.SpentXp + 10, afterDisable.SpentXp);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync($"/api/characters/{characterId}/skills/{sharedSkill.Id}/buy-rank", null)).StatusCode);
    }

    [Fact]
    public async Task CharacterUsesCampaignUnion_ExplicitContextUsesOneCampaign_StandaloneRestoresPersonalPack()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var player = await _factory.CreateAuthorizedClientAsync();
        var campaigns = new List<CampaignDetailDto>();
        var campaignSkills = new List<SkillDefDto>();
        for (var i = 0; i < 2; i++)
        {
            var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/",
                new CreateCampaignRequest($"Rules {i}", ""), Json.Options))
                .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
            campaigns.Add(campaign);
            campaignSkills.Add((await (await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/custom/skills",
                new CreateCustomSkillRequest(GameSystem.GenesysCore, $"Campaign skill {i}", CharacteristicType.Agility, SkillKind.General), Json.Options))
                .Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!);
        }
        var personalSkill = (await (await player.PostAsJsonAsync("/api/custom/skills",
            new CreateCustomSkillRequest(GameSystem.GenesysCore, "Private skill", CharacteristicType.Agility, SkillKind.General), Json.Options))
            .Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;
        var reference = (await player.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var characterId = (await (await player.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Traveller", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null), Json.Options))
            .Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        foreach (var campaign in campaigns)
            Assert.Equal(HttpStatusCode.OK, (await player.PostAsJsonAsync("/api/campaigns/join",
                new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options)).StatusCode);

        var contextPath = $"/api/reference/GenesysCore?characterId={characterId}";
        var union = (await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!;
        Assert.All(campaignSkills, skill => Assert.Contains(union.Skills, s => s.Id == skill.Id));
        Assert.DoesNotContain(union.Skills, s => s.Id == personalSkill.Id);
        var explicitCampaign = (await player.GetFromJsonAsync<ReferenceResponse>(
            $"{contextPath}&campaignId={campaigns[0].Id}", Json.Options))!;
        Assert.Contains(explicitCampaign.Skills, s => s.Id == campaignSkills[0].Id);
        Assert.DoesNotContain(explicitCampaign.Skills, s => s.Id == campaignSkills[1].Id);
        Assert.DoesNotContain(explicitCampaign.Skills, s => s.Id == personalSkill.Id);

        foreach (var campaign in campaigns)
            Assert.Equal(HttpStatusCode.NoContent, (await player.DeleteAsync($"/api/campaigns/{campaign.Id}/characters/{characterId}")).StatusCode);
        var standalone = (await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!;
        Assert.Contains(standalone.Skills, s => s.Id == personalSkill.Id);
        Assert.All(campaignSkills, skill => Assert.DoesNotContain(standalone.Skills, s => s.Id == skill.Id));
        foreach (var campaign in campaigns)
            Assert.Equal(HttpStatusCode.OK, (await player.GetAsync($"/api/campaigns/{campaign.Id}")).StatusCode);
    }

    [Fact]
    public async Task GmPersonalArchetype_RequiresCampaignConnection_EvenForGmCharacterCreation()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("GM rules", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var archetype = (await (await gm.PostAsJsonAsync("/api/custom/archetypes",
            new CreateCustomArchetypeRequest(GameSystem.GenesysCore, "Personal GM species", "Вид мастера", 2, 2, 2, 2, 2, 2,
                10, 10, 100, "Own text", null, null), Json.Options))
            .Content.ReadFromJsonAsync<ArchetypeDto>(Json.Options))!;
        var reference = (await gm.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var request = new CreateCharacterRequest("GM character", GameSystem.GenesysCore, archetype.Id,
            reference.Careers[0].Id, null, CampaignId: campaign.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PostAsJsonAsync("/api/characters/", request, Json.Options)).StatusCode);
        var pack = Assert.Single((await gm.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(true), Json.Options);
        Assert.Equal(HttpStatusCode.Created, (await gm.PostAsJsonAsync("/api/characters/", request, Json.Options)).StatusCode);
    }

}
