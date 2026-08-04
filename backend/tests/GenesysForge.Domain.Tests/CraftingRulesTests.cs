using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-CRAFT-01 и ROT-ALCH-02: сложность, время, стоимость компонентов и распределение символов.
/// </summary>
public class CraftingRulesTests
{
    [Theory]
    [InlineData(0, 0)]  // Простая
    [InlineData(1, 1)]  // Лёгкая
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(5, 3)]
    [InlineData(10, 5)] // Грозная
    public void Difficulty_IsHalfRarityRoundedUp(int rarity, int expected) =>
        Assert.Equal(expected, CraftingRules.Difficulty(rarity));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(10, 11)]
    public void BaseTime_IsOnePlusRarity(int rarity, int expected) =>
        Assert.Equal(expected, CraftingRules.BaseTime(rarity));

    /// <summary>Половина цены округляется **вверх** — это отдельное решение ТЗ, а не как у выручки.</summary>
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(25, 13)]
    [InlineData(250, 125)]
    [InlineData(999, 500)]
    public void ComponentCost_IsHalfPriceRoundedUp(int price, int expected) =>
        Assert.Equal(expected, CraftingRules.ComponentCost(price));

    [Theory]
    [InlineData(100, 100, 100)]
    [InlineData(100, 50, 50)]
    [InlineData(100, 200, 200)]
    [InlineData(125, 150, 187)] // доля округляется вниз, как при покупке
    public void Cost_AppliesPercentLikePurchase(int listed, int percent, int expected) =>
        Assert.Equal(expected, CraftingRules.Cost(listed, percent, null, null));

    [Fact]
    public void Cost_RejectsPercentOutsideRangeAndOffStep()
    {
        Assert.Equal("trade.percent_invalid",
            Assert.Throws<DomainRuleException>(() => CraftingRules.Cost(100, 300, null, null)).ReasonCode);
        Assert.Equal("trade.percent_step_invalid",
            Assert.Throws<DomainRuleException>(() => CraftingRules.Cost(100, 60, null, null)).ReasonCode);
    }

    [Fact]
    public void Cost_OwnPriceReplacesPercentAndNeedsReason()
    {
        Assert.Equal(7, CraftingRules.Cost(100, 100, 7, "договорился с гильдией"));
        Assert.Equal("crafting.cost_reason_required",
            Assert.Throws<DomainRuleException>(() => CraftingRules.Cost(100, 100, 7, " ")).ReasonCode);
    }

    // ─────────────────────────── распределение символов ───────────────────────────

    private static Dictionary<string, CraftingSpendDef> Catalog(params CraftingSpendDef[] defs) =>
        defs.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

    private static CraftingSpendDef Spend(
        string code, string row = "r1", int adv = 0, int thr = 0, int tri = 0, int desp = 0,
        bool repeatable = false, CraftingSpendEffect effect = CraftingSpendEffect.Descriptive,
        int value = 0, string quality = "", bool weaponOnly = false, bool param = false) => new()
    {
        Code = code, RowCode = row, NameRu = code, Name = code,
        AdvantageCost = adv, ThreatCost = thr, TriumphCost = tri, DespairCost = desp,
        Repeatable = repeatable, Effect = effect, Value = value, Quality = quality,
        WeaponOnly = weaponOnly, RequiresParameter = param,
    };

    [Fact]
    public void Allocate_SuccessCreatesOneInstance_FailureCreatesNone()
    {
        var catalog = Catalog();
        Assert.Equal(1, CraftingRules.Allocate([], catalog, 0, 0, 0, 0, 3, false, success: true).Quantity);
        Assert.Equal(0, CraftingRules.Allocate([], catalog, 0, 0, 0, 0, 3, false, success: false).Quantity);
    }

    [Fact]
    public void Allocate_TimeNeverDropsBelowOne()
    {
        var catalog = Catalog(Spend("faster", adv: 1, repeatable: true, effect: CraftingSpendEffect.Time, value: -1));
        var outcome = CraftingRules.Allocate(
            [new CraftingSpendChoice("faster", 5, "", "advantage")], catalog, 5, 0, 0, 0, 3, false, true);
        Assert.Equal(1, outcome.Time);
    }

    [Fact]
    public void Allocate_HalvedTimeNeverDropsBelowOne()
    {
        var catalog = Catalog(Spend("half", adv: 2, effect: CraftingSpendEffect.TimeHalved));
        Assert.Equal(4, CraftingRules.Allocate(
            [new CraftingSpendChoice("half", 1, "", "advantage")], catalog, 2, 0, 0, 0, 7, false, true).Time);
        Assert.Equal(1, CraftingRules.Allocate(
            [new CraftingSpendChoice("half", 1, "", "advantage")], catalog, 2, 0, 0, 0, 1, false, true).Time);
    }

    /// <summary>Бюджет символов не резиновый: на что не хватило, то не тратится.</summary>
    [Fact]
    public void Allocate_RejectsSpendingMoreSymbolsThanRolled()
    {
        var catalog = Catalog(Spend("enc", adv: 2, effect: CraftingSpendEffect.Encumbrance, value: -1));
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            [new CraftingSpendChoice("enc", 1, "", "advantage")], catalog, 1, 0, 0, 0, 3, false, true));
        Assert.Equal("crafting.spend_budget", ex.ReasonCode);
    }

    [Fact]
    public void Allocate_RejectsRepeatOfNonRepeatableSpend()
    {
        var catalog = Catalog(Spend("hp", adv: 3, effect: CraftingSpendEffect.HardPoints, value: 1));
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            [new CraftingSpendChoice("hp", 2, "", "advantage")], catalog, 9, 0, 0, 0, 3, false, true));
        Assert.Equal("crafting.spend_not_repeatable", ex.ReasonCode);
    }

    /// <summary>За одну цену берут один эффект строки, а не всю строку целиком.</summary>
    [Fact]
    public void Allocate_RejectsTwoEffectsFromTheSameRow()
    {
        var catalog = Catalog(
            Spend("a", row: "craft-2", adv: 2, effect: CraftingSpendEffect.Encumbrance, value: -1),
            Spend("b", row: "craft-2", adv: 2, effect: CraftingSpendEffect.HardPoints, value: 1));
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            [new CraftingSpendChoice("a", 1, "", "advantage"), new CraftingSpendChoice("b", 1, "", "advantage")],
            catalog, 8, 0, 0, 0, 3, false, true));
        Assert.Equal("crafting.spend_row_conflict", ex.ReasonCode);
    }

    [Fact]
    public void Allocate_TriumphPaysTheSameRowWhenTableSaysSo()
    {
        var catalog = Catalog(Spend("enc", adv: 2, tri: 1, effect: CraftingSpendEffect.Encumbrance, value: -1));
        var outcome = CraftingRules.Allocate(
            [new CraftingSpendChoice("enc", 1, "", "triumph")], catalog, 0, 0, 1, 0, 3, false, true);
        Assert.Equal(-1, outcome.EncumbranceDelta);
    }

    [Fact]
    public void Allocate_RejectsPaymentBySymbolTheRowDoesNotAccept()
    {
        var catalog = Catalog(Spend("enc", adv: 2, effect: CraftingSpendEffect.Encumbrance, value: -1));
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            [new CraftingSpendChoice("enc", 1, "", "despair")], catalog, 0, 0, 0, 5, 3, false, true));
        Assert.Equal("crafting.spend_payment_invalid", ex.ReasonCode);
    }

    [Fact]
    public void Allocate_InaccurateIsWeaponOnly()
    {
        var catalog = Catalog(Spend("inacc", thr: 3, effect: CraftingSpendEffect.AddQuality,
            quality: "inaccurate", value: 1, weaponOnly: true));
        var choice = new[] { new CraftingSpendChoice("inacc", 1, "", "threat") };
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            choice, catalog, 0, 3, 0, 0, 3, isWeapon: false, success: true));
        Assert.Equal("crafting.spend_weapon_only", ex.ReasonCode);

        var ok = CraftingRules.Allocate(choice, catalog, 0, 3, 0, 0, 3, isWeapon: true, success: true);
        Assert.Contains(ok.Qualities, q => q.Code == "inaccurate" && q.Rating == 1);
    }

    [Fact]
    public void Allocate_QualityRatingNeedsAChoiceAndRefusesDamageAndSoak()
    {
        var catalog = Catalog(Spend("rating", tri: 1, param: true,
            effect: CraftingSpendEffect.QualityRating, value: 1));
        Assert.Equal("crafting.spend_parameter_required",
            Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
                [new CraftingSpendChoice("rating", 1, "", "triumph")], catalog, 0, 0, 1, 0, 3, false, true))
            .ReasonCode);
        Assert.Equal("crafting.spend_rating_forbidden",
            Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
                [new CraftingSpendChoice("rating", 1, "breach", "triumph")], catalog, 0, 0, 1, 0, 3, false, true))
            .ReasonCode);
    }

    [Fact]
    public void Allocate_ExtraCopiesOnlyAppearOnSuccess()
    {
        var catalog = Catalog(Spend("copy", adv: 2, repeatable: true,
            effect: CraftingSpendEffect.ExtraQuantity, value: 1));
        var choice = new[] { new CraftingSpendChoice("copy", 2, "", "advantage") };
        Assert.Equal(3, CraftingRules.Allocate(choice, catalog, 4, 0, 0, 0, 3, false, success: true).Quantity);
        Assert.Equal(0, CraftingRules.Allocate(choice, catalog, 4, 0, 0, 0, 3, false, success: false).Quantity);
    }

    [Fact]
    public void Allocate_EveryChoiceLandsInTheNotes()
    {
        var catalog = Catalog(
            Spend("superior", tri: 1, effect: CraftingSpendEffect.AddQuality, quality: "superior"),
            Spend("slow", row: "r2", thr: 1, repeatable: true, effect: CraftingSpendEffect.Time, value: 1));
        var outcome = CraftingRules.Allocate(
            [new CraftingSpendChoice("superior", 1, "", "triumph"),
             new CraftingSpendChoice("slow", 2, "", "threat")],
            catalog, 0, 2, 1, 0, 3, false, true);
        Assert.Equal(2, outcome.Notes.Count);
        Assert.Contains("slow ×2", outcome.Notes);
        Assert.Equal(5, outcome.Time);
    }

    [Fact]
    public void Allocate_RejectsUnknownSpendCode()
    {
        var ex = Assert.Throws<DomainRuleException>(() => CraftingRules.Allocate(
            [new CraftingSpendChoice("nope", 1, "", "advantage")], Catalog(), 5, 0, 0, 0, 3, false, true));
        Assert.Equal("crafting.spend_unknown", ex.ReasonCode);
    }

    // ─────────────────────────── качества экземпляра ───────────────────────────

    [Fact]
    public void PackedQualities_RoundTrip()
    {
        var packed = CraftingRules.PackQualities(
            [new EffectiveQuality("superior", 0), new EffectiveQuality("inaccurate", 1)]);
        Assert.Equal("superior,inaccurate:1", packed);
        var back = CraftingRules.UnpackQualities(packed);
        Assert.Equal(2, back.Count);
        Assert.Contains(back, q => q.Code == "inaccurate" && q.Rating == 1);
    }

    [Fact]
    public void UnpackQualities_IgnoresGarbageInsteadOfInventingQualities()
    {
        var back = CraftingRules.UnpackQualities(",, :3 , superior , pierce:x");
        Assert.Equal(2, back.Count);
        Assert.Contains(back, q => q.Code == "superior" && q.Rating == 0);
        Assert.Contains(back, q => q.Code == "pierce" && q.Rating == 0);
    }

    // ─────────────────────────── что вообще можно изготовить ───────────────────────────

    [Fact]
    public void EnsureCraftable_RejectsPricelessRelic()
    {
        var relic = new ItemDef { Name = "Soulbound Sword", NameRu = "Меч", Price = null };
        Assert.Equal("crafting.target_priceless",
            Assert.Throws<DomainRuleException>(() => CraftingRules.EnsureCraftable(relic, CraftingKind.Item))
            .ReasonCode);
        // Зачарование не создаёт запись каталога, поэтому цены цели ему не нужно.
        CraftingRules.EnsureCraftable(relic, CraftingKind.Enchantment);
    }

    [Fact]
    public void EnsureCraftable_MatchesPotionKindToTheRecipeCatalog()
    {
        var potion = new ItemDef { Name = "Stamina Elixir", Code = "stamina-elixir", Price = 50 };
        var axe = new ItemDef { Name = "Axe", Code = "axe", Price = 150 };

        CraftingRules.EnsureCraftable(potion, CraftingKind.Potion);
        CraftingRules.EnsureCraftable(axe, CraftingKind.Item);
        Assert.Equal("crafting.target_not_potion",
            Assert.Throws<DomainRuleException>(() =>
                CraftingRules.EnsureCraftable(axe, CraftingKind.Potion)).ReasonCode);
        Assert.Equal("crafting.target_is_potion",
            Assert.Throws<DomainRuleException>(() =>
                CraftingRules.EnsureCraftable(potion, CraftingKind.Item)).ReasonCode);
    }

    [Fact]
    public void EnsureEnchantable_RequiresSuperiorBase()
    {
        Assert.Equal("crafting.base_not_superior",
            Assert.Throws<DomainRuleException>(() =>
                CraftingRules.EnsureEnchantable([new EffectiveQuality("pierce", 1)])).ReasonCode);
        CraftingRules.EnsureEnchantable([new EffectiveQuality("superior", 0)]);
    }
}
