namespace GenesysForge.Domain;

/// <summary>Категория домашнего правила (см. спецификацию §5.3). Применима к записям типа HouseRule.</summary>
public enum HouseRuleCategory
{
    None = 0,
    CharacterCreation = 1,
    Combat = 2,
    Magic = 3,
    Equipment = 4,
    Xp = 5,
    CampaignTone = 6,
    Custom = 7,
}
