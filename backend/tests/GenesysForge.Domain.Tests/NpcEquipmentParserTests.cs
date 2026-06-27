using GenesysForge.Domain.Rules;

namespace GenesysForge.Domain.Tests;

public class NpcEquipmentParserTests
{
    [Fact]
    public void Parse_FullCombatLine_ExtractsAllFields()
    {
        var a = NpcEquipmentParser.Parse("Длинный меч (Урон +3, Крит 2, Средняя, Оборонительное 1)");

        Assert.NotNull(a);
        Assert.Equal("Длинный меч", a!.Value.Name);
        Assert.Equal("+3", a.Value.Damage);
        Assert.Equal("2", a.Value.Critical);
        Assert.Equal("Средняя", a.Value.RangeBand);
        var q = Assert.Single(a.Value.Qualities);
        Assert.Equal(("Оборонительное", (int?)1), (q.Name, q.Rating));
    }

    [Fact]
    public void Parse_AbsoluteDamage_AndEnglishMarkers()
    {
        var a = NpcEquipmentParser.Parse("Bow (Damage 7, Crit 3, Long)");

        Assert.NotNull(a);
        Assert.Equal("7", a!.Value.Damage);
        Assert.Equal("3", a.Value.Critical);
        Assert.Equal("Long", a.Value.RangeBand);
    }

    [Theory]
    [InlineData("Кинжал")]                       // голое имя — не атака
    [InlineData("Зелье лечения (3 шт.)")]         // скобка без боевых маркеров
    [InlineData("")]
    [InlineData(null)]
    public void Parse_NonCombat_ReturnsNull(string? line) =>
        Assert.Null(NpcEquipmentParser.Parse(line));

    [Fact]
    public void Parse_CritOnly_StillCountsAsAttack()
    {
        var a = NpcEquipmentParser.Parse("Когти (Крит 4, Вплотную)");

        Assert.NotNull(a);
        Assert.Equal("", a!.Value.Damage);
        Assert.Equal("4", a.Value.Critical);
        Assert.Equal("Вплотную", a.Value.RangeBand);
    }
}
