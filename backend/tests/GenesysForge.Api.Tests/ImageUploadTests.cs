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
}
