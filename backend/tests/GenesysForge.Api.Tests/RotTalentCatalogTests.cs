using System.Net.Http.Json;
using GenesysForge.Application.Dtos;

namespace GenesysForge.Api.Tests;

/// <summary>
/// ROT-TAL-01: точный состав активного RoT-каталога талантов и исправленная metadata.
/// Проверяются множества кодов и каждое значение таблицы ТЗ, а не только количество.
/// </summary>
public class RotTalentCatalogTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    /// <summary>Ровно столько активных встроенных талантов должно быть в RoT scope.</summary>
    private const int ExpectedActiveRotTalents = 112;

    /// <summary>Исключены из новой покупки в RoT, но остаются доступны в Genesys Core.</summary>
    private static readonly string[] RetiredFromRot =
    [
        "Rapid Reaction", "Surgeon", "Scathing Tirade", "Scathing Tirade (Improved)",
        "Scathing Tirade (Supreme)", "Just in Time!", "Indomitable", "Ruinous Repartee",
        "Attuned", "Counterspell", "Empowered Casting",
    ];

    /// <summary>Пять errata-талантов, которые обязаны остаться в каталоге.</summary>
    private static readonly string[] ErrataTalents =
        ["Second Wind", "Side Step", "Swift", "Toughened", "Unremarkable"];

    private static readonly string[] KnownRussianOcrFragments =
    [
        "сде ланной", "реа гирования", "совер шавших", "лег кодоступное", "дей ствием", "ору жия",
        "навы ков", "провер кам", "ближ нем", "добав ляет", "совер шить", "вме сто", "уби рает",
        "пер сонаж", "использо ванием", "пер сонажа", "про верка", "про верке", "посмо трите",
        "состав ляет", "исполь зовать", "переме ститься", "персо наж", "недо ступна", "веду щим",
        "коли чество", "сту пень", "характе ристики", "Хлёст кая", "Воодушевля ющая",
        "Воо душевляющая", "кри тических", "использую щих", "игнори рует", "одобрен ным",
        "суще ствует", "реше нию", "гру бого", "следую щим", "совер шать", "съе дать",
        "потра тить", "Осо бенности", "при сутствовать", "высту пать", "даль него", "дистан ции",
        "использо вания", "спо собностей", "опре делённое", "слож ность", "уста лости",
        "задействован ное", "сте чением", "кри тическую", "пре делах", "извест ных", "неко торые",
        "туре лью", "увеличива ются", "значе ния", "персо нажа", "увели чить", "харак теристику",
        "преде лах", "выве дена", "отпущен ных", "футуристи ческом",
    ];

    private async Task<List<TalentDefDto>> RotTalentsAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/RealmsOfTerrinoth", Json.Options))!;
        return reference.Talents.Where(t => !t.IsCustom).ToList();
    }

    private async Task<List<TalentDefDto>> CoreTalentsAsync()
    {
        var client = await factory.CreateAuthorizedClientAsync();
        var reference = (await client.GetFromJsonAsync<ReferenceResponse>(
            "/api/reference/GenesysCore", Json.Options))!;
        return reference.Talents.Where(t => !t.IsCustom).ToList();
    }

    [Fact]
    public async Task RotScopeHasExactlyTheExpectedNumberOfActiveTalents()
    {
        Assert.Equal(ExpectedActiveRotTalents, (await RotTalentsAsync()).Count);
    }

    [Fact]
    public async Task TheThreeMissingTalentsAreNowPresent()
    {
        var names = (await RotTalentsAsync()).Select(t => t.Name).ToHashSet();

        Assert.Contains("Challenge!", names);
        Assert.Contains("Let’s Talk This Over", names);
        Assert.Contains("Retribution!", names);
    }

    [Fact]
    public async Task ErrataTalentsAreKept()
    {
        var names = (await RotTalentsAsync()).Select(t => t.Name).ToHashSet();

        foreach (var talent in ErrataTalents) Assert.Contains(talent, names);
    }

    [Fact]
    public async Task TalentsWrongForRot_AreGoneFromRot_ButStayInGenesysCore()
    {
        var rot = (await RotTalentsAsync()).Select(t => t.Name).ToHashSet();
        var core = (await CoreTalentsAsync()).Select(t => t.Name).ToHashSet();

        foreach (var talent in RetiredFromRot)
        {
            Assert.DoesNotContain(talent, rot);
            Assert.Contains(talent, core); // запись из Core/EPG не удаляется глобально
        }
    }

    /// <summary>Таланты, у которых ТЗ исправляет флаг ranked.</summary>
    [Theory]
    [InlineData("Apothecary")]
    [InlineData("Blood Sacrifice")]
    [InlineData("Body Guard")]
    [InlineData("Dungeoneer")]
    [InlineData("Exploit")]
    [InlineData("Threaten")]
    public async Task CorrectedRankedFlags(string name)
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == name);

        Assert.True(talent.IsRanked);
    }

    [Theory]
    [InlineData("Signature Spell", 2, false)]
    [InlineData("Signature Spell (Improved)", 4, false)]
    [InlineData("Conduit", 4, false)]
    public async Task CorrectedTierAndRanked(string name, int tier, bool ranked)
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == name);

        Assert.Equal(tier, talent.Tier);
        Assert.Equal(ranked, talent.IsRanked);
    }

    /// <summary>Канонические display names при прежних stable codes.</summary>
    [Theory]
    [InlineData("Can’t We Talk About This?")]
    [InlineData("Eagle Eyes")]
    [InlineData("Painkiller Specialization")]
    [InlineData("Back-to-Back")]
    [InlineData("Chill of Nordros")]
    [InlineData("Dominion of the Dimora")]
    [InlineData("Favor of the Fae")]
    [InlineData("Flames of Kellos")]
    [InlineData("Flash of Insight")]
    [InlineData("Justice of the Citadel")]
    [InlineData("Let’s Ride")]
    public async Task CanonicalDisplayNames(string name)
    {
        Assert.Contains(await RotTalentsAsync(), t => t.Name == name);
    }

    /// <summary>Out-of-turn Incidental — отдельный тайминг, а не обычный Incidental.</summary>
    [Theory]
    [InlineData("Block")]
    [InlineData("Bulwark")]
    [InlineData("Clever Retort")]
    [InlineData("Counterattack")]
    [InlineData("Dodge")]
    [InlineData("Heroic Will")]
    [InlineData("Let’s Talk This Over")]
    [InlineData("Parry")]
    [InlineData("Parry (Improved)")]
    [InlineData("Retribution!")]
    [InlineData("Threaten")]
    public async Task OutOfTurnTalentsAreMarkedAsSuch(string name)
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == name);

        Assert.True(talent.CanUseOutOfTurn);
        Assert.Equal("Out-of-turn Incidental", talent.ActivationEn);
    }

    [Fact]
    public async Task ShapeshifterImproved_IsOutOfTurnOnlyThroughItsTrigger()
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == "Shapeshifter (Improved)");

        Assert.True(talent.CanUseOutOfTurn);
        Assert.Equal("Triggered Incidental", talent.ActivationEn);
    }

    [Fact]
    public async Task OrdinaryIncidentalIsNotMarkedOutOfTurn()
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == "Quick Draw");

        Assert.False(talent.CanUseOutOfTurn);
        Assert.Equal("Incidental", talent.ActivationEn);
    }

    /// <summary>ROT-TAL-04: таланты, выдающие карьерные навыки.</summary>
    [Theory]
    [InlineData("Adventurer", new[] { "Athletics", "Knowledge (Adventuring)" })]
    [InlineData("Bard", new[] { "Knowledge (Lore)", "Verse" })]
    [InlineData("Hunter", new[] { "Knowledge (Geography)", "Ranged", "Survival" })]
    [InlineData("Runic Lore", new[] { "Knowledge (Lore)", "Runes" })]
    [InlineData("Templar", new[] { "Divine" })]
    [InlineData("Well-Traveled", new[] { "Knowledge (Geography)", "Negotiation", "Vigilance" })]
    public async Task TalentGrantedCareerSkills(string name, string[] expected)
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == name);

        Assert.Equal(expected, talent.CareerSkillNames);
    }

    [Fact]
    public async Task OrdinaryTalentGrantsNoCareerSkills()
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == "Grit");

        Assert.Empty(talent.CareerSkillNames);
    }

    [Fact]
    public async Task EveryActiveTalentHasATierWithinTheAllowedRange()
    {
        foreach (var talent in await RotTalentsAsync())
            Assert.InRange(talent.Tier, 1, 5);
    }

    [Fact]
    public async Task HeightenedAwareness_UsesReadableBoostNotation()
    {
        var talent = (await RotTalentsAsync()).Single(t => t.Name == "Heightened Awareness");

        Assert.Equal(
            "Союзники в пределах короткой дистанции от персонажа прибавляют ◻ к проверкам Бдительности и Внимания, а союзники, находящиеся вплотную к персонажу, — ◻ ◻.",
            talent.Description);
        Assert.Contains("boost die", talent.DescriptionEn, StringComparison.Ordinal);
        Assert.Contains("two boost dice", talent.DescriptionEn, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RussianTalentDescriptionsContainNoKnownOcrFragments()
    {
        var descriptions = (await RotTalentsAsync()).Concat(await CoreTalentsAsync())
            .Select(t => t.Description)
            .Distinct(StringComparer.Ordinal);

        foreach (var description in descriptions)
        {
            foreach (var fragment in KnownRussianOcrFragments)
                Assert.DoesNotContain(fragment, description, StringComparison.Ordinal);
            Assert.DoesNotContain(" Х ", description, StringComparison.Ordinal);
            Assert.DoesNotContain("на l.", description, StringComparison.Ordinal);
            Assert.DoesNotContain("[]", description, StringComparison.Ordinal);
        }
    }
}
