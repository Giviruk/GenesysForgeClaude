using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

/// <summary>ROT-CRE-01: единый резолвер карьерных навыков и предел стартовых рангов.</summary>
public class CareerSkillResolverTests
{
    private static readonly Guid Discipline = Guid.NewGuid();
    private static readonly Guid Forbidden = Guid.NewGuid();
    private static readonly Guid Divine = Guid.NewGuid();

    private static Guid? Lookup(string name) => name switch
    {
        "Discipline" => Discipline,
        "Knowledge (Forbidden)" => Forbidden,
        "Divine" => Divine,
        _ => null,
    };

    [Fact]
    public void Union_OfCareerSpeciesAndTalent_IsCareer()
    {
        var resolution = CareerSkillResolver.Resolve(
        [
            new CareerSkillGrant("Discipline", CareerSkillGrantSource.Career, "Mage"),
            new CareerSkillGrant("Knowledge (Forbidden)", CareerSkillGrantSource.Species, "Deep Elf"),
            new CareerSkillGrant("Divine", CareerSkillGrantSource.Talent, "Templar"),
        ], Lookup);

        Assert.True(resolution.IsCareer(Discipline));
        Assert.True(resolution.IsCareer(Forbidden));
        Assert.True(resolution.IsCareer(Divine));
        Assert.Equal(3, resolution.SkillDefIds.Count);
    }

    [Fact]
    public void SameSkillFromCareerAndSpecies_IsNotDuplicated_ButKeepsBothSources()
    {
        var resolution = CareerSkillResolver.Resolve(
        [
            new CareerSkillGrant("Divine", CareerSkillGrantSource.Career, "Disciple"),
            new CareerSkillGrant("Divine", CareerSkillGrantSource.Species, "Highborn Elf"),
        ], Lookup);

        Assert.Single(resolution.SkillDefIds);
        var grants = resolution.GrantsFor(Divine);
        Assert.Equal(2, grants.Count);
        Assert.Contains(grants, g => g.Source == CareerSkillGrantSource.Career);
        Assert.Contains(grants, g => g.Source == CareerSkillGrantSource.Species);
    }

    [Fact]
    public void RepeatedIdenticalGrant_IsCollapsed()
    {
        var resolution = CareerSkillResolver.Resolve(
        [
            new CareerSkillGrant("Divine", CareerSkillGrantSource.Career, "Disciple"),
            new CareerSkillGrant("Divine", CareerSkillGrantSource.Career, "Disciple"),
        ], Lookup);

        Assert.Single(resolution.GrantsFor(Divine));
    }

    [Fact]
    public void UnknownSkillName_IsReportedNotSilentlyDropped()
    {
        var resolution = CareerSkillResolver.Resolve(
            [new CareerSkillGrant("Gunnery", CareerSkillGrantSource.Career, "Soldier")], Lookup);

        Assert.Empty(resolution.SkillDefIds);
        Assert.Equal(["Gunnery"], resolution.UnresolvedSkillNames);
    }

    [Fact]
    public void Plan_SumsFreeRanksAcrossSources()
    {
        var plan = new CreationSkillPlan();
        plan.AddFreeRanks(Divine, "Divine", 1, "вид Highborn Elf");
        plan.AddFreeRanks(Divine, "Divine", 1, "карьера Disciple");

        Assert.Equal(2, plan.RanksOf(Divine));
        Assert.Empty(plan.Validate());
    }

    [Fact]
    public void Plan_RejectsRankAboveCreationCap_AndNamesEverySource()
    {
        // Deep Elf уже получает Knowledge (Forbidden) 2 от вида; ещё один бесплатный ранг даёт 3.
        var plan = new CreationSkillPlan();
        plan.AddFreeRanks(Forbidden, "Knowledge (Forbidden)", 2, "вид Deep Elf");
        plan.AddFreeRanks(Forbidden, "Knowledge (Forbidden)", 1, "карьера Scholar");

        var violations = plan.Validate();
        var violation = Assert.Single(violations);
        Assert.Equal(3, violation.TotalRanks);
        Assert.Equal(2, violation.Grants.Count);
        Assert.Contains("вид Deep Elf", violation.Describe());
        Assert.Contains("карьера Scholar", violation.Describe());
    }

    [Fact]
    public void Plan_MarkCareerAlone_AddsNoRanks()
    {
        var plan = new CreationSkillPlan();
        plan.MarkCareer(Discipline, "Discipline");

        var entry = Assert.Single(plan.Entries);
        Assert.True(entry.IsCareer);
        Assert.Equal(0, entry.TotalRanks);
        Assert.Empty(plan.Validate());
    }
}
