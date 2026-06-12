namespace GenesysForge.Domain;

public record SkillComputed(string Name, CharacteristicType Characteristic, int Ranks, bool IsCareer, DicePool Pool);
