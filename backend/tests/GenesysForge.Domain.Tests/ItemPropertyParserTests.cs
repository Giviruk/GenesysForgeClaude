using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class ItemPropertyParserTests
{
    [Fact]
    public void Parse_SplitsTokens_AndExtractsRatings()
    {
        var tokens = ItemPropertyParser.Parse("Точное 1, Оборонительное 2, Нокдаун").ToList();

        Assert.Equal(3, tokens.Count);
        Assert.Equal(("Точное", (int?)1), (tokens[0].Name, tokens[0].Rating));
        Assert.Equal(("Оборонительное", (int?)2), (tokens[1].Name, tokens[1].Rating));
        Assert.Equal(("Нокдаун", (int?)null), (tokens[2].Name, tokens[2].Rating));
    }

    [Fact]
    public void Parse_EmptyOrNull_ReturnsNothing()
    {
        Assert.Empty(ItemPropertyParser.Parse(null));
        Assert.Empty(ItemPropertyParser.Parse("   "));
    }

    [Fact]
    public void Normalize_LowercasesYoAndStripsRating()
    {
        Assert.Equal("залповое", ItemPropertyParser.Normalize("Залповое 3"));
        Assert.Equal("елка", ItemPropertyParser.Normalize("Ёлка")); // ё→е, нижний регистр
        Assert.Equal("точное", ItemPropertyParser.Normalize("Точное"));
    }
}
