using System.Net.Http.Json;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using GenesysForge.Infrastructure.Persistence;

namespace GenesysForge.Api.Tests;

/// <summary>
/// GEN-EQP-QUAL-01: у каждого качества есть структурные метаданные, а у тех, что приложение
/// исполняет, — типизированная механика. Проверяются значения, а не количество записей.
/// </summary>
public class RotQualityCatalogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Одна строка ожидаемых метаданных качества.</summary>
    public sealed record QualityRow(
        string Code, QualityEffectKind Effect, bool Active, int AdvantageCost, bool CanActivateOnMiss)
    {
        public override string ToString() => Code;
    }

    public static IEnumerable<QualityRow> Expected =>
    [
        new("accurate", QualityEffectKind.AttackBoost, false, 0, false),
        new("inaccurate", QualityEffectKind.AttackSetback, false, 0, false),
        new("cumbersome", QualityEffectKind.DifficultyPerMissingBrawn, false, 0, false),
        new("unwieldy", QualityEffectKind.DifficultyPerMissingAgility, false, 0, false),
        new("superior", QualityEffectKind.AutomaticAdvantage, false, 0, false),
        new("inferior", QualityEffectKind.AutomaticThreat, false, 0, false),
        new("defensive", QualityEffectKind.DefenseMelee, false, 0, false),
        new("deflection", QualityEffectKind.DefenseRanged, false, 0, false),
        new("pierce", QualityEffectKind.IgnoreSoak, false, 0, false),
        new("breach", QualityEffectKind.IgnoreSoakTenfold, false, 0, false),
        new("reinforced", QualityEffectKind.ImmuneToPierceAndSunder, false, 0, false),
        new("vicious", QualityEffectKind.CriticalBonusTenfold, false, 0, false),
        // Активные качества: по умолчанию два преимущества и требование попадания.
        new("stun", QualityEffectKind.Descriptive, true, 2, false),
        new("concussive", QualityEffectKind.Descriptive, true, 2, false),
        new("knockdown", QualityEffectKind.Descriptive, true, 2, false),
        // Исключения из ТЗ: Повреждение стоит одно преимущество, Наведение — три,
        // а Взрыв единственный активируется на промахе.
        new("sunder", QualityEffectKind.Descriptive, true, 1, false),
        new("guided", QualityEffectKind.Descriptive, true, 3, false),
        new("blast", QualityEffectKind.Descriptive, true, 2, true),
    ];

    private async Task<ReferenceResponse> ReferenceAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        return (await client.GetFromJsonAsync<ReferenceResponse>("/api/reference/RealmsOfTerrinoth", Json.Options))!;
    }

    public static TheoryData<QualityRow> Rows() => [.. Expected];

    [Theory]
    [MemberData(nameof(Rows))]
    public async Task EveryQuality_HasItsStructuralMetadata(QualityRow expected)
    {
        var reference = await ReferenceAsync();
        var quality = reference.Qualities.Single(q => q.Code == expected.Code);

        Assert.Equal(expected, new QualityRow(
            quality.Code, quality.EffectKind, quality.IsActive, quality.AdvantageCost,
            quality.CanActivateOnMiss));
    }

    [Fact]
    public async Task ActiveQualities_RequireAHit_AndTriumphMayPayForThem()
    {
        var reference = await ReferenceAsync();

        foreach (var quality in reference.Qualities.Where(q => q.IsActive))
        {
            Assert.True(quality.RequiresHit, $"«{quality.Code}» активное, но не требует попадания.");
            Assert.True(quality.TriumphMayPay, $"«{quality.Code}»: триумф обязан оплачивать активацию.");
        }
    }

    [Fact]
    public async Task PassiveQualities_CarryNoActivationFlags()
    {
        var reference = await ReferenceAsync();

        foreach (var quality in reference.Qualities.Where(q => !q.IsActive))
        {
            Assert.Equal(0, quality.AdvantageCost);
            Assert.False(quality.RequiresHit);
            Assert.False(quality.CanActivateOnMiss);
        }
    }

    [Fact]
    public async Task RepeatableQualities_AreMarkedAsSuch()
    {
        var reference = await ReferenceAsync();

        // Залповое и Автоматическое повторяются на каждое дополнительное попадание,
        // Жжение и Ошеломление — на каждую поражённую цель.
        Assert.Equal(QualityRepeatability.PerAdditionalHit,
            reference.Qualities.Single(q => q.Code == "linked").Repeatability);
        Assert.Equal(QualityRepeatability.PerAdditionalHit,
            reference.Qualities.Single(q => q.Code == "auto-fire").Repeatability);
        Assert.Equal(QualityRepeatability.PerHitTarget,
            reference.Qualities.Single(q => q.Code == "burn").Repeatability);
        Assert.Equal(QualityRepeatability.PerHitTarget,
            reference.Qualities.Single(q => q.Code == "concussive").Repeatability);
    }

    [Fact]
    public void Catalogue_HasNoBrokenImportArtifacts_AndIsSelfConsistent()
    {
        // Взрыв, Жжение и Повреждение приехали из CSV с оборванными строками и висящими кавычками.
        var issues = QualityContentValidator.Validate(QualityCatalog.Load());

        Assert.Empty(issues.Select(i => $"{i.Code}: {i.Problem} — {i.Message}"));
    }

    [Fact]
    public async Task BlastCarriesBothCosts_AndItsCategoryIsNotAPriceTag()
    {
        var reference = await ReferenceAsync();
        var blast = reference.Qualities.Single(q => q.Code == "blast");

        Assert.Contains("3 преимущества при промахе", blast.ActivationCost);
        Assert.DoesNotContain("преимущест", blast.Category);
    }
}
