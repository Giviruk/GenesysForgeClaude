using System.Net;
using GenesysForge.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;

namespace GenesysForge.Api.Tests;

public class ContentLibraryV2Tests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static async Task<HomebrewPackListItemDto> Pack(HttpClient client, string name = "World", GameSystem system = GameSystem.GenesysCore) =>
        (await (await client.PostAsJsonAsync("/api/homebrew-packs", new PackDetailsRequest(name, "User content", system), Json.Options))
            .Content.ReadFromJsonAsync<HomebrewPackListItemDto>(Json.Options))!;
    private static async Task<SkillDefDto> Skill(HttpClient client, string name = "Own Navigation", params Guid[] packs) =>
        (await (await client.PostAsJsonAsync("/api/custom/skills", new CreateCustomSkillRequest(GameSystem.GenesysCore, name,
            CharacteristicType.Agility, SkillKind.General, packs), Json.Options)).Content.ReadFromJsonAsync<SkillDefDto>(Json.Options))!;
    private static async Task<CampaignDetailDto> Campaign(HttpClient client) =>
        (await (await client.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("V2 world", ""), Json.Options)).Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
    private static async Task<Guid> Character(HttpClient client) =>
        (await (await client.PostAsJsonAsync("/api/characters/", await CharacterRequest(client), Json.Options))
            .Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options))!["id"];
    private static async Task<CreateCharacterRequest> CharacterRequest(HttpClient client, Guid? campaign = null)
    {
        var r = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        return new("Explorer", GameSystem.GenesysCore, r.Archetypes[0].Id, r.Careers[0].Id, null, CampaignId: campaign);
    }
    private static async Task<ReferenceResponse> Reference(HttpClient client, Guid? campaign = null, Guid? character = null) =>
        (await client.GetFromJsonAsync<ReferenceResponse>($"/api/reference/GenesysCore?{(campaign is null ? "" : $"campaignId={campaign}&")}{(character is null ? "" : $"characterId={character}")}", Json.Options))!;
    private static ContentEntriesRequest Entries(Guid id) => new([new(CustomEntryType.Skill, id)]);
    private static async Task NoContent(Task<HttpResponseMessage> response) => Assert.Equal(HttpStatusCode.NoContent, (await response).StatusCode);

    private async Task DateConnectionBeforeEdit(Guid campaign, Guid pack)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        var connection = await db.HomebrewPackCampaigns.SingleAsync(x => x.CampaignId == campaign && x.HomebrewPackId == pack);
        connection.UpdatedAt = DateTime.UtcNow.AddDays(-3);
        foreach (var change in await db.CustomContentChanges.Where(x => x.HomebrewPackId == pack).ToListAsync())
            change.CreatedAt = DateTime.UtcNow.AddDays(-2);
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task ApprovalToggleAndSharedReconnect_ResetTheChangeWarning()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm); var pack = await Pack(player);
        (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!), Json.Options)).EnsureSuccessStatusCode();
        var proposals = $"/api/campaigns/{campaign.Id}/content/proposals";
        await NoContent(player.PostAsJsonAsync(proposals, new ContentProposalRequest(PackId: pack.Id), Json.Options));
        await Skill(player, "Added after proposal", pack.Id);
        await DateConnectionBeforeEdit(campaign.Id, pack.Id);
        async Task<CampaignHomebrewPackDto> Connection() => Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs", Json.Options))!);
        await NoContent(gm.PostAsync($"{proposals}/pack/{pack.Id}/approve", null));
        Assert.False((await Connection()).ChangedAfterConnection);
        await DateConnectionBeforeEdit(campaign.Id, pack.Id);
        Assert.True((await Connection()).ChangedAfterConnection);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(false), Json.Options));
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(true), Json.Options));
        Assert.False((await Connection()).ChangedAfterConnection);
        await DateConnectionBeforeEdit(campaign.Id, pack.Id);
        Assert.True((await Connection()).ChangedAfterConnection);
        var share = (await (await player.PostAsync($"/api/homebrew-packs/{pack.Id}/share", null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        (await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{share.Token}/import", null)).EnsureSuccessStatusCode();
        Assert.False((await Connection()).ChangedAfterConnection);
    }

    [Fact]
    public async Task ManualToAuto_EnablesAllPendingAndPreservesDisabledEntries()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm); var pack = await Pack(gm);
        var disabled = await Skill(gm, "Explicitly disabled", pack.Id);
        var connection = $"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}";
        await NoContent(gm.PutAsJsonAsync(connection, new HomebrewPackToggleRequest(true, ContentUpdatePolicy.Manual), Json.Options));
        await NoContent(gm.PutAsJsonAsync($"{connection}/entries", new CampaignPackEntriesRequest([new(CustomEntryType.Skill, disabled.Id, false)]), Json.Options));
        var pending = new List<SkillDefDto>();
        for (var i = 0; i < 3; i++) pending.Add(await Skill(gm, $"Pending {i}", pack.Id));
        var id = await Character(player);
        (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, id), Json.Options)).EnsureSuccessStatusCode();
        var before = await Reference(player, character: id);
        Assert.All(pending, skill => Assert.DoesNotContain(before.Skills, x => x.Id == skill.Id));
        await NoContent(gm.PutAsJsonAsync(connection, new HomebrewPackToggleRequest(true, ContentUpdatePolicy.Auto), Json.Options));
        var reference = await Reference(player, character: id);
        foreach (var skill in pending)
        {
            Assert.Contains(reference.Skills, x => x.Id == skill.Id);
            await NoContent(player.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null));
        }
        Assert.DoesNotContain(reference.Skills, x => x.Id == disabled.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync($"/api/characters/{id}/skills/{disabled.Id}/buy-rank", null)).StatusCode);
        var states = Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs", Json.Options))!).Entries!;
        Assert.All(pending, skill => Assert.Equal("enabled", states.Single(x => x.EntryId == skill.Id).State));
        Assert.Equal("disabled", states.Single(x => x.EntryId == disabled.Id).State);
    }

    [Fact]
    public async Task DirectItems_RequireGmOwnership_AndPlayerProposalIsApprovedSeparately()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm);
        (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!), Json.Options)).EnsureSuccessStatusCode();
        var own = await Skill(gm, "GM owned"); var foreign = await Skill(player, "Private player entry");
        var url = $"/api/campaigns/{campaign.Id}/content/items";
        var response = await gm.PostAsJsonAsync(url, new ContentEntriesRequest([new(CustomEntryType.Skill, own.Id), new(CustomEntryType.Skill, foreign.Id)]), Json.Options);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Контент игрока подключается только через его предложение или shared-ссылку.",
            (await response.Content.ReadFromJsonAsync<ErrorResponse>(Json.Options))!.Message);
        Assert.Empty((await gm.GetFromJsonAsync<List<CampaignContentItemDto>>(url, Json.Options))!);
        await NoContent(gm.PostAsJsonAsync(url, Entries(own.Id), Json.Options));
        Assert.Contains((await Reference(player, campaign.Id)).Skills, x => x.Id == own.Id);
        var proposals = $"/api/campaigns/{campaign.Id}/content/proposals";
        await NoContent(player.PostAsJsonAsync(proposals, new ContentProposalRequest(EntryType: CustomEntryType.Skill, EntryId: foreign.Id), Json.Options));
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PostAsJsonAsync(url, Entries(foreign.Id), Json.Options)).StatusCode);
        Assert.DoesNotContain((await Reference(player, campaign.Id)).Skills, x => x.Id == foreign.Id);
        await NoContent(gm.PostAsync($"{proposals}/item/{foreign.Id}/approve", null));
        Assert.Contains((await Reference(player, campaign.Id)).Skills, x => x.Id == foreign.Id);
    }

    [Fact]
    public async Task NextPool_MatchesPurchasedPool_AndStopsAtCreationLimitOrDisabledContent()
    {
        var client = await factory.CreateAuthorizedClientAsync(); var campaign = await Campaign(client);
        var skill = await Skill(client, "Preview navigation"); var id = await Character(client);
        async Task<CharacterSkillDto> Row() => Assert.Single((await client.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!.Skills, x => x.SkillDefId == skill.Id);
        var before = await Row(); Assert.NotNull(before.NextPool);
        await NoContent(client.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null));
        var after = await Row(); Assert.Equal(before.NextPool, after.Pool); Assert.NotNull(after.NextPool);
        (await client.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, id), Json.Options)).EnsureSuccessStatusCode();
        Assert.Null((await Row()).NextPool);
        await NoContent(client.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/content/items", Entries(skill.Id), Json.Options));
        before = await Row(); Assert.NotNull(before.NextPool);
        await NoContent(client.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null));
        after = await Row(); Assert.Equal(before.NextPool, after.Pool); Assert.Null(after.NextPool);
    }

    [Fact]
    public async Task StandaloneEntries_ManyPacks_Toggles_AndDeletionKeepIdentity()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var s = await Skill(client);
        Assert.Empty((await client.GetFromJsonAsync<List<HomebrewPackListItemDto>>("/api/homebrew-packs", Json.Options))!);
        var a = await Pack(client, "A"); var b = await Pack(client, "B");
        await NoContent(client.PostAsJsonAsync($"/api/homebrew-packs/{a.Id}/entries", Entries(s.Id), Json.Options));
        await NoContent(client.PostAsJsonAsync($"/api/homebrew-packs/{b.Id}/entries", Entries(s.Id), Json.Options));
        var row = Assert.Single((await client.GetFromJsonAsync<List<LibraryEntryDto>>("/api/library?system=GenesysCore", Json.Options))!);
        Assert.Equal(2, row.PackIds.Count); Assert.Equal(s.Id, row.Id);
        await NoContent(client.PutAsJsonAsync($"/api/homebrew-packs/{a.Id}/default", new HomebrewPackToggleRequest(false), Json.Options));
        Assert.Contains((await Reference(client)).Skills, x => x.Id == s.Id);
        await NoContent(client.PutAsJsonAsync($"/api/homebrew-packs/{b.Id}/default", new HomebrewPackToggleRequest(false), Json.Options));
        Assert.DoesNotContain((await Reference(client)).Skills, x => x.Id == s.Id);
        await NoContent(client.DeleteAsync($"/api/homebrew-packs/{a.Id}"));
        await NoContent(client.DeleteAsync($"/api/homebrew-packs/{b.Id}"));
        Assert.Empty(Assert.Single((await client.GetFromJsonAsync<List<LibraryEntryDto>>("/api/library", Json.Options))!).PackIds);
        Assert.Contains((await Reference(client)).Skills, x => x.Id == s.Id);
    }

    [Fact]
    public async Task BookRestrictions_TruthTable_ManualRestore_TwoPacks_AndOutsideContext()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(client); var a = await Pack(client, "A"); var b = await Pack(client, "B");
        var catalog = (await client.GetFromJsonAsync<List<BaseCatalogEntryDto>>("/api/reference/base-catalog?system=GenesysCore", Json.Options))!;
        var entry = catalog.First(x => x.Category == BaseContentCategory.Skill);
        var exclusions = new PackExclusionsRequest([new(entry.Category, entry.Key)]);
        foreach (var p in new[] { a, b })
        {
            await NoContent(client.PostAsJsonAsync($"/api/homebrew-packs/{p.Id}/exclusions", exclusions, Json.Options));
            await NoContent(client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{p.Id}", new HomebrewPackToggleRequest(true), Json.Options));
        }
        async Task<CampaignBaseEntryDto> Base() => (await client.GetFromJsonAsync<List<CampaignBaseEntryDto>>(
            $"/api/campaigns/{campaign.Id}/content/base?system=GenesysCore&category=Skill", Json.Options))!.Single(x => x.Key == entry.Key);
        async Task Set(bool? on) => await NoContent(client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/content/base", new BaseOverridesRequest(GameSystem.GenesysCore, [new(entry.Category, entry.Key, on)]), Json.Options));
        Assert.False((await Base()).Enabled); Assert.Equal("pack", (await Base()).Source);
        Assert.Contains((await Reference(client)).Skills, x => x.Name == entry.Name);
        await Set(true); Assert.True((await Base()).Enabled); Assert.Equal("restored", (await Base()).Source);
        await Set(null); Assert.False((await Base()).Enabled);
        await NoContent(client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{a.Id}", new HomebrewPackToggleRequest(false), Json.Options));
        Assert.False((await Base()).Enabled);
        await NoContent(client.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{b.Id}", new HomebrewPackToggleRequest(false), Json.Options));
        Assert.True((await Base()).Enabled);
        await Set(false); Assert.Equal("manual", (await Base()).Source); Assert.False((await Base()).Enabled);
        await Set(true); Assert.Null((await Base()).Source);
        var summary = (await client.GetFromJsonAsync<CampaignContentDto>($"/api/campaigns/{campaign.Id}/content", Json.Options))!;
        Assert.Equal(0, summary.OverrideCount);
    }

    [Fact]
    public async Task ManualUpdates_ArePending_ExistingRanksRemain_AndDecisionEnablesOriginal()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm); var pack = await Pack(gm);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}", new HomebrewPackToggleRequest(true, ContentUpdatePolicy.Manual), Json.Options));
        var skill = await Skill(gm, "Campaign Navigation", pack.Id);
        var state = Assert.Single((await gm.GetFromJsonAsync<List<CampaignHomebrewPackDto>>($"/api/campaigns/{campaign.Id}/homebrew-packs", Json.Options))!).Entries![0];
        Assert.Equal("pending", state.State);
        var id = await Character(player);
        Assert.Equal(HttpStatusCode.OK, (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, id), Json.Options)).StatusCode);
        Assert.DoesNotContain((await Reference(player, character: id)).Skills, x => x.Id == skill.Id);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}/entries", new CampaignPackEntriesRequest([new(CustomEntryType.Skill, skill.Id, true)]), Json.Options));
        Assert.Contains((await Reference(player, character: id)).Skills, x => x.Id == skill.Id);
        await NoContent(player.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null));
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{pack.Id}/entries", new CampaignPackEntriesRequest([new(CustomEntryType.Skill, skill.Id, false)]), Json.Options));
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank", null)).StatusCode);
        var sheet = (await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}", Json.Options))!;
        var retained = Assert.Single(sheet.Skills, x => x.SkillDefId == skill.Id);
        Assert.Equal(1, retained.Ranks); Assert.NotNull(retained.UnavailableReason);
    }

    [Fact]
    public async Task PlayerProposals_ApproveDeclineResubmitWithdraw_AndOwnership()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync(); var stranger = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!), Json.Options);
        var skill = await Skill(player); var pack = await Pack(player);
        var url = $"/api/campaigns/{campaign.Id}/content/proposals";
        await NoContent(player.PostAsJsonAsync(url, new ContentProposalRequest(EntryType: CustomEntryType.Skill, EntryId: skill.Id), Json.Options));
        Assert.DoesNotContain((await Reference(player, campaign.Id)).Skills, x => x.Id == skill.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.PostAsJsonAsync(url, new ContentProposalRequest(PackId: pack.Id), Json.Options)).StatusCode);
        await NoContent(gm.PostAsync($"{url}/item/{skill.Id}/decline", null));
        Assert.Equal(ContentConnectionStatus.Declined, Assert.Single((await player.GetFromJsonAsync<List<LibraryProposalDto>>("/api/library/proposals", Json.Options))!).Status);
        await NoContent(player.PostAsJsonAsync(url, new ContentProposalRequest(EntryType: CustomEntryType.Skill, EntryId: skill.Id), Json.Options));
        await NoContent(gm.PostAsync($"{url}/item/{skill.Id}/approve", null));
        Assert.Equal(skill.Id, Assert.Single((await Reference(player, campaign.Id)).Skills, x => x.Name == skill.Name).Id);
        await NoContent(player.PostAsJsonAsync(url, new ContentProposalRequest(PackId: pack.Id), Json.Options));
        Assert.Contains((await gm.GetFromJsonAsync<CampaignContentDto>($"/api/campaigns/{campaign.Id}/content", Json.Options))!.Alerts, x => x.Kind == "pendingPack");
        await NoContent(player.DeleteAsync($"{url}/pack/{pack.Id}"));
        Assert.DoesNotContain((await player.GetFromJsonAsync<List<LibraryProposalDto>>("/api/library/proposals", Json.Options))!, x => x.TargetId == pack.Id);
        Assert.Equal(HttpStatusCode.BadRequest, (await gm.PostAsJsonAsync($"/api/homebrew-packs/{pack.Id}/entries", Entries(skill.Id), Json.Options)).StatusCode);
    }

    [Fact]
    public async Task ClosedSystem_BlocksCreationAndJoin_AndLeavesExistingCharacterWorking()
    {
        var gm = await factory.CreateAuthorizedClientAsync(); var player = await factory.CreateAuthorizedClientAsync();
        var campaign = await Campaign(gm); var existing = await Character(player);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, existing), Json.Options);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/content/systems/GenesysCore", new SystemOpenRequest(false), Json.Options));
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsJsonAsync("/api/characters/", await CharacterRequest(player, campaign.Id), Json.Options)).StatusCode);
        var newId = await Character(player);
        Assert.Equal(HttpStatusCode.BadRequest, (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, newId), Json.Options)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await player.GetAsync($"/api/characters/{existing}")).StatusCode);
        Assert.Contains(GameSystem.GenesysCore, (await player.GetFromJsonAsync<CampaignDetailDto>($"/api/campaigns/{campaign.Id}", Json.Options))!.ClosedSystems!);
    }

    [Fact]
    public async Task V1Import_V2Export_UnknownExclusionWarnings_AndStableBookKeys()
    {
        var client = await factory.CreateAuthorizedClientAsync(); var pack = await Pack(client);
        var key = (await client.GetFromJsonAsync<List<BaseCatalogEntryDto>>("/api/reference/base-catalog?system=GenesysCore", Json.Options))!.First();
        await NoContent(client.PostAsJsonAsync($"/api/homebrew-packs/{pack.Id}/exclusions", new PackExclusionsRequest([new(key.Category, key.Key)]), Json.Options));
        var doc = (await client.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{pack.Id}/export", Json.Options))!;
        Assert.Equal("genesysforge.homebrew-pack.v2", doc.Format); Assert.Equal(key.Key, Assert.Single(doc.Exclusions!).Key);
        var response = await client.PostAsJsonAsync("/api/homebrew-packs/import", doc with { Exclusions = [new(key.Category, key.Key), new(BaseContentCategory.Skill, "unknown")] }, Json.Options);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Single((await response.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!.Warnings!);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/homebrew-packs/import", doc with { Format = "genesysforge.homebrew-pack.v1", Exclusions = null }, Json.Options)).StatusCode);
    }
    [Fact]
    public async Task SaveAsPack_KeepsRestorationsAgainstForeignAndMixedPacks()
    {
        var gm=await factory.CreateAuthorizedClientAsync(); var player=await factory.CreateAuthorizedClientAsync();
        var campaign=await Campaign(gm);
        await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!), Json.Options);
        var foreign=await Pack(player,"Player exclusions"); var mixed=await Pack(gm,"Mixed pack");
        await Skill(gm,"Mixed navigation",mixed.Id);
        var rows=(await gm.GetFromJsonAsync<List<BaseCatalogEntryDto>>("/api/reference/base-catalog?system=GenesysCore",Json.Options))!.Where(x=>x.Category==BaseContentCategory.Skill).Take(3).ToList();
        foreach(var (client,pack,row) in new[] { (player,foreign,rows[0]), (gm,mixed,rows[1]) })
            await NoContent(client.PostAsJsonAsync($"/api/homebrew-packs/{pack.Id}/exclusions",new PackExclusionsRequest([new(row.Category,row.Key)]),Json.Options));
        var share=(await (await player.PostAsync($"/api/homebrew-packs/{foreign.Id}/share",null)).Content.ReadFromJsonAsync<HomebrewPackShareDto>(Json.Options))!;
        Assert.Equal(HttpStatusCode.OK,(await gm.PostAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/shared/{share.Token}/import",null)).StatusCode);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/homebrew-packs/{mixed.Id}",new HomebrewPackToggleRequest(true),Json.Options));
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{campaign.Id}/content/base",new BaseOverridesRequest(GameSystem.GenesysCore,[new(rows[0].Category,rows[0].Key,true),new(rows[1].Category,rows[1].Key,true),new(rows[2].Category,rows[2].Key,false)]),Json.Options));
        async Task<List<CampaignBaseEntryDto>> Base()=> (await gm.GetFromJsonAsync<List<CampaignBaseEntryDto>>($"/api/campaigns/{campaign.Id}/content/base?system=GenesysCore",Json.Options))!;
        var before=await Base();
        Assert.Equal(HttpStatusCode.OK,(await gm.PostAsJsonAsync($"/api/campaigns/{campaign.Id}/content/base/save-as-pack",new SaveRestrictionsRequest(GameSystem.GenesysCore,"Saved rules"),Json.Options)).StatusCode);
        var after=await Base();
        Assert.Equal(before.Select(x=>(x.Category,x.Key,x.Enabled)),after.Select(x=>(x.Category,x.Key,x.Enabled)));
        Assert.Equal("restored",after.Single(x=>x.Key==rows[0].Key).Source);
        Assert.Equal("restored",after.Single(x=>x.Key==rows[1].Key).Source);
        Assert.Equal(2,(await gm.GetFromJsonAsync<CampaignContentDto>($"/api/campaigns/{campaign.Id}/content",Json.Options))!.OverrideCount);
    }

    [Fact]
    public async Task CampaignUnion_AndAtomicCreation_PreserveFreeRanksOfDisabledSkills()
    {
        var gm=await factory.CreateAuthorizedClientAsync(); var player=await factory.CreateAuthorizedClientAsync();
        var a=await Campaign(gm); var b=await Campaign(gm);
        await player.PostAsJsonAsync("/api/campaigns/join",new JoinCampaignRequest(a.JoinCode!),Json.Options);
        var r=await Reference(player,a.Id); var career=r.Careers.First(x=>x.CareerSkillNames.Count>0);
        var freeName=career.CareerSkillNames.First(); var skill=r.Skills.First(x=>x.Name==freeName);
        var key=(await gm.GetFromJsonAsync<List<BaseCatalogEntryDto>>("/api/reference/base-catalog?system=GenesysCore",Json.Options))!.Single(x=>x.Category==BaseContentCategory.Skill && x.Name==freeName);
        await NoContent(gm.PutAsJsonAsync($"/api/campaigns/{a.Id}/content/base",new BaseOverridesRequest(GameSystem.GenesysCore,[new(key.Category,key.Key,false)]),Json.Options));
        var request=(await CharacterRequest(player,a.Id)) with {CareerId=career.Id,FreeCareerSkillNames=[freeName]};
        var created=await player.PostAsJsonAsync("/api/characters/",request,Json.Options);
        Assert.Equal(HttpStatusCode.Created,created.StatusCode);
        var id=(await created.Content.ReadFromJsonAsync<Dictionary<string,Guid>>(Json.Options))!["id"];
        var sheet=(await player.GetFromJsonAsync<CharacterSheetDto>($"/api/characters/{id}",Json.Options))!;
        var retained=Assert.Single(sheet.Skills,x=>x.SkillDefId==skill.Id);
        Assert.Equal(1,retained.Ranks);Assert.Equal(1,retained.FreeRanks);Assert.NotNull(retained.UnavailableReason);
        Assert.Equal(HttpStatusCode.BadRequest,(await player.PostAsync($"/api/characters/{id}/skills/{skill.Id}/refund-rank",null)).StatusCode);
        await player.PostAsJsonAsync("/api/campaigns/join",new JoinCampaignRequest(b.JoinCode!,id),Json.Options);
        Assert.Contains((await Reference(player,character:id)).Skills,x=>x.Id==skill.Id);
        await NoContent(player.PostAsync($"/api/characters/{id}/skills/{skill.Id}/buy-rank",null));
    }

    [Fact]
    public async Task ImportNonemptyPack_CopiesDefinitions_AndLibraryEditorBypassesOnlyOwnToggles()
    {
        var client=await factory.CreateAuthorizedClientAsync(); var other=await factory.CreateAuthorizedClientAsync();
        var pack=await Pack(client);var skill=await Skill(client,"Export navigation",pack.Id);
        await NoContent(client.PutAsJsonAsync($"/api/homebrew-packs/{pack.Id}/default",new HomebrewPackToggleRequest(false),Json.Options));
        Assert.DoesNotContain((await Reference(client)).Skills,x=>x.Id==skill.Id);
        Assert.Contains((await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore?library=true",Json.Options))!.Skills,x=>x.Id==skill.Id);
        Assert.DoesNotContain((await other.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore?library=true",Json.Options))!.Skills,x=>x.Id==skill.Id);
        var doc=(await client.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{pack.Id}/export",Json.Options))!;
        var response=await other.PostAsJsonAsync("/api/homebrew-packs/import",doc,Json.Options);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var result=(await response.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(1,result.EntryCount);
        var copy=Assert.Single((await other.GetFromJsonAsync<List<LibraryEntryDto>>("/api/library",Json.Options))!);
        Assert.NotEqual(skill.Id,copy.Id);Assert.Contains(result.Id,copy.PackIds);
        var campaign=await Campaign(client);
        Assert.Equal(HttpStatusCode.BadRequest,(await client.GetAsync($"/api/reference/GenesysCore?library=true&campaignId={campaign.Id}")).StatusCode);
    }

    [Fact]
    public async Task V2ImportExport_AttachmentsAndMountsKeepNestedMechanics()
    {
        var client=await factory.CreateAuthorizedClientAsync();
        var response=await client.PostAsJsonAsync("/api/homebrew-packs/import",new {
            format="genesysforge.homebrew-pack.v2",name="Technical profiles",system="realmsOfTerrinoth",
            attachments=new[] {new {name="Custom grip",code="custom.grip",hostKind="weapon",hardPointCost=1,price=40,
                effects=new[] {new {kind="grantOrIncreaseQuality",qualityCode="accurate",value=1,increment=2}}}},
            mounts=new[] {new {name="Custom mount",code="custom.mount",brawn=3,agility=2,capacity=12,woundThreshold=10,price=50,
                skills=new[] {new {name="Athletics",ranks=2,isGroupSkill=false}},
                attacks=new[] {new {name="Kick",skillName="Brawl",damage=4,critical=3,range="engaged",qualityCodes=new[]{"knockdown"}}}}}
        },Json.Options);
        Assert.Equal(HttpStatusCode.Created,response.StatusCode);
        var imported=(await response.Content.ReadFromJsonAsync<HomebrewPackImportResult>(Json.Options))!;
        Assert.Equal(2,imported.EntryCount);
        var doc=(await client.GetFromJsonAsync<HomebrewPackExportDto>($"/api/homebrew-packs/{imported.Id}/export",Json.Options))!;
        Assert.Equal(2,Assert.Single(Assert.Single(doc.Attachments!).Effects).Increment);
        var mount=Assert.Single(doc.Mounts!);Assert.Equal(12,mount.Capacity);Assert.Equal(2,Assert.Single(mount.Skills).Ranks);
        Assert.Equal("knockdown",Assert.Single(Assert.Single(mount.Attacks).QualityCodes));
        var copy=await client.PostAsJsonAsync("/api/homebrew-packs/import",doc,Json.Options);
        Assert.Equal(HttpStatusCode.Created,copy.StatusCode);
        var entries=(await client.GetFromJsonAsync<List<LibraryEntryDto>>("/api/library?system=RealmsOfTerrinoth",Json.Options))!;
        Assert.Equal(4,entries.Count);Assert.Equal(4,entries.Select(x=>x.Id).Distinct().Count());
    }

}
