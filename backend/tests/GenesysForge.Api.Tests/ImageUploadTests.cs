using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Media;
using GenesysForge.Domain;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace GenesysForge.Api.Tests;

/// <summary>Юнит-тесты определения формата изображения по сигнатуре содержимого.</summary>
public class ImageSignatureTests
{
    public static readonly byte[] Png =
    [
        0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D,
    ];

    public static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    public static readonly byte[] Webp =
    [
        0x52, 0x49, 0x46, 0x46, 0x00, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50,
    ];

    [Fact]
    public void Detects_KnownFormats()
    {
        Assert.True(ImageSignature.TryDetect(Png, out var pngType, out var pngExt));
        Assert.Equal(("image/png", "png"), (pngType, pngExt));

        Assert.True(ImageSignature.TryDetect(Jpeg, out var jpegType, out var jpegExt));
        Assert.Equal(("image/jpeg", "jpg"), (jpegType, jpegExt));

        Assert.True(ImageSignature.TryDetect(Webp, out var webpType, out var webpExt));
        Assert.Equal(("image/webp", "webp"), (webpType, webpExt));
    }

    [Fact]
    public void Rejects_OtherContent()
    {
        // GIF и SVG не входят в белый список (SVG умеет содержать скрипты).
        Assert.False(ImageSignature.TryDetect("GIF89a"u8, out _, out _));
        Assert.False(ImageSignature.TryDetect("<svg xmlns=\"…\">"u8, out _, out _));
        Assert.False(ImageSignature.TryDetect([], out _, out _));
        Assert.False(ImageSignature.TryDetect([0xFF, 0xD8], out _, out _)); // обрезанный JPEG-заголовок
    }
}

/// <summary>Фейковое хранилище: запоминает загрузки и удаления, отдаёт предсказуемые URL.</summary>
public sealed class FakeObjectStorage : IObjectStorage
{
    public List<string> Uploaded { get; } = [];
    public List<string> Deleted { get; } = [];

    public bool IsEnabled => true;

    public Task<string> UploadPublicAsync(Stream content, string key, string contentType, CancellationToken ct)
    {
        var url = $"https://storage.test/bucket/genesysforge/uploads/{key}";
        Uploaded.Add(url);
        return Task.FromResult(url);
    }

    public Task DeleteByUrlAsync(string? url, CancellationToken ct)
    {
        if (url is not null) Deleted.Add(url);
        return Task.CompletedTask;
    }
}

public class ImageUploadTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public ImageUploadTests(ApiFactory factory) => _factory = factory;

    private static int _seq;

    private static async Task<HttpClient> RegisterAsync(WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();
        var n = Interlocked.Increment(ref _seq);
        var resp = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest($"upload{n}@test.local", "password123", $"Upload {n}"));
        resp.EnsureSuccessStatusCode();
        var auth = (await resp.Content.ReadFromJsonAsync<AuthResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        return client;
    }

    /// <summary>Фабрика с фейковым хранилищем вместо DisabledObjectStorage.</summary>
    private (WebApplicationFactory<Program> Factory, FakeObjectStorage Storage) WithFakeStorage()
    {
        var storage = new FakeObjectStorage();
        var factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
                services.AddSingleton<IObjectStorage>(storage)));
        return (factory, storage);
    }

    private static ByteArrayContent ImageContent(byte[] bytes) => new(bytes);

    private static async Task<Guid> CreateCharacterAsync(HttpClient client)
    {
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/GenesysCore", Json.Options))!;
        var career = reference.Careers[0];
        var create = await client.PostAsJsonAsync("/api/characters/",
            new CreateCharacterRequest("Portrait Hero", GameSystem.GenesysCore, reference.Archetypes[0].Id, career.Id,
                [career.CareerSkillNames[0]]));
        create.EnsureSuccessStatusCode();
        var body = await create.Content.ReadFromJsonAsync<Dictionary<string, Guid>>(Json.Options);
        return body!["id"];
    }

    [Fact]
    public async Task UploadAvatar_StorageDisabled_Rejected()
    {
        var client = await RegisterAsync(_factory); // без фейка: Storage:Provider=None
        var resp = await client.PostAsync("/api/account/avatar", ImageContent(ImageSignatureTests.Png));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_SavesUrl_AndDeletesPrevious()
    {
        var (factory, storage) = WithFakeStorage();
        var client = await RegisterAsync(factory);

        var first = await client.PostAsync("/api/account/avatar", ImageContent(ImageSignatureTests.Png));
        first.EnsureSuccessStatusCode();
        var account = (await first.Content.ReadFromJsonAsync<AccountDto>(Json.Options))!;
        Assert.Equal(storage.Uploaded[0], account.AvatarUrl);
        Assert.Contains("/avatars/", account.AvatarUrl);
        Assert.EndsWith(".png", account.AvatarUrl);

        // Повторная загрузка заменяет файл и удаляет прежний.
        var second = await client.PostAsync("/api/account/avatar", ImageContent(ImageSignatureTests.Jpeg));
        second.EnsureSuccessStatusCode();
        var updated = (await second.Content.ReadFromJsonAsync<AccountDto>(Json.Options))!;
        Assert.EndsWith(".jpg", updated.AvatarUrl);
        Assert.Contains(account.AvatarUrl!, storage.Deleted);
    }

    [Fact]
    public async Task UploadAvatar_NonImageContent_Rejected()
    {
        var (factory, _) = WithFakeStorage();
        var client = await RegisterAsync(factory);
        var resp = await client.PostAsync("/api/account/avatar",
            ImageContent("<svg onload=\"alert(1)\"/>"u8.ToArray()));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task UploadAvatar_OversizedBody_Rejected()
    {
        var (factory, storage) = WithFakeStorage();
        var client = await RegisterAsync(factory);

        var oversized = new byte[ImageSignature.MaxBytes + 1];
        ImageSignatureTests.Png.CopyTo(oversized, 0);
        var resp = await client.PostAsync("/api/account/avatar", ImageContent(oversized));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task UploadPortrait_OwnCharacter_SetsUrl_VisibleOnSheet()
    {
        var (factory, storage) = WithFakeStorage();
        var client = await RegisterAsync(factory);
        var characterId = await CreateCharacterAsync(client);

        var resp = await client.PostAsync($"/api/characters/{characterId}/portrait",
            ImageContent(ImageSignatureTests.Webp));
        resp.EnsureSuccessStatusCode();
        Assert.Single(storage.Uploaded);

        var sheet = (await client.GetFromJsonAsync<CharacterSheetDto>(
            $"/api/characters/{characterId}", Json.Options))!;
        Assert.Equal(storage.Uploaded[0], sheet.PortraitUrl);
        Assert.Contains($"/portraits/{characterId}/", sheet.PortraitUrl);
    }

    [Fact]
    public async Task UploadPortrait_ForeignCharacter_Rejected()
    {
        var (factory, storage) = WithFakeStorage();
        var owner = await RegisterAsync(factory);
        var characterId = await CreateCharacterAsync(owner);

        var stranger = await RegisterAsync(factory);
        var resp = await stranger.PostAsync($"/api/characters/{characterId}/portrait",
            ImageContent(ImageSignatureTests.Png));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.Empty(storage.Uploaded);
    }

    [Fact]
    public async Task UploadChronicleImage_MemberAllowed_StrangerRejected()
    {
        var (factory, storage) = WithFakeStorage();
        var gm = await RegisterAsync(factory);
        var campaignResponse = await gm.PostAsJsonAsync("/api/campaigns/",
            new CreateCampaignRequest("Иллюстрированная хроника", ""), Json.Options);
        campaignResponse.EnsureSuccessStatusCode();
        var campaign = (await campaignResponse.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;

        var player = await RegisterAsync(factory);
        var characterId = await CreateCharacterAsync(player);
        var join = await player.PostAsJsonAsync("/api/campaigns/join",
            new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options);
        join.EnsureSuccessStatusCode();

        var uploaded = await player.PostAsync($"/api/campaigns/{campaign.Id}/chronicle/images",
            ImageContent(ImageSignatureTests.Png));
        uploaded.EnsureSuccessStatusCode();
        var body = (await uploaded.Content.ReadFromJsonAsync<Dictionary<string, string>>(Json.Options))!;
        Assert.Equal(storage.Uploaded[0], body["imageUrl"]);
        Assert.Contains($"/campaigns/{campaign.Id}/chronicle/", body["imageUrl"]);

        var stranger = await RegisterAsync(factory);
        var rejected = await stranger.PostAsync($"/api/campaigns/{campaign.Id}/chronicle/images",
            ImageContent(ImageSignatureTests.Webp));
        Assert.Equal(HttpStatusCode.BadRequest, rejected.StatusCode);
        Assert.Single(storage.Uploaded);
    }
    private static NpcInput PortraitNpcInput => new(
        "Portrait NPC", GameSystem.GenesysCore, NpcKind.Minion, NpcRole.Skirmisher, null, null,
        2, 2, 2, 2, 2, 2, 5, null, 2, 0, 0, 1, null, NpcVisibility.PublicTemplate,
        null, null, null, null, null, null, null);

    private static async Task<NpcDetailDto> CreateNpcAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/npcs/", PortraitNpcInput, Json.Options);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
    }

    [Theory]
    [InlineData("png")]
    [InlineData("jpg")]
    [InlineData("webp")]
    public async Task NpcPortrait_UploadReplaceDelete_UpdatesDetailAndList(string extension)
    {
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var owner = await RegisterAsync(factory);
        var npc = await CreateNpcAsync(owner);
        var bytes = extension switch { "jpg" => ImageSignatureTests.Jpeg, "webp" => ImageSignatureTests.Webp, _ => ImageSignatureTests.Png };
        var response = await owner.PostAsync($"/api/v1/npcs/{npc.Id}/portrait", ImageContent(bytes));
        response.EnsureSuccessStatusCode();
        var url = (await response.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["portraitUrl"];
        Assert.EndsWith($".{extension}", url);
        Assert.Contains($"/npc-portraits/{npc.Id}/", url);
        var detail = (await owner.GetFromJsonAsync<NpcDetailDto>($"/api/npcs/{npc.Id}", Json.Options))!;
        Assert.Equal(url, detail.PortraitUrl);
        var list = (await owner.GetFromJsonAsync<List<NpcListItemDto>>("/api/npcs/", Json.Options))!;
        Assert.Equal(url, list.Single(n => n.Id == npc.Id).PortraitUrl);
        var viewer = await RegisterAsync(factory);
        Assert.Equal(url, (await viewer.GetFromJsonAsync<NpcDetailDto>($"/api/npcs/{npc.Id}", Json.Options))!.PortraitUrl);

        (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Webp))).EnsureSuccessStatusCode();
        Assert.Contains(url, storage.Deleted);
        Assert.Equal(2, storage.Uploaded.Count);
        Assert.NotEqual(url, storage.Uploaded[1]);
        (await owner.DeleteAsync($"/api/v1/npcs/{npc.Id}/portrait")).EnsureSuccessStatusCode();
        Assert.Contains(storage.Uploaded[1], storage.Deleted);
        Assert.Null((await owner.GetFromJsonAsync<NpcDetailDto>($"/api/npcs/{npc.Id}", Json.Options))!.PortraitUrl);
        // Removing an absent portrait is safe and does not issue another object deletion.
        (await owner.DeleteAsync($"/api/npcs/{npc.Id}/portrait")).EnsureSuccessStatusCode();
        Assert.Equal(2, storage.Deleted.Count);
    }

    [Fact]
    public async Task NpcPortrait_RejectsNonOwnerAndBuiltIn_WithoutStorageChanges()
    {
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var owner = await RegisterAsync(factory);
        var npc = await CreateNpcAsync(owner);
        (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).EnsureSuccessStatusCode();
        var stranger = await RegisterAsync(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.DeleteAsync($"/api/npcs/{npc.Id}/portrait")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await stranger.DeleteAsync($"/api/npcs/{npc.Id}")).StatusCode);

        // Even a built-in with a legacy owner must remain read-only for portrait operations.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            var entity = await db.Npcs.FindAsync(npc.Id);
            entity!.IsBuiltIn = true;
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await owner.DeleteAsync($"/api/npcs/{npc.Id}/portrait")).StatusCode);
        Assert.Single(storage.Uploaded);
        Assert.Empty(storage.Deleted);
    }

    [Theory]
    [InlineData("oversize", "Файл больше 5 МБ.")]
    [InlineData("svg", "Поддерживаются только изображения JPEG, PNG и WebP.")]
    [InlineData("empty", "Файл пуст.")]
    public async Task NpcPortrait_RejectsInvalidFiles_AndPreservesPrevious(string kind, string message)
    {
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var owner = await RegisterAsync(factory);
        var npc = await CreateNpcAsync(owner);
        (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).EnsureSuccessStatusCode();
        var bytes = kind switch { "oversize" => new byte[ImageSignature.MaxBytes + 1], "svg" => "<svg/>"u8.ToArray(), _ => [] };
        var response = await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(bytes));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(message, (await response.Content.ReadFromJsonAsync<Dictionary<string, string>>())!["message"]);
        Assert.Single(storage.Uploaded);
        Assert.Empty(storage.Deleted);
        Assert.Equal(storage.Uploaded[0], (await owner.GetFromJsonAsync<NpcDetailDto>($"/api/npcs/{npc.Id}", Json.Options))!.PortraitUrl);
    }

    [Fact]
    public async Task NpcPortrait_AcceptsExactLimit_AndRejectsDisabledStorage()
    {
        var disabled = await RegisterAsync(_factory);
        var disabledNpc = await CreateNpcAsync(disabled);
        Assert.Equal(HttpStatusCode.BadRequest, (await disabled.PostAsync($"/api/npcs/{disabledNpc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).StatusCode);
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var owner = await RegisterAsync(factory);
        var npc = await CreateNpcAsync(owner);
        var bytes = new byte[ImageSignature.MaxBytes];
        ImageSignatureTests.Png.CopyTo(bytes, 0);
        (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(bytes))).EnsureSuccessStatusCode();
        Assert.Single(storage.Uploaded);
    }

    [Fact]
    public async Task NpcPortrait_DuplicateAndTemplateDoNotCopy_DeleteNpcCleansFile()
    {
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var owner = await RegisterAsync(factory);
        var npc = await CreateNpcAsync(owner);
        (await owner.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Png))).EnsureSuccessStatusCode();
        var duplicate = await owner.PostAsync($"/api/npcs/{npc.Id}/duplicate", null);
        duplicate.EnsureSuccessStatusCode();
        var copy = (await duplicate.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!;
        Assert.Null(copy.PortraitUrl);
        var template = await owner.PostAsJsonAsync("/api/npcs/apply-template",
            new ApplyTemplateRequest(PortraitNpcInput, CreatureTemplate.None), Json.Options);
        template.EnsureSuccessStatusCode();
        Assert.Null((await template.Content.ReadFromJsonAsync<NpcDetailDto>(Json.Options))!.PortraitUrl);
        (await owner.DeleteAsync($"/api/npcs/{copy.Id}")).EnsureSuccessStatusCode();
        Assert.Empty(storage.Deleted);
        (await owner.DeleteAsync($"/api/npcs/{npc.Id}")).EnsureSuccessStatusCode();
        Assert.Equal(storage.Uploaded, storage.Deleted);
    }

    [Fact]
    public async Task GameTablePortraits_UseCurrentSourceUrls_AndExcludeHiddenNpcForPlayers()
    {
        var (factory, storage) = WithFakeStorage();
        using var ownedFactory = factory;
        var gm = await RegisterAsync(factory);
        var player = await RegisterAsync(factory);
        var characterId = await CreateCharacterAsync(player);
        (await player.PostAsync($"/api/characters/{characterId}/portrait", ImageContent(ImageSignatureTests.Png))).EnsureSuccessStatusCode();
        var pcUrl = storage.Uploaded.Last();
        var npc = await CreateNpcAsync(gm);
        (await gm.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Webp))).EnsureSuccessStatusCode();
        var npcUrl = storage.Uploaded.Last();
        var created = await gm.PostAsJsonAsync("/api/campaigns/", new CreateCampaignRequest("Portrait scene", ""), Json.Options);
        var campaign = (await created.Content.ReadFromJsonAsync<CampaignDetailDto>(Json.Options))!;
        (await player.PostAsJsonAsync("/api/campaigns/join", new JoinCampaignRequest(campaign.JoinCode!, characterId), Json.Options)).EnsureSuccessStatusCode();
        var route = $"/api/campaigns/{campaign.Id}/session";
        (await gm.PostAsJsonAsync(route, new CreateSessionRequest("Portraits", null, 1, 1), Json.Options)).EnsureSuccessStatusCode();
        (await gm.PostAsJsonAsync(route + "/participants", new AddParticipantRequest(characterId, null, null, null, null, null, null, null, null, null, null), Json.Options)).EnsureSuccessStatusCode();
        var added = await gm.PostAsJsonAsync(route + "/participants", new AddParticipantRequest(null, npc.Id, null, ParticipantType.MinionGroup, null, 3, null, null, null, null, null), Json.Options);
        added.EnsureSuccessStatusCode();
        var scene = (await added.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        Assert.Equal(pcUrl, scene.Participants.Single(p => p.CharacterId == characterId).PortraitUrl);
        var npcParticipant = scene.Participants.Single(p => p.NpcId == npc.Id);
        Assert.Equal(npcUrl, npcParticipant.PortraitUrl);
        Assert.Equal(npcUrl, (await player.GetFromJsonAsync<GameSessionDto>(route, Json.Options))!.Participants.Single(p => p.NpcId == npc.Id).PortraitUrl);

        (await gm.PostAsync($"/api/npcs/{npc.Id}/portrait", ImageContent(ImageSignatureTests.Jpeg))).EnsureSuccessStatusCode();
        npcUrl = storage.Uploaded.Last();
        var hidden = await gm.PatchAsJsonAsync(route + $"/participants/{npcParticipant.Id}", new { isHiddenFromPlayers = true });
        hidden.EnsureSuccessStatusCode();
        Assert.Equal(npcUrl, (await hidden.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!.Participants.Single(p => p.NpcId == npc.Id).PortraitUrl);
        var playerResponse = await player.GetAsync(route);
        var visible = (await playerResponse.Content.ReadFromJsonAsync<GameSessionDto>(Json.Options))!;
        Assert.Equal(pcUrl, Assert.Single(visible.Participants).PortraitUrl);
        Assert.DoesNotContain(npcUrl, await playerResponse.Content.ReadAsStringAsync());

        (await gm.DeleteAsync($"/api/npcs/{npc.Id}/portrait")).EnsureSuccessStatusCode();
        Assert.Null((await gm.GetFromJsonAsync<GameSessionDto>(route, Json.Options))!.Participants.Single(p => p.NpcId == npc.Id).PortraitUrl);
    }

}
