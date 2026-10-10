using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

public class CustomContentChangeTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<List<CustomContentChangeDto>> History(HttpClient client, Guid packId, Guid? campaignId = null) =>
        (await client.GetFromJsonAsync<List<CustomContentChangeDto>>(
            $"/api/homebrew-packs/{packId}/changes?take=200" + (campaignId is null ? "" : $"&campaignId={campaignId}"), Json.Options))!;

    private static async Task<CampaignDetailDto> Campaign(HttpClient gm) =>
        (await (await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("History", ""), Json.Options))
            .Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;

    [Theory]
    [InlineData("skills", "skill")]
    [InlineData("talents", "talent")]
    [InlineData("items", "item")]
    [InlineData("heroic-abilities", "heroicAbility")]
    [InlineData("archetypes", "archetype")]
    [InlineData("careers", "career")]
    public async Task AllCustomTypes_RecordCrud_NoOpAndDeletedName(string route, string type)
    {
        var owner = await factory.CreateAuthorizedClientAsync();
        var pack = await owner.CreateLibraryPackAsync(route == "heroic-abilities" ? GameSystem.RealmsOfTerrinoth : GameSystem.GenesysCore);
        var reference = (await owner.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        object Request(bool updated) => route switch
        {
            "skills" => new CreateCustomSkillRequest(GameSystem.GenesysCore, updated ? "New Sailing" : "Old Sailing",
                updated ? CharacteristicType.Intellect : CharacteristicType.Agility, SkillKind.General, [pack.Id]),
            "talents" => new CreateCustomTalentRequest(GameSystem.GenesysCore, updated ? "New Captain" : "Old Captain",
                updated ? 2 : 1, false, "Passive", "Own text", 0, 0, 0, 0, 0, PackIds: [pack.Id]),
            "items" => new CreateCustomItemRequest(GameSystem.GenesysCore, updated ? "New Compass" : "Old Compass",
                ItemKind.Gear, 1, 0, 0, 0, 0, "Own text", updated ? 20 : 10, 1, PackIds: [pack.Id]),
            "heroic-abilities" => new CreateCustomHeroicAbilityRequest(updated ? "New Courage" : "Old Courage", "Own text", [pack.Id]),
            "archetypes" => new CreateCustomArchetypeRequest(GameSystem.GenesysCore, updated ? "New Species" : "Old Species", null,
                2, 2, 2, 2, 2, 2, 10, 10, 100, "Own text", "Own ability", updated ? "New effect" : "Old effect", PackIds: [pack.Id]),
            _ => new CreateCustomCareerRequest(GameSystem.GenesysCore, updated ? "New Career" : "Old Career", null,
                "Own text", reference.Skills.Take(updated ? 1 : 2).Select(x => x.Name).ToList(), 0, "", PackIds: [pack.Id]),
        };
        var created = await owner.PostAsJsonAsync($"/api/custom/{route}", Request(false), Json.Options);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var definition = (await created.Content.ReadFromJsonAsync<JsonElement>(Json.Options)).GetProperty("id").GetGuid();
        var first = Assert.Single(await History(owner, pack.Id));
        Assert.Equal(CustomContentChangeAction.Created, first.Action);
        Assert.Equal(type, first.DefinitionType);
        Assert.Equal(definition, first.DefinitionId);
        Assert.NotEqual(Guid.Empty, first.UserId);
        Assert.NotEmpty(first.UserName);
        Assert.Empty(first.Changes);

        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/custom/{route}/{definition}", Request(true), Json.Options)).StatusCode);
        var history = await History(owner, pack.Id);
        Assert.Equal(2, history.Count);
        var edit = history[0];
        Assert.Equal(CustomContentChangeAction.Updated, edit.Action);
        Assert.Contains(edit.Changes, x => x.Field == "name" && x.From!.Contains("Old") && x.To!.Contains("New"));
        if (type == "archetype") Assert.Contains(edit.Changes, x => x.Field == "abilities" && x.To!.StartsWith('['));
        if (type == "career") Assert.Contains(edit.Changes, x => x.Field == "careerSkillNames");
        Assert.DoesNotContain(edit.Changes, x => x.Field == "id");
        var system = type == "heroicAbility" ? "RealmsOfTerrinoth" : "GenesysCore";
        var datedReference = (await owner.GetFromJsonAsync<ReferenceResponse>($"/api/reference/{system}", Json.Options))!;
        Assert.Equal(edit.CreatedAt, datedReference.CustomLastEditedAt![definition]);
        Assert.Equal(HttpStatusCode.OK, (await owner.PutAsJsonAsync($"/api/custom/{route}/{definition}", Request(true), Json.Options)).StatusCode);
        Assert.Equal(2, (await History(owner, pack.Id)).Count);

        Assert.Equal(HttpStatusCode.NoContent, (await owner.DeleteAsync($"/api/custom/{route}/{definition}")).StatusCode);
        var deletion = (await History(owner, pack.Id))[0];
        Assert.Equal(CustomContentChangeAction.Deleted, deletion.Action);
        Assert.Equal(edit.DefinitionName, deletion.DefinitionName);
        Assert.Equal(definition, deletion.DefinitionId);
        Assert.Equal(3, (await History(owner, pack.Id)).Count);
        var deletedReference = (await owner.GetFromJsonAsync<ReferenceResponse>($"/api/reference/{system}", Json.Options))!;
        Assert.DoesNotContain(definition, deletedReference.CustomLastEditedAt!.Keys);
    }

    [Fact]
    public async Task GmCampaignContent_NeverFlagsOwnChanges_ForGmOrMembers()
    {
        var gm = await factory.CreateAuthorizedClientAsync();
        var member = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm);
        Assert.Equal(HttpStatusCode.OK, (await member.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!), Json.Options)).StatusCode);
        await gm.CreateLibraryPackAsync(GameSystem.GenesysCore, campaign.Id);
        var packId = (await gm.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs", Json.Options))!.Single().Id;
        var request = new CreateCustomTalentRequest(GameSystem.GenesysCore, "Gm Living Talent", 1, false,
            "Passive", "Own text", 0, 0, 0, 0, 0, PackIds: [packId]);
        var customPath = $"/api/campaigns/{campaign.Id}/custom/talents";
        var created = await gm.PostAsJsonAsync(customPath, request, Json.Options);
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var talent = (await created.Content.ReadFromJsonAsync<TalentDefDto>(Json.Options))!;
        for (var tier = 1; tier <= 2; tier++)
        {
            if (tier == 2)
                Assert.Equal(HttpStatusCode.OK, (await gm.PutAsJsonAsync($"{customPath}/{talent.Id}",
                    request with { Tier = tier }, Json.Options)).StatusCode);
            foreach (var client in new[] { gm, member })
            {
                var pack = Assert.Single((await client.GetFromJsonAsync<List<CampaignHomebrewPackDto>>(
                    $"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!);
                Assert.Equal(client == gm, pack.IsMine);
                // Reproduces the review: creation/edit follows the initial connection timestamp.
                Assert.True(pack.LastChangedAt > pack.ConnectedAt);
                Assert.False(pack.ChangedAfterConnection);
            }
        }
    }

    [Fact]
    public async Task ConnectedOriginal_LiveTierEdit_IsVisibleOnlyToOwnerAndConnectedCampaignParticipants()
    {
        var gm = await factory.CreateAuthorizedClientAsync();
        var author = await factory.CreateAuthorizedClientAsync();
        var member = await factory.CreateAuthorizedClientAsync();
        var outsider = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm);
        foreach (var client in new[] { author, member })
            Assert.Equal(HttpStatusCode.OK, (await client.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!), Json.Options)).StatusCode);
        var request = new CreateCustomTalentRequest(GameSystem.GenesysCore, "Living Talent", 1, false, "Passive", "Own text", 0, 0, 0, 0, 0);
        var talent = (await (await author.PostAsJsonAsync("/api/custom/talents", request, Json.Options)).Content.ReadFromJsonAsync<TalentDefDto>(Json.Options))!;
        var pack = await author.CreateLibraryPackAsync();
        var share = (await (await author.PostAsync($"/api/homebrew-packs/{pack.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        Assert.Equal(HttpStatusCode.OK, (await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{share.Token}/import", null)).StatusCode);
        var connection = Assert.Single((await member.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!);
        Assert.True(connection.OwnerIsMember);
        Assert.True(connection.LastChangedAt <= connection.ConnectedAt);
        Assert.False(connection.ChangedAfterConnection);
        Assert.Equal(HttpStatusCode.OK, (await author.PutAsJsonAsync($"/api/custom/talents/{talent.Id}", request with { Tier = 2 }, Json.Options)).StatusCode);
        var edited = Assert.Single(await History(author, pack.Id), x => x.Action == CustomContentChangeAction.Updated);
        Assert.Equal(new CustomContentFieldChangeDto("tier", "1", "2"), Assert.Single(edited.Changes));
        foreach (var client in new[] { gm, member })
        {
            Assert.Contains(await History(client, pack.Id, campaign.Id), x => x.Id == edited.Id);
            var reference = (await client.GetFromJsonAsync<ReferenceResponse>($"/api/reference/GenesysCore?campaignId={campaign.Id}", Json.Options))!;
            Assert.Equal(2, Assert.Single(reference.Talents, x => x.Id == talent.Id).Tier);
            Assert.Equal(edited.CreatedAt, reference.CustomLastEditedAt![talent.Id]);
            Assert.DoesNotContain(talent.Id, reference.EditableCustomIds!);
            var metadata = Assert.Single((await client.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!);
            Assert.Equal(edited.CreatedAt, metadata.LastChangedAt);
            Assert.True(metadata.LastChangedAt > metadata.ConnectedAt);
            Assert.True(metadata.ChangedAfterConnection);
        }
        var otherCampaign = await Campaign(outsider);
        foreach (var (client, context) in new[] { (outsider, campaign.Id), (outsider, otherCampaign.Id), (gm, otherCampaign.Id) })
            Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync($"/api/homebrew-packs/{pack.Id}/changes?campaignId={context}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await member.GetAsync($"/api/homebrew-packs/{pack.Id}/changes")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await outsider.GetAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PutAsJsonAsync($"/api/custom/talents/{talent.Id}", request, Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await author.PutAsJsonAsync($"/api/custom/talents/{talent.Id}", request with { Tier = 6 }, Json.Options)).StatusCode);
        Assert.Equal(2, (await History(author, pack.Id)).Count);
        // Disabling the pack hides reference entries, while its approved history remains readable.
        Assert.Equal(HttpStatusCode.NoContent, (await gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(false), Json.Options)).StatusCode);
        Assert.False(Assert.Single((await member.GetFromJsonAsync<List<CampaignHomebrewPackDto>>(
            $"/api/campaigns/{campaign.Id}/homebrew-packs/", Json.Options))!).ChangedAfterConnection);
        Assert.Equal(2, (await History(member, pack.Id, campaign.Id)).Count);
        var disabled = (await member.GetFromJsonAsync<ReferenceResponse>($"/api/reference/GenesysCore?campaignId={campaign.Id}", Json.Options))!;
        Assert.DoesNotContain(talent.Id, disabled.CustomLastEditedAt!.Keys);
        Assert.DoesNotContain(talent.Id, (await outsider.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!.CustomLastEditedAt!.Keys);
        var authorId = edited.UserId;
        Assert.Equal(HttpStatusCode.NoContent, (await gm.DeleteAsync($"/api/campaigns/{campaign.Id}/members/{authorId}")).StatusCode);
        Assert.Equal(2, (await History(member, pack.Id, campaign.Id)).Count);
        Assert.Equal(HttpStatusCode.OK, (await author.GetAsync($"/api/homebrew-packs/{pack.Id}/changes")).StatusCode);
    }

    [Fact]
    public async Task Imports_HaveNoInventedHistoryOrDates_IncludingSharedCopies()
    {
        var author = await factory.CreateAuthorizedClientAsync();
        await author.PostAsJsonAsync("/api/custom/skills", new CreateCustomSkillRequest(GameSystem.GenesysCore, "Imported Sailing", CharacteristicType.Agility, SkillKind.General), Json.Options);
        var source = await author.CreateLibraryPackAsync();
        var document = (await author.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{source.Id}/export", Json.Options))!;
        var share = (await (await author.PostAsync($"/api/homebrew-packs/{source.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        foreach (var shared in new[] { false, true })
        {
            var importer = await factory.CreateAuthorizedClientAsync();
            var response = shared ? await importer.PostAsync($"/api/homebrew-packs/shared/{share.Token}/import", null)
                : await importer.PostAsJsonAsync("/api/homebrew-packs/import", document, Json.Options);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var pack = (await response.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
            Assert.Empty(await History(importer, pack.Id));
            var reference = (await importer.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
            Assert.Contains(reference.Skills, x => x.Name == "Imported Sailing");
            Assert.Empty(reference.CustomLastEditedAt!);
        }
    }

    [Fact]
    public async Task History_IsNewestFirst_AndClampsTakeTo200()
    {
        var owner = await factory.CreateAuthorizedClientAsync();
        var pack = await owner.CreateLibraryPackAsync();
        var skill = (await (await owner.PostAsJsonAsync("/api/custom/skills", new CreateCustomSkillRequest(GameSystem.GenesysCore, "Paged Sailing", CharacteristicType.Agility, SkillKind.General, [pack.Id]), Json.Options))
            .Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;
        var original = Assert.Single(await History(owner, pack.Id));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.CustomContentChanges.AddRange(Enumerable.Range(1, 210).Select(n => new CustomContentChange
            {
                Id = Guid.NewGuid(), HomebrewPackId = pack.Id, DefinitionId = skill.Id, DefinitionType = "skill",
                DefinitionName = skill.Name, UserId = original.UserId, Action = CustomContentChangeAction.Updated,
                CreatedAt = original.CreatedAt.AddSeconds(n),
            }));
            await db.SaveChangesAsync();
        }
        var rows = (await owner.GetFromJsonAsync<List<CustomContentChangeDto>>($"/api/homebrew-packs/{pack.Id}/changes?take=999", Json.Options))!;
        Assert.Equal(200, rows.Count);
        Assert.Equal(original.CreatedAt.AddSeconds(210), rows[0].CreatedAt);
        Assert.Equal(rows.OrderByDescending(x => x.CreatedAt), rows);
        Assert.Single((await owner.GetFromJsonAsync<List<CustomContentChangeDto>>($"/api/homebrew-packs/{pack.Id}/changes?take=-1", Json.Options))!);
        Assert.Equal(HttpStatusCode.Unauthorized, (await factory.CreateClient().GetAsync($"/api/homebrew-packs/{pack.Id}/changes")).StatusCode);
    }
}
