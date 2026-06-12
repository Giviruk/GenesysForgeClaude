namespace GenesysForge.Domain;

/// <summary>Талант персонажа с пассивными бонусами, применяемыми за каждый ранг.</summary>
public record TalentInput(
    string Name,
    int Tier,
    int Ranks,
    int WoundBonusPerRank = 0,
    int StrainBonusPerRank = 0,
    int SoakBonusPerRank = 0,
    int MeleeDefenseBonusPerRank = 0,
    int RangedDefenseBonusPerRank = 0);
