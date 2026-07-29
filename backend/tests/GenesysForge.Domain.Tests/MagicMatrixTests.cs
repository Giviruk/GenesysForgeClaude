using GenesysForge.Domain;
using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>
/// ROT-MAG-01: структурная доступность магических действий и дополнительных эффектов. Проверяется
/// вся матрица 8×5 целиком (обе половины: и что доступно, и что нет), девять исключений книги,
/// повторяемость по паре «действие + эффект», несочетаемость и способ применения.
/// </summary>
public class MagicMatrixTests
{
    /// <summary>
    /// Контрольная таблица задачи: строка на действие, крестики — направления, которым оно доступно.
    /// Живёт в тесте отдельной копией специально: если правило подправят, тест должен спорить.
    /// </summary>
    public static TheoryData<string, string[]> Matrix() => new()
    {
        { "Attack", ["Arcana", "Divine", "Primal", "Runes"] },
        { "Augment", ["Divine", "Primal", "Runes", "Verse"] },
        { "Barrier", ["Arcana", "Divine", "Runes"] },
        { "Conjure", ["Arcana", "Primal"] },
        { "Curse", ["Arcana", "Divine", "Runes", "Verse"] },
        { "Dispel", ["Arcana", "Verse"] },
        { "Heal", ["Divine", "Primal", "Verse"] },
        { "Utility", ["Arcana", "Divine", "Primal", "Runes", "Verse"] },
    };

    [Theory]
    [MemberData(nameof(Matrix))]
    public void NativeActions_MatchTheWholeTable(string action, string[] expected)
    {
        Assert.Equal(expected, MagicMatrix.SkillsForAction(action));

        // Обратная половина таблицы: каждое «нет» тоже проверяется, иначе матрица держится
        // на одном списке и молча разъедется.
        foreach (var skill in MagicMatrix.AllSkills)
            Assert.Equal(expected.Contains(skill), MagicMatrix.IsActionAvailable(skill, action));
    }

    [Fact]
    public void NativeActions_AreExactlyTheEightOfTheTable()
    {
        Assert.Equal(
            ["Attack", "Augment", "Barrier", "Conjure", "Curse", "Dispel", "Heal", "Utility"],
            MagicMatrix.NativeActions.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("Mask")]
    [InlineData("Predict")]
    [InlineData("Transform")]
    public void EpgActions_AreOptional_AndDoNotJoinTheNativeMatrix(string action)
    {
        Assert.True(MagicMatrix.IsOptionalAction(action));
        Assert.DoesNotContain(action, MagicMatrix.NativeActions);
        Assert.NotEmpty(MagicMatrix.SkillsForAction(action));
    }

    // ── Девять исключений книги ──

    public static TheoryData<string, string, string> Restrictions() => new()
    {
        { "Attack", "Manipulative", "Arcana" },
        { "Attack", "Non-Lethal", "Primal" },
        { "Attack", "Holy/Unholy", "Divine" },
        { "Barrier", "Reflective", "Arcana" },
        { "Barrier", "Sanctuary", "Divine" },
        { "Augment", "Divine Health", "Divine" },
        { "Augment", "Primal Fury", "Primal" },
        { "Curse", "Despair", "Divine" },
        { "Curse", "Doom", "Arcana" },
    };

    [Theory]
    [MemberData(nameof(Restrictions))]
    public void RestrictedEffects_BelongToOneSkillOnly(string action, string effect, string only)
    {
        Assert.Equal(only, MagicMatrix.RestrictedSkill(action, effect));
        Assert.Equal([only], MagicMatrix.SkillsForEffect(action, effect));

        foreach (var skill in MagicMatrix.AllSkills)
            Assert.Equal(skill == only, MagicMatrix.IsEffectAvailable(skill, action, effect));
    }

    [Fact]
    public void UnrestrictedEffect_InheritsTheAvailabilityOfItsAction()
    {
        // «Взрывной» ограничений не имеет: он доступен ровно тем, кому доступна Атака.
        Assert.Equal(MagicMatrix.SkillsForAction("Attack"), MagicMatrix.SkillsForEffect("Attack", "Blast"));
        Assert.True(MagicMatrix.IsEffectAvailable("Runes", "Attack", "Blast"));
        Assert.False(MagicMatrix.IsEffectAvailable("Verse", "Attack", "Blast"));
    }

    [Fact]
    public void RestrictionNeverWidensTheAction()
    {
        // Проклятье недоступно Природе, и «Рок» её доступным не делает.
        Assert.False(MagicMatrix.IsEffectAvailable("Primal", "Curse", "Doom"));
        // Divine умеет Проклятье, но не эту его разновидность.
        Assert.True(MagicMatrix.IsActionAvailable("Divine", "Curse"));
        Assert.False(MagicMatrix.IsEffectAvailable("Divine", "Curse", "Doom"));
    }

    // ── Повторяемость ──

    [Theory]
    [InlineData("Attack", "Range")]
    [InlineData("Heal", "Range")]
    [InlineData("Mask", "Size")]
    [InlineData("Transform", "Silhouette Increase")]
    public void RepeatableEffects_AreMarkedExplicitly(string action, string effect) =>
        Assert.True(MagicMatrix.IsRepeatable(action, effect));

    [Theory]
    [InlineData("Attack", "Blast")]
    [InlineData("Attack", "Deadly")]
    [InlineData("Curse", "Paralyzed")]
    [InlineData("Augment", "Haste")]
    [InlineData("Barrier", "Additional Target")]
    public void OtherEffects_AreNotRepeatable(string action, string effect) =>
        Assert.False(MagicMatrix.IsRepeatable(action, effect));

    [Fact]
    public void Repeatability_DoesNotTravelBetweenActionsByName()
    {
        // «Размер» повторяем у иллюзии EPG и не становится повторяемым у чужого действия
        // с таким же именем эффекта.
        Assert.True(MagicMatrix.IsRepeatable("Mask", "Size"));
        Assert.False(MagicMatrix.IsRepeatable("Attack", "Size"));
        Assert.False(MagicMatrix.IsRepeatable("Transform", "Size"));
    }

    // ── Несочетаемость ──

    [Fact]
    public void Exclusions_AreSymmetric()
    {
        Assert.Contains("Additional Target", MagicMatrix.ConflictsFor("Curse", "Despair"));
        Assert.Contains("Additional Target", MagicMatrix.ConflictsFor("Curse", "Paralyzed"));

        var fromTheOtherSide = MagicMatrix.ConflictsFor("Curse", "Additional Target");
        Assert.Contains("Despair", fromTheOtherSide);
        Assert.Contains("Paralyzed", fromTheOtherSide);
    }

    [Fact]
    public void EffectWithoutConflicts_HasAnEmptyList() =>
        Assert.Empty(MagicMatrix.ConflictsFor("Curse", "Misfortune"));

    // ── Способ применения ──

    [Theory]
    [InlineData("Attack", "Fire", SpellResolutionKind.ActivatedQuality)]
    [InlineData("Attack", "Non-Lethal", SpellResolutionKind.PassiveQuality)]
    [InlineData("Attack", "Manipulative", SpellResolutionKind.AdvantageSpend)]
    [InlineData("Attack", "Range", SpellResolutionKind.Parameter)]
    [InlineData("Predict", "Cheat Death", SpellResolutionKind.StoryPoint)]
    [InlineData("Attack", "Empowered", SpellResolutionKind.OnSuccess)]
    public void Resolution_IsAField_NotASentenceInTheDescription(
        string action, string effect, SpellResolutionKind expected) =>
        Assert.Equal(expected, MagicMatrix.ResolutionFor(action, effect));

    [Fact]
    public void Actions_HaveTheirOwnResolution()
    {
        Assert.Equal(SpellResolutionKind.Narrative, MagicMatrix.ResolutionFor("Utility"));
        Assert.Equal(SpellResolutionKind.OnSuccess, MagicMatrix.ResolutionFor("Attack"));
    }

    // ── Рейтинги по Знанию (ROT-MAG-10) ──

    [Theory]
    [InlineData("Attack", "Fire", "Burn")]
    [InlineData("Attack", "Ice", "Ensnare")]
    [InlineData("Attack", "Blast", "Blast")]
    [InlineData("Attack", "Lightning", "Stun")]
    [InlineData("Attack", "Deadly", "Vicious")]
    [InlineData("Attack", "Impact", "Disorient")]
    [InlineData("Attack", "Destructive", "Pierce")]
    public void RatedQualities_AreListedExplicitly(string action, string effect, string quality)
    {
        Assert.True(MagicMatrix.UsesKnowledgeRating(action, effect));
        Assert.Equal([quality], MagicMatrix.KnowledgeRatedQualities(action, effect));
    }

    [Fact]
    public void QualitiesWithoutARating_NeverGetOne()
    {
        // «Повреждение N» и «Нокдаун N» не существуют: рейтинг получает только вторая половина пары.
        Assert.DoesNotContain("Sunder", MagicMatrix.KnowledgeRatedQualities("Attack", "Destructive"));
        Assert.DoesNotContain("Knockdown", MagicMatrix.KnowledgeRatedQualities("Attack", "Impact"));
        Assert.DoesNotContain("Auto-fire", MagicMatrix.KnowledgeRatedQualities("Attack", "Lightning"));
    }

    [Theory]
    [InlineData("Attack", "Poisonous")]
    [InlineData("Barrier", "Add Defense")]
    [InlineData("Curse", "Despair")]
    [InlineData("Augment", "Divine Health")]
    [InlineData("Augment", "Primal Fury")]
    public void NumericEffects_UseTheRating_WithoutGrantingAQuality(string action, string effect)
    {
        Assert.True(MagicMatrix.UsesKnowledgeRating(action, effect));
        Assert.Empty(MagicMatrix.KnowledgeRatedQualities(action, effect));
    }

    [Theory]
    [InlineData("Attack", "Range")]
    [InlineData("Attack", "Holy/Unholy")]
    [InlineData("Curse", "Misfortune")]
    [InlineData("Heal", "Restoration")]
    public void UnrelatedEffects_DoNotDependOnKnowledge(string action, string effect)
    {
        Assert.False(MagicMatrix.UsesKnowledgeRating(action, effect));
        Assert.Empty(MagicMatrix.KnowledgeRatedQualities(action, effect));
    }

    [Fact]
    public void UnknownAction_IsAvailableToNobody_AndDoesNotThrow()
    {
        Assert.Empty(MagicMatrix.SkillsForAction("Necromancy"));
        Assert.False(MagicMatrix.IsActionAvailable("Arcana", "Necromancy"));
        Assert.Equal(SpellResolutionKind.OnSuccess, MagicMatrix.ResolutionFor("Necromancy"));
    }
}
