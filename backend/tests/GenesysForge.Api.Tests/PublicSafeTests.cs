using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Reference;
using GenesysForge.Application.Features.Characters;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GenesysForge.Api.Tests;

public class PublicApiFactory : ApiFactory
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(
            new Dictionary<string, string?> { ["Content:Mode"] = "PublicSafe" }));
        return base.CreateHost(builder);
    }
}

public class PublicSafeTests
{
    private static AppDbContext NewDb() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task PublicApi_Reference_IsBookOnly_AndNoStore()
    {
        using var factory = new PublicApiFactory();
        using var client = await factory.CreateAuthorizedClientAsync();
        var response = await client.GetAsync("/api/v1/reference/RealmsOfTerrinoth");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);
        var reference = (await response.Content.ReadFromJsonAsync<ReferenceResponse>(Json.Options))!;
        Assert.NotEmpty(reference.Talents);
        Assert.All(reference.Talents, t =>
        {
            Assert.Empty(t.Description); Assert.Empty(t.SafeDescription); Assert.Empty(t.DescriptionEn);
            Assert.Contains(", с. ", t.Source);
        });
        Assert.Equal(11, reference.HeroicAbilities.Count);
        Assert.All(reference.HeroicAbilities, h =>
        {
            Assert.Empty(h.Description); Assert.Empty(h.SafeDescription); Assert.Empty(h.DescriptionEn);
            Assert.Empty(h.Notes);
            // Структурные параметры не являются прозой правила и остаются в публичной версии.
            Assert.Equal("2 очка сюжета", h.ActivationCost);
            Assert.NotEmpty(h.Activation); Assert.NotEmpty(h.Duration); Assert.NotEmpty(h.Frequency);
            Assert.NotEmpty(h.Requirement);
            Assert.Contains(", с. ", h.Source);
            Assert.Equal(2, h.Upgrades.Count);
            Assert.All(h.Upgrades, u =>
            {
                Assert.Empty(u.Description); Assert.Empty(u.DescriptionEn); Assert.Empty(u.Notes);
                Assert.Equal(h.Source, u.Source);
            });
            Assert.All(h.Effects, e => Assert.NotEmpty(e.Description));
        });
        Assert.All(reference.HeroicSecondaryEffects, e =>
        {
            Assert.Empty(e.Description); Assert.Empty(e.SafeDescription); Assert.Empty(e.DescriptionEn);
            Assert.Equal("Realms of Terrinoth, с. 79", e.Source);
        });
        Assert.All(reference.Mounts!, m => Assert.NotEmpty(m.Description));
    }

    [Fact]
    public async Task PublicApi_HidesUnconfirmedTalentsFromPurchase_AndSearchShowsBookReference()
    {
        using var factory = new PublicApiFactory();
        using var client = await factory.CreateAuthorizedClientAsync();
        var core = (await client.GetFromJsonAsync<ReferenceResponse>("/api/v1/reference/GenesysCore", Json.Options))!;
        Assert.NotEmpty(core.Talents);
        // Без описания и без подтверждённой страницы талант нельзя осознанно купить.
        Assert.All(core.Talents, t => Assert.Contains(", с. ", t.Source));
        Assert.DoesNotContain(core.Talents, t => t.Name is "Attuned" or "Counterspell" or "Empowered Casting");

        var search = (await client.GetFromJsonAsync<SearchResponse>(
            "/api/search?system=RealmsOfTerrinoth&q=Paragon", Json.Options))!;
        var heroic = Assert.Single(search.Hits, h => h.Type == "heroic");
        Assert.Equal("Realms of Terrinoth, с. 76", heroic.Snippet);
    }

    [Fact]
    public void PrivateSeed_KeepsUnconfirmedTalentsPurchasable()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);
        Assert.False(db.TalentDefs.Single(t => t.System == GameSystem.GenesysCore && t.Name == "Attuned").Retired);
    }

    [Fact]
    public void SecondaryEffectHint_WithoutProse_PointsToTheBook()
    {
        var result = new GenesysForge.Domain.Rules.RuleEffectResult();
        HeroicSecondaryEffectApplier.Apply(
            [new HeroicSecondaryEffectDef
            {
                Code = "rot.heroic.secondary.devastating", Name = "Devastating", NameRu = "Сокрушительный",
                Source = "Realms of Terrinoth, с. 79",
            }],
            new GenesysForge.Domain.Rules.MutableCombatTarget(), result);
        Assert.Equal("Сокрушительный: см. Realms of Terrinoth, с. 79", Assert.Single(result.Manual));
    }

    [Fact]
    public async Task PublicSeed_Reseed_ScrubsOldProse_PreservesCustomContentAndMechanics()
    {
        using var db = NewDb();
        SeedData.Apply(db, ContentMode.PrivateFull);
        var custom = new TalentDef
        {
            Id = Guid.NewGuid(), OwnerUserId = Guid.NewGuid(), Name = "Author content",
            Description = "Author's original text", SafeDescription = "Author hint", DescriptionEn = "Author EN",
            System = GameSystem.GenesysCore,
        };
        db.TalentDefs.Add(custom); db.SaveChanges();
        var legacy = new TalentDef
        {
            Id = Guid.NewGuid(), Name = "Retired built-in", Code = "rot.talent.legacy-unmapped", Retired = true,
            Description = "Old full text", SafeDescription = "Old summary", DescriptionEn = "Old EN text",
            System = GameSystem.RealmsOfTerrinoth,
        };
        db.TalentDefs.Add(legacy); db.SaveChanges();
        var handler = new GetReferenceHandler(db);
        var before = await handler.Handle(new GetReferenceQuery(Guid.NewGuid(), GameSystem.RealmsOfTerrinoth));
        var ids = db.TalentDefs.Where(t => t.OwnerUserId == null).Select(t => t.Id).OrderBy(x => x).ToList();
        SeedData.Apply(db, ContentMode.PublicSafe);
        SeedData.Apply(db, ContentMode.PublicSafe);
        var after = await handler.Handle(new GetReferenceQuery(Guid.NewGuid(), GameSystem.RealmsOfTerrinoth));
        Assert.Equal(Normalize(before), Normalize(after));
        Assert.Equal(ids, db.TalentDefs.Where(t => t.OwnerUserId == null).Select(t => t.Id).OrderBy(x => x).ToList());
        Assert.Equal("Author's original text", custom.Description);
        Assert.Equal("Author hint", custom.SafeDescription); Assert.Equal("Author EN", custom.DescriptionEn);
        Assert.Empty(legacy.Description); Assert.Empty(legacy.SafeDescription); Assert.Empty(legacy.DescriptionEn);
        Assert.All(db.TalentDefs.Where(t => t.OwnerUserId == null), t => Assert.Empty(t.DescriptionEn));
        Assert.All(db.HeroicAbilityDefs.Include(h => h.Upgrades), h => Assert.All(h.Upgrades, u =>
        {
            Assert.Empty(u.Description); Assert.Empty(u.SafeDescription); Assert.Empty(u.DescriptionEn); Assert.Empty(u.Notes);
        }));
        var spends = db.CraftingSpendDefs.ToList().Select(CraftingMapper.ToDto).ToList();
        Assert.Equal(39, spends.Count); Assert.All(spends, s => Assert.NotEmpty(s.Description));
    }

    [Fact]
    public void Catalog_References_AreComplete_ExceptExplicitLegacyExceptions()
    {
        var talents = TalentCatalog.Load().GroupBy(t => t.Code[(t.Code.LastIndexOf('.') + 1)..])
            .Select(g => g.First()).ToList();
        Assert.Equal(123, talents.Count);
        var unconfirmed = talents.Where(t => !t.Source.Contains(", с. ")).Select(t => t.Name).OrderBy(x => x).ToList();
        Assert.Equal(new[] { "Attuned", "Counterspell", "Empowered Casting" }, unconfirmed);
        Assert.All(talents.Where(t => t.Source.Contains(", с. ")), t =>
            Assert.Matches(@"^(Genesys Core Rulebook \(RU translation\)|Realms of Terrinoth), с\. \d+(-\d+)?$", t.Source));
        Assert.Equal("Genesys Core Rulebook (RU translation), с. 73", talents.Single(t => t.Name == "Toughened").Source);
        Assert.Equal("Realms of Terrinoth, с. 91", talents.Single(t => t.Name == "Conduit").Source);
    }

    [Fact]
    public void PublicArtifact_ContainsNoPrivateResources_WhenRequested()
    {
        if (Environment.GetEnvironmentVariable("VERIFY_PUBLIC_ARTIFACT") != "1") return;
        var assembly = typeof(SeedData).Assembly;
        Assert.DoesNotContain(assembly.GetManifestResourceNames(), n => n.Contains("private-content") || n.EndsWith(".ru.json"));
        foreach (var resource in assembly.GetManifestResourceNames().Where(n =>
                     n.EndsWith("talents.catalog.json") || n.EndsWith("heroics.catalog.json")))
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var catalog = JsonDocument.Parse(stream);
            void CheckProse(JsonElement element)
            {
                if (element.ValueKind == JsonValueKind.Array)
                    foreach (var item in element.EnumerateArray()) CheckProse(item);
                if (element.ValueKind != JsonValueKind.Object) return;
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Name is "desc" or "descEn" or "safe" or "notes" or "trigger")
                        Assert.Equal("", property.Value.GetString());
                    else CheckProse(property.Value);
                }
            }
            CheckProse(catalog.RootElement);
        }
        Assert.Empty(PrivateContentStore.Load().Get("rot.heroic.influential") ?? "");
        Assert.All(TalentCatalog.Load(), t => { Assert.Empty(t.SafeDescription); Assert.Empty(t.DescriptionEn); });
        Assert.All(HeroicCatalog.Load(), h =>
        {
            Assert.Empty(h.Description); Assert.Empty(h.SafeDescription); Assert.Empty(h.DescriptionEn);
            Assert.All(h.Upgrades, u => { Assert.Empty(u.Description); Assert.Empty(u.DescriptionEn); });
        });
        Assert.All(HeroicSecondaryEffectCatalog.Load(), e => Assert.Empty(e.Description));
    }

    // Compare every returned structural field, ignoring prose and independently generated identifiers.
    private static string Normalize(object value)
    {
        var node = JsonSerializer.SerializeToNode(value)!;
        var prose = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        { "Description", "SafeDescription", "DescriptionEn", "Trigger", "Notes" };
        void Walk(JsonNode? n)
        {
            if (n is JsonObject o)
                foreach (var key in o.Select(p => p.Key).ToList())
                    if (prose.Contains(key)) o.Remove(key); else Walk(o[key]);
            else if (n is JsonArray a) foreach (var item in a) Walk(item);
        }
        Walk(node);
        return node.ToJsonString();
    }
}
