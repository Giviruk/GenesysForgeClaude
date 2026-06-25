using System.Net.Http.Json;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Api.Tests;

public class QualityReferenceTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;
    public QualityReferenceTests(ApiFactory factory) => _factory = factory;

    private async Task<ReferenceResponse> ReferenceAsync(string system = "RealmsOfTerrinoth")
    {
        var client = await _factory.CreateAuthorizedClientAsync();
        return (await client.GetFromJsonAsync<ReferenceResponse>($"/api/reference/{system}", Json.Options))!;
    }

    [Fact]
    public async Task Reference_IncludesQualityCatalog()
    {
        var reference = await ReferenceAsync();

        Assert.NotEmpty(reference.Qualities);
        var accurate = reference.Qualities.FirstOrDefault(q => q.NameEn == "Accurate");
        Assert.NotNull(accurate);
        Assert.True(accurate!.HasRating);

        var knockdown = reference.Qualities.FirstOrDefault(q => q.NameEn == "Knockdown");
        Assert.NotNull(knockdown);
        Assert.False(knockdown!.HasRating);
    }

    [Fact]
    public async Task BuiltInWeapon_PropertiesBackfilledIntoStructuredQualities()
    {
        var reference = await ReferenceAsync();

        // Берём любой встроенный предмет, чья строка свойств содержит «Точное N».
        var item = reference.Items.FirstOrDefault(i => i.Properties.Contains("Точное"));
        Assert.NotNull(item);

        var accurate = item!.Qualities.FirstOrDefault(q => q.Code == "accurate");
        Assert.NotNull(accurate);
        Assert.True(accurate!.Rating >= 1);          // рейтинг распарсен из строки
        Assert.True(accurate.HasRating);
        Assert.Equal("Точное", accurate.NameRu);
    }

    [Fact]
    public async Task NonRatedQuality_HasNullRating()
    {
        var reference = await ReferenceAsync();

        // Предмет со свойством без рейтинга (например «Нокдаун») — структурный рейтинг должен быть null.
        var item = reference.Items.FirstOrDefault(i => i.Properties.Contains("Нокдаун"));
        Assert.NotNull(item);
        var knockdown = item!.Qualities.FirstOrDefault(q => q.Code == "knockdown");
        Assert.NotNull(knockdown);
        Assert.Null(knockdown!.Rating);
    }

    [Fact]
    public async Task AliasProperty_IsResolved()
    {
        var reference = await ReferenceAsync();

        // «Оглушающее N» — вариант написания качества «Оглушение» (Stun); должно сопоставиться.
        var item = reference.Items.FirstOrDefault(i => i.Properties.Contains("Оглушающее"));
        if (item is null) return; // нет такого встроенного предмета — пропускаем
        Assert.Contains(item.Qualities, q => q.Code == "stun");
    }
}
