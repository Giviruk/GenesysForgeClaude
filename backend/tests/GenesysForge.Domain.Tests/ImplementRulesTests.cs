using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-MAG-IMP-01 и ROT-MAG-MAT-01: магические инструменты и их материалы. Проверяется полная
/// контрольная матрица цен и редкостей 6×5, каждая скидка на сложность и границы выбора эффектов.
/// </summary>
public class ImplementRulesTests
{
    private static ImplementSpec Spec(string code) => ImplementRules.For($"rot.item.{code}")!;

    // ── Каталог ──

    [Theory]
    [InlineData("holy-icon", 0, 0, 250, 4)]
    [InlineData("magic-scepter", 2, 1, 350, 5)]
    [InlineData("magic-staff", 4, 2, 400, 6)]
    [InlineData("magic-tome", 0, 1, 750, 7)]
    [InlineData("magic-wand", 3, 1, 400, 7)]
    [InlineData("musical-instrument", 0, 1, 200, 4)]
    public void Catalog_MatchesTheBookTable(
        string code, int damage, int enc, int price, int rarity)
    {
        var spec = Spec(code);
        Assert.Equal(damage, spec.AttackDamageBonus);
        Assert.Equal(enc, spec.Encumbrance);
        Assert.Equal(price, spec.Price);
        Assert.Equal(rarity, spec.Rarity);
    }

    [Fact]
    public void OrdinaryItem_IsNotAnImplement()
    {
        Assert.Null(ImplementRules.For("rot.item.sword"));
        Assert.False(ImplementRules.IsImplement("rot.item.plate"));
        // Обычный посох — оружие, а не магический инструмент: коды разные.
        Assert.False(ImplementRules.IsImplement("rot.item.staff"));
        Assert.True(ImplementRules.IsImplement("rot.item.magic-staff"));
    }

    // ── Материалы: полная контрольная матрица 6×5 ──

    [Theory]
    // Кость
    [InlineData("holy-icon", ImplementMaterial.Bone, 375, 6)]
    [InlineData("magic-scepter", ImplementMaterial.Bone, 525, 7)]
    [InlineData("magic-staff", ImplementMaterial.Bone, 600, 8)]
    [InlineData("magic-tome", ImplementMaterial.Bone, 1125, 9)]
    [InlineData("magic-wand", ImplementMaterial.Bone, 600, 9)]
    [InlineData("musical-instrument", ImplementMaterial.Bone, 300, 6)]
    // Дуб
    [InlineData("holy-icon", ImplementMaterial.Oak, 250, 4)]
    [InlineData("magic-scepter", ImplementMaterial.Oak, 350, 5)]
    [InlineData("magic-staff", ImplementMaterial.Oak, 400, 6)]
    [InlineData("magic-tome", ImplementMaterial.Oak, 750, 7)]
    [InlineData("magic-wand", ImplementMaterial.Oak, 400, 7)]
    [InlineData("musical-instrument", ImplementMaterial.Oak, 200, 4)]
    // Орешник
    [InlineData("holy-icon", ImplementMaterial.Hazel, 375, 5)]
    [InlineData("magic-scepter", ImplementMaterial.Hazel, 525, 6)]
    [InlineData("magic-staff", ImplementMaterial.Hazel, 600, 7)]
    [InlineData("magic-tome", ImplementMaterial.Hazel, 1125, 8)]
    [InlineData("magic-wand", ImplementMaterial.Hazel, 600, 8)]
    [InlineData("musical-instrument", ImplementMaterial.Hazel, 300, 5)]
    // Ива
    [InlineData("holy-icon", ImplementMaterial.Willow, 500, 6)]
    [InlineData("magic-scepter", ImplementMaterial.Willow, 700, 7)]
    [InlineData("magic-staff", ImplementMaterial.Willow, 800, 8)]
    [InlineData("magic-tome", ImplementMaterial.Willow, 1500, 9)]
    [InlineData("magic-wand", ImplementMaterial.Willow, 800, 9)]
    [InlineData("musical-instrument", ImplementMaterial.Willow, 400, 6)]
    // Тис
    [InlineData("holy-icon", ImplementMaterial.Yew, 375, 5)]
    [InlineData("magic-scepter", ImplementMaterial.Yew, 525, 6)]
    [InlineData("magic-staff", ImplementMaterial.Yew, 600, 7)]
    [InlineData("magic-tome", ImplementMaterial.Yew, 1125, 8)]
    [InlineData("magic-wand", ImplementMaterial.Yew, 600, 8)]
    [InlineData("musical-instrument", ImplementMaterial.Yew, 300, 5)]
    public void MaterialMatrix_MatchesTheErrataTable(
        string code, ImplementMaterial material, int price, int rarity)
    {
        var spec = Spec(code);
        Assert.Equal(price, ImplementRules.Price(spec.Price, material));
        Assert.Equal(rarity, ImplementRules.Rarity(spec.Rarity, material));
    }

    /// <summary>
    /// Кость, орешник и тис дорожают в полтора раза по официальной errata. Печатное «вдвое дешевле»
    /// сделало бы редкий материал дешевле дуба — именно эту ошибку errata и правит.
    /// </summary>
    [Theory]
    [InlineData(ImplementMaterial.Bone)]
    [InlineData(ImplementMaterial.Hazel)]
    [InlineData(ImplementMaterial.Yew)]
    public void ErrataMaterials_CostHalfAgain_NotHalf(ImplementMaterial material)
    {
        Assert.Equal(1.5m, ImplementRules.PriceMultiplier(material));
        Assert.True(ImplementRules.Price(400, material) > 400);
    }

    [Fact]
    public void FractionalPrice_RoundsUpToAWholeCoin() =>
        Assert.Equal(38, ImplementRules.Price(25, ImplementMaterial.Bone));

    [Fact]
    public void Rarity_IsClampedToTen() =>
        Assert.Equal(10, ImplementRules.Rarity(9, ImplementMaterial.Bone));

    [Fact]
    public void Material_OnAnOrdinaryItem_IsRejected()
    {
        var error = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureApplicable("rot.item.sword", ImplementMaterial.Willow));
        Assert.Equal("implement.material.not_applicable", error.ReasonCode);
        // Дуб ничего не меняет, поэтому обычная вещь «дубовой» быть может — это её значение по умолчанию.
        ImplementRules.EnsureApplicable("rot.item.sword", ImplementMaterial.Oak);
    }

    // ── Скидки на сложность ──

    private static readonly SpellEffectInput Range = new("Range", 1);
    private static readonly SpellEffectInput CloseCombat = new("Close Combat", 1);
    private static readonly SpellEffectInput AdditionalTarget = new("Additional Target", 1);
    private static readonly SpellEffectInput Empowered = new("Empowered", 2);
    private static readonly SpellEffectInput Sanctuary = new("Sanctuary", 2, "Divine");

    [Fact]
    public void WithoutAnImplement_DifficultyIsTheSumOfIncreases()
    {
        var result = ImplementRules.Difficulty(1, [Range, Empowered]);
        Assert.Equal(4, result.Raw);
        Assert.Equal(4, result.Effective);
        Assert.Empty(result.Discounts);
    }

    [Fact]
    public void Staff_MakesTheFirstRangeFree_ButNotTheSecond()
    {
        var result = ImplementRules.Difficulty(1, [Range, Range], Spec("magic-staff"), "Arcana");
        Assert.Equal(3, result.Raw);
        Assert.Equal(2, result.Effective);
        Assert.Single(result.Discounts);
    }

    [Fact]
    public void Scepter_MakesCloseCombatFree_AndAddsOneBoost()
    {
        var result = ImplementRules.Difficulty(1, [CloseCombat], Spec("magic-scepter"), "Arcana");
        Assert.Equal(1, result.Effective);
        Assert.Equal(1, result.BoostDice);
    }

    [Fact]
    public void MusicalInstrument_WorksOnlyWithVerse()
    {
        var spec = Spec("musical-instrument");
        Assert.Equal(1, ImplementRules.Difficulty(1, [AdditionalTarget], spec, "Verse").Effective);
        // Тот же инструмент в руках чародея не делает ничего.
        Assert.Equal(2, ImplementRules.Difficulty(1, [AdditionalTarget], spec, "Arcana").Effective);
    }

    /// <summary>Икона удешевляет, но не обнуляет: эффект за +2 всё ещё стоит +1.</summary>
    [Fact]
    public void HolyIcon_TakesOneOffEveryDivineOnlyEffect()
    {
        var result = ImplementRules.Difficulty(1, [Sanctuary, Range], Spec("holy-icon"), "Divine");
        Assert.Equal(4, result.Raw);
        Assert.Equal(3, result.Effective);
        Assert.Equal("Sanctuary", Assert.Single(result.Discounts).EffectCode);
    }

    [Fact]
    public void Tome_MakesItsTwoChosenEffectsFree()
    {
        var result = ImplementRules.Difficulty(
            2, [Range, AdditionalTarget, Empowered], Spec("magic-tome"), "Arcana",
            configured: ["Range", "Additional Target"]);
        Assert.Equal(6, result.Raw);
        Assert.Equal(4, result.Effective);
        Assert.Equal(2, result.Discounts.Count);
    }

    /// <summary>
    /// Скидка даётся один раз на эффект: у фолианта с выбранной Дистанцией бесплатно только
    /// первое добавление, второе стоит полную надбавку — как и у посоха.
    /// </summary>
    [Fact]
    public void ChosenEffect_IsFreeOnlyOnItsFirstApplication()
    {
        var result = ImplementRules.Difficulty(
            1, [Range, Range, Range], Spec("magic-tome"), "Arcana", configured: ["Range"]);
        Assert.Equal(4, result.Raw);
        Assert.Equal(3, result.Effective);
        Assert.Single(result.Discounts);
    }

    [Fact]
    public void NamedEffect_IsAlsoFreeOnlyOnce()
    {
        var result = ImplementRules.Difficulty(
            1, [CloseCombat, CloseCombat], Spec("magic-scepter"), "Arcana");
        Assert.Equal(3, result.Raw);
        Assert.Equal(2, result.Effective);
    }

    [Fact]
    public void PendingConfiguration_GivesNoFreeEffect()
    {
        var result = ImplementRules.Difficulty(
            1, [Range], Spec("magic-wand"), "Arcana", configured: ["Range"], pending: true);
        Assert.Equal(2, result.Effective);
        Assert.Empty(result.Discounts);
    }

    /// <summary>Инструмент удешевляет добавки, а не само действие: ниже базовой сложности не опускается.</summary>
    [Fact]
    public void Difficulty_NeverDropsBelowTheActionsBaseDifficulty()
    {
        var result = ImplementRules.Difficulty(2, [CloseCombat], Spec("magic-scepter"), "Arcana");
        Assert.Equal(2, result.Effective);
    }

    // ── Выбор эффектов у экземпляра ──

    [Fact]
    public void Wand_TakesExactlyOneEffectWithAPlusOneIncrease()
    {
        var wand = Spec("magic-wand");
        ImplementRules.EnsureConfigurationValid(wand, [Range]);

        var tooMany = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureConfigurationValid(wand, [Range, CloseCombat]));
        Assert.Equal("implement.choices.too_many", tooMany.ReasonCode);

        var wrongCost = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureConfigurationValid(wand, [Empowered]));
        Assert.Equal("implement.choices.increase_mismatch", wrongCost.ReasonCode);
    }

    [Fact]
    public void Tome_BudgetOfThree_IsAGmRecommendation_NotAHardBan()
    {
        var tome = Spec("magic-tome");
        ImplementRules.EnsureConfigurationValid(tome, [Range, Empowered]); // 1 + 2 = 3, ровно бюджет

        var over = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureConfigurationValid(tome, [Empowered, Empowered with { Code = "Poisonous" }]));
        Assert.Equal("implement.choices.budget_exceeded", over.ReasonCode);

        // С явной причиной ведущий может выйти за рекомендацию книги.
        ImplementRules.EnsureConfigurationValid(
            tome, [Empowered, Empowered with { Code = "Poisonous" }], "решение ведущего");
    }

    [Fact]
    public void Configuration_RejectsDuplicatesAndNonConfigurableImplements()
    {
        var duplicate = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureConfigurationValid(Spec("magic-tome"), [Range, Range]));
        Assert.Equal("implement.choices.duplicate", duplicate.ReasonCode);

        var notApplicable = Assert.Throws<DomainRuleException>(
            () => ImplementRules.EnsureConfigurationValid(Spec("magic-staff"), [Range]));
        Assert.Equal("implement.configuration.not_applicable", notApplicable.ReasonCode);
    }
}
