namespace GenesysForge.Domain;

/// <summary>Тип записи Content Pack (см. спецификацию Campaign Handbook §4).</summary>
public enum ContentEntryType
{
    Archetype = 0,
    Career = 1,
    Skill = 2,
    Talent = 3,
    Item = 4,
    HeroicAbility = 5,
    Spell = 6,
    MagicAction = 7,
    AlchemyRecipe = 8,
    Rune = 9,
    HouseRule = 10,
    CustomNote = 11,
}
