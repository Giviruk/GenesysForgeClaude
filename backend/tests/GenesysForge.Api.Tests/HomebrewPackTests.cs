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
    public async Task CampaignConnectsOriginalPlayerPack_WithoutCopiesOrExtraXp_AndKeepsOwnedRanks()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var player = await _factory.CreateAuthorizedClientAsync();
        var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Content isolation", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var skillRequest = new CreateCustomSkillRequest(GameSystem.GenesysCore, "Personal Sky Sailing", CharacteristicType.Agility, SkillKind.General);
        var skill = (await (await player.PostAsJsonAsync("/api/custom/skills", skillRequest, Json.Options))
            .Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;
        var talent = (await (await player.PostAsJsonAsync("/api/custom/talents",
            new CreateCustomTalentRequest(GameSystem.GenesysCore, "Personal Ranked Talent", 1, true, "Passive", "Own text", 0, 0, 0, 0, 0), Json.Options))
            .Content.ReadFromJsonAsync<TalentDefDto>(Json.Options))!;
        var personalPack = Assert.Single((await player.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        var reference = (await player.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var characterId = (await (await player.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Pilot", GameSystem.GenesysCore, reference.Archetypes[0].Id, reference.Careers[0].Id, null), Json.Options))
            .Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsync($"/api/characters/{characterId}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsJsonAsync($"/api/characters/{characterId}/talents/buy", new BuyTalentRequest(talent.Id), Json.Options)).StatusCode);
        var grit = reference.Talents.First(t => !t.IsCustom && t.Name == "Grit");
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsJsonAsync($"/api/characters/{characterId}/talents/buy", new BuyTalentRequest(grit.Id), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options)).StatusCode);
        var contextPath = $"/api/reference/GenesysCore?characterId={characterId}";
        Assert.DoesNotContain((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == skill.Id);
        var before = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Contains(before.Skills, s => s.SkillDefId == skill.Id && s.Ranks == 1);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync($"/api/characters/{characterId}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsJsonAsync($"/api/characters/{characterId}/talents/buy", new BuyTalentRequest(talent.Id), Json.Options)).StatusCode);
        var ignoredToggle = await player.PutAsJsonAsync($"/api/characters/{characterId}/homebrew-packs/{personalPack.Id}", new HomebrewPackToggleRequest(true), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, ignoredToggle.StatusCode);
        Assert.Equal("homebrew.character_campaign_context", (await ignoredToggle.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{personalPack.Id}",
            new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        var share = (await (await player.PostAsync($"/api/homebrew-packs/{personalPack.Id}/share", null))
            .Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        var connectedResponse = await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{share.Token}/import", null);
        Assert.Equal(HttpStatusCode.OK, connectedResponse.StatusCode);
        var connected = (await connectedResponse.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(personalPack.Id, connected.Id);
        Assert.Empty((await gm.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        Assert.Equal(before.SpentXp, (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!.SpentXp);
        var enabled = (await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!;
        Assert.Equal(skill.Id, Assert.Single(enabled.Skills, s => s.Name == skill.Name).Id);
        Assert.Equal(talent.Id, Assert.Single(enabled.Talents, t => t.Name == talent.Name).Id);
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsync($"/api/characters/{characterId}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await player.PostAsJsonAsync($"/api/characters/{characterId}/talents/buy", new BuyTalentRequest(talent.Id), Json.Options)).StatusCode);
        var developed = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Equal(2, Assert.Single(developed.Skills, s => s.Name == skill.Name).Ranks);
        Assert.Equal(2, Assert.Single(developed.Talents!, t => t.TalentDefId == talent.Id).Ranks);
        Assert.Equal(before.SpentXp + 15 + 10, developed.SpentXp);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PutAsJsonAsync($"/api/custom/skills/{skill.Id}", skillRequest, Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.GetAsync($"/api/homebrew-packs/{personalPack.Id}/export")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await player.PutAsJsonAsync($"/api/custom/skills/{skill.Id}", skillRequest with { Name = "Updated Original Sailing" }, Json.Options)).StatusCode);
        Assert.Contains((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == skill.Id && s.Name == "Updated Original Sailing");
        var gmReference = (await gm.GetFromJsonAsync<ReferenceResponse>($"/api/reference/GenesysCore?campaignId={campaign.Id}", Json.Options))!;
        Assert.DoesNotContain(skill.Id, gmReference.EditableCustomIds!);
        Assert.Contains(skill.Id, enabled.EditableCustomIds!);
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{personalPack.Id}", new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
        Assert.DoesNotContain((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == skill.Id);
        var disabled = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{characterId}", Json.Options))!;
        Assert.Equal(2, Assert.Single(disabled.Skills, s => s.SkillDefId == skill.Id).Ranks);
        Assert.Equal(Assert.Single(developed.Skills, s => s.SkillDefId == skill.Id).Pool, Assert.Single(disabled.Skills, s => s.SkillDefId == skill.Id).Pool);
        Assert.Equal(developed.SpentXp, disabled.SpentXp);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsJsonAsync($"/api/characters/{characterId}/talents/buy", new BuyTalentRequest(talent.Id), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{personalPack.Id}", new HomebrewPackToggleRequest(true), Json.Options)).StatusCode);
        Assert.Contains((await player.GetFromJsonAsync<ReferenceResponse>(contextPath, Json.Options))!.Skills, s => s.Id == skill.Id);
    }

    [Fact]
    public async Task OriginalPackConnection_RequiresGmAndCurrentShareToken_AndIsIdempotent()
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var player = await _factory.CreateAuthorizedClientAsync();
        var outsider = await _factory.CreateAuthorizedClientAsync();
        var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("Authorization", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        await player.PostAsJsonAsync("/api/custom/skills", new CreateCustomSkillRequest(GameSystem.GenesysCore, "Shared auth skill", CharacteristicType.Cunning, SkillKind.General), Json.Options);
        var pack = Assert.Single((await player.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        var oldShare = (await (await player.PostAsync($"/api/homebrew-packs/{pack.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        var current = (await (await player.PostAsync($"/api/homebrew-packs/{pack.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{oldShare.Token}/import", null)).StatusCode);
        var path = $"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{current.Token}/import";
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await outsider.PostAsync(path, null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/invalid/import", null)).StatusCode);
        Assert.Empty((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!);
        var nonMember = await gm.PostAsync(path, null);
        Assert.Equal(HttpStatusCode.BadRequest, nonMember.StatusCode);
        Assert.Equal("homebrew.owner_not_member", (await nonMember.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.ReasonCode);
        Assert.Equal(HttpStatusCode.OK, (await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, null), Json.Options)).StatusCode);
        for (var i = 0; i < 2; i++)
            Assert.Equal(HttpStatusCode.OK, (await gm.PostAsync(path, null)).StatusCode);
        var connected = Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!);
        Assert.Equal(pack.Id, connected.Id);
        Assert.False(connected.IsMine);
        Assert.True(connected.IsEnabled);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.GetAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PlayerPackAfterLeavingOrRemoval_RemainsEnabledWithFormerOwnerMarker(bool kicked)
    {
        var gm = await _factory.CreateAuthorizedClientAsync();
        var player = await _factory.CreateAuthorizedClientAsync();
        var campaign = (await (await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("Former author", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var joined = (await (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, null), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        var author = Assert.Single(joined.Players!);
        await player.PostAsJsonAsync("/api/custom/skills", new CreateCustomSkillRequest(GameSystem.GenesysCore, "Former owner skill", CharacteristicType.Cunning, SkillKind.General), Json.Options);
        var pack = Assert.Single((await player.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs/", Json.Options))!);
        var share = (await (await player.PostAsync($"/api/homebrew-packs/{pack.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        Assert.Equal(HttpStatusCode.OK, (await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{share.Token}/import", null)).StatusCode);
        var listPath = $"/api/campaigns/{campaign.Id}/homebrew-packs/";
        var initial = Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>(listPath, Json.Options))!);
        Assert.True(initial.OwnerIsMember);
        Assert.Equal(author.DisplayName, initial.OwnerName);
        var removal = await (kicked ? gm : player).DeleteAsync($"/api/campaigns/{campaign.Id}/members/{author.UserId}");
        Assert.True(removal.StatusCode == HttpStatusCode.NoContent,
            $"{removal.RequestMessage?.RequestUri}: {removal.StatusCode} {await removal.Content.ReadAsStringAsync()}");
        var former = Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>(listPath, Json.Options))!);
        Assert.True(former.IsEnabled);
        Assert.False(former.OwnerIsMember);
        Assert.Equal(initial.OwnerName, former.OwnerName);
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
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
