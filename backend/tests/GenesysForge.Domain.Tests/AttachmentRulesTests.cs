using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-EQP-ATT-01: слоты, совместимость и эффекты улучшений. Проверяются границы слотов,
/// предикат совместимости и то, что «не ниже», «плюс один» и отмена противоположного качества
/// считаются по-разному.
/// </summary>
public class AttachmentRulesTests
{
    private static AttachmentDef Def(
        string code = "test", int hp = 1, ItemKind host = ItemKind.Weapon,
        WeaponFormTraits required = WeaponFormTraits.None,
        WeaponFormTraits requiredAny = WeaponFormTraits.None,
        WeaponFormTraits forbidden = WeaponFormTraits.None,
        bool enchantment = false,
        params AttachmentEffect[] effects) => new()
        {
            Id = Guid.NewGuid(), Code = code, Name = code, NameRu = code,
            HardPointCost = hp, HostKind = host, IsEnchantment = enchantment,
            RequiredTraits = required, RequiredAnyTraits = requiredAny, ForbiddenTraits = forbidden,
            Effects = [.. effects],
        };

    private static AttachmentEffect Effect(
        AttachmentEffectKind kind, string quality = "", int value = 0, int increment = 0,
        string opposite = "", string skill = "",
        AttachmentEffectCondition condition = AttachmentEffectCondition.Always) => new()
        {
            Kind = kind, QualityCode = quality, Value = value, Increment = increment,
            OppositeQualityCode = opposite, SkillName = skill, Condition = condition,
        };

    // ── Слоты ──

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(4, 2)]
    [InlineData(5, 3)]
    public void FallbackHardPoints_IsHalfTheWeightRoundedUp(int encumbrance, int expected) =>
        Assert.Equal(expected, AttachmentRules.FallbackHardPoints(encumbrance));

    [Fact]
    public void BookHardPoints_WinOverTheFallback()
    {
        // У записи с книжным значением вес ни при чём — даже если значение нулевое.
        Assert.Equal(0, AttachmentRules.HardPoints(0, baseEncumbrance: 4));
        Assert.Equal(2, AttachmentRules.HardPoints(null, baseEncumbrance: 4));
    }

    [Fact]
    public void RemainingHardPoints_NeverGoNegative()
    {
        var installed = new[] { Def(hp: 2), Def(code: "other", hp: 2) };
        Assert.Equal(0, AttachmentRules.RemainingHardPoints(1, installed));
        Assert.True(AttachmentRules.IsOverCapacity(1, installed));
        Assert.False(AttachmentRules.IsOverCapacity(4, installed));
    }

    // ── Совместимость ──

    [Fact]
    public void Compatibility_ChecksKindRequiredAnyAndForbidden()
    {
        var razor = Def(required: WeaponFormTraits.Bladed, forbidden: WeaponFormTraits.Ranged);
        Assert.True(AttachmentRules.IsCompatible(
            ItemKind.Weapon, WeaponFormTraits.Bladed | WeaponFormTraits.Sword, razor));
        // Клинковое, но дальнобойное — запрещённый признак перевешивает.
        Assert.False(AttachmentRules.IsCompatible(
            ItemKind.Weapon, WeaponFormTraits.Bladed | WeaponFormTraits.Ranged, razor));
        // Дробящее — нужного признака нет.
        Assert.False(AttachmentRules.IsCompatible(
            ItemKind.Weapon, WeaponFormTraits.BluntOrCrushing, razor));
        // Броня с теми же признаками не подходит оружейному улучшению.
        Assert.False(AttachmentRules.IsCompatible(ItemKind.Armor, WeaponFormTraits.Bladed, razor));
    }

    [Fact]
    public void RequiredAny_NeedsAtLeastOneTrait()
    {
        var hilt = Def(requiredAny: WeaponFormTraits.OneHanded | WeaponFormTraits.Brawl);
        Assert.True(AttachmentRules.IsCompatible(ItemKind.Weapon, WeaponFormTraits.OneHanded, hilt));
        Assert.True(AttachmentRules.IsCompatible(ItemKind.Weapon, WeaponFormTraits.Brawl, hilt));
        Assert.False(AttachmentRules.IsCompatible(ItemKind.Weapon, WeaponFormTraits.TwoHanded, hilt));
    }

    // ── Проверки установки ──

    [Fact]
    public void Install_RejectsIncompatibleHost()
    {
        var ex = Assert.Throws<DomainRuleException>(() => AttachmentRules.EnsureCanInstall(
            ItemKind.Armor, WeaponFormTraits.PlateArmor, 2, [], Def(), true));
        Assert.Equal("attachment.incompatible", ex.ReasonCode);
    }

    [Fact]
    public void Install_RejectsSecondCopyOfTheSameAttachment()
    {
        var def = Def(code: "razor-edge");
        var ex = Assert.Throws<DomainRuleException>(() => AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 4, [def], def, true));
        Assert.Equal("attachment.duplicate", ex.ReasonCode);
    }

    [Fact]
    public void Install_RejectsWhenSlotsRunOut()
    {
        var installed = new[] { Def(code: "a", hp: 1) };
        var ex = Assert.Throws<DomainRuleException>(() => AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 1, installed, Def(code: "b", hp: 1), true));
        Assert.Equal("attachment.no_hard_points", ex.ReasonCode);
    }

    [Fact]
    public void ZeroCostAttachment_FitsEvenWithoutFreeSlots()
    {
        var installed = new[] { Def(code: "a", hp: 2) };
        AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 2, installed, Def(code: "gilded", hp: 0), true);
    }

    [Fact]
    public void Enchantment_RequiresMagicRank_UnlessReasonGiven()
    {
        var rune = Def(code: "rune", enchantment: true);
        var ex = Assert.Throws<DomainRuleException>(() => AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 2, [], rune, installerHasMagicRank: false));
        Assert.Equal("attachment.magic_rank_required", ex.ReasonCode);

        AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 2, [], rune, installerHasMagicRank: true);
        AttachmentRules.EnsureCanInstall(
            ItemKind.Weapon, WeaponFormTraits.None, 2, [], rune, false, "помог городской чародей");
    }

    // ── Эффекты ──

    [Fact]
    public void Aggregate_SumsNumbers_AndKeepsUnexecutableRulesVisible()
    {
        var def = Def(effects:
        [
            Effect(AttachmentEffectKind.Damage, value: 2),
            Effect(AttachmentEffectKind.CritReduction, value: 1),
            Effect(AttachmentEffectKind.Encumbrance, value: 1),
            Effect(AttachmentEffectKind.NarrativeOnly),
        ]);
        var aggregate = AttachmentRules.Aggregate([new AttachmentInput(def, WornAndActive: true)]);

        Assert.Equal(2, aggregate.DamageBonus);
        Assert.Equal(1, aggregate.CritReduction);
        Assert.Equal(1, aggregate.Encumbrance);
        Assert.Single(aggregate.Notes);
    }

    [Fact]
    public void WornOnlyEffects_StaySilentOnUnwornArmor()
    {
        var def = Def(host: ItemKind.Armor, effects:
        [
            Effect(AttachmentEffectKind.RangedDefense, value: 1,
                condition: AttachmentEffectCondition.WornAndActive),
            Effect(AttachmentEffectKind.SkillBoost, value: 2, skill: "Stealth",
                condition: AttachmentEffectCondition.WornAndActive),
        ]);

        var worn = AttachmentRules.Aggregate([new AttachmentInput(def, WornAndActive: true)]);
        Assert.Equal(1, worn.RangedDefense);
        Assert.Single(worn.SkillBoosts);

        var carried = AttachmentRules.Aggregate([new AttachmentInput(def, WornAndActive: false)]);
        Assert.Equal(0, carried.RangedDefense);
        Assert.Empty(carried.SkillBoosts);
    }

    [Fact]
    public void GrantOrIncrease_GrantsFullRating_ThenAddsIncrement()
    {
        var razor = Def(effects: [Effect(AttachmentEffectKind.GrantOrIncreaseQuality,
            quality: "pierce", value: 2, increment: 1)]);
        var input = new[] { new AttachmentInput(razor, true) };

        var granted = AttachmentRules.ApplyQualities([], input);
        Assert.Equal(2, Assert.Single(granted, q => q.Code == "pierce").Rating);

        var increased = AttachmentRules.ApplyQualities([new EffectiveQuality("pierce", 3)], input);
        Assert.Equal(4, Assert.Single(increased, q => q.Code == "pierce").Rating);
    }

    [Fact]
    public void SetAtLeast_DoesNotStack()
    {
        var rune = Def(effects: [Effect(AttachmentEffectKind.SetQualityAtLeast, quality: "vicious", value: 5)]);
        var input = new[] { new AttachmentInput(rune, true) };

        Assert.Equal(5, Assert.Single(AttachmentRules.ApplyQualities([], input)).Rating);
        // Уже более сильное качество руна не усиливает — это «не ниже», а не «плюс пять».
        Assert.Equal(6, Assert.Single(
            AttachmentRules.ApplyQualities([new EffectiveQuality("vicious", 6)], input)).Rating);
    }

    [Fact]
    public void GrantOrCancelOpposite_RemovesTheOpposite_InsteadOfGranting()
    {
        var hilt = Def(effects: [Effect(AttachmentEffectKind.GrantQualityOrCancelOpposite,
            quality: "accurate", opposite: "inaccurate", value: 1)]);
        var input = new[] { new AttachmentInput(hilt, true) };

        // У точного оружия эфес добавляет бонусный куб.
        var granted = AttachmentRules.ApplyQualities([], input);
        Assert.Equal(1, Assert.Single(granted, q => q.Code == "accurate").Rating);

        // У неточного — снимает помеху и Точного не выдаёт.
        var cancelled = AttachmentRules.ApplyQualities([new EffectiveQuality("inaccurate", 2)], input);
        Assert.Equal(1, Assert.Single(cancelled, q => q.Code == "inaccurate").Rating);
        Assert.DoesNotContain(cancelled, q => q.Code == "accurate");

        // Неточное 1 исчезает полностью, а не остаётся нулём.
        var removed = AttachmentRules.ApplyQualities([new EffectiveQuality("inaccurate", 1)], input);
        Assert.Empty(removed);
    }

    [Fact]
    public void BaseQualities_SurviveWithoutAttachments()
    {
        var qualities = AttachmentRules.ApplyQualities([new EffectiveQuality("defensive", 1)], []);
        Assert.Equal(1, Assert.Single(qualities).Rating);
    }
}
