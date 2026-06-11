using GenesysForge.Domain;

namespace GenesysForge.Api.Data;

public class User
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public required string PasswordHash { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>Определение навыка (встроенное или кастомное — у кастомного задан OwnerUserId).</summary>
public class SkillDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public CharacteristicType Characteristic { get; set; }
    public SkillKind Kind { get; set; }
    public Guid? OwnerUserId { get; set; }
}

public class TalentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public int Tier { get; set; }
    public bool IsRanked { get; set; }
    public string Description { get; set; } = "";
    public string Activation { get; set; } = "Пассивный";
    // Пассивные бонусы, применяемые автоматически за каждый ранг.
    public int WoundBonus { get; set; }
    public int StrainBonus { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefenseBonus { get; set; }
    public int RangedDefenseBonus { get; set; }
    public Guid? OwnerUserId { get; set; }
}

public class ItemDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public ItemKind Kind { get; set; }
    public int Encumbrance { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefense { get; set; }
    public int RangedDefense { get; set; }
    public int EncumbranceThresholdBonus { get; set; }
    public string Description { get; set; } = "";
    public int Price { get; set; }
    public int Rarity { get; set; }
    public Guid? OwnerUserId { get; set; }
}

public class HeroicAbilityDef
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public Guid? OwnerUserId { get; set; }
}

public class ArchetypeDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public int Brawn { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Cunning { get; set; }
    public int Willpower { get; set; }
    public int Presence { get; set; }
    public int WoundBase { get; set; }
    public int StrainBase { get; set; }
    public int StartingXp { get; set; }
    public string Description { get; set; } = "";
}

public class CareerDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    public required string Name { get; set; }
    public string Description { get; set; } = "";
    public List<string> CareerSkillNames { get; set; } = [];
}

public class Character
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    public GameSystem System { get; set; }
    public Guid ArchetypeId { get; set; }
    public ArchetypeDef? Archetype { get; set; }
    public Guid CareerId { get; set; }
    public CareerDef? Career { get; set; }

    public int Brawn { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Cunning { get; set; }
    public int Willpower { get; set; }
    public int Presence { get; set; }

    public int TotalXp { get; set; }
    public int SpentXp { get; set; }
    /// <summary>Пока true — действуют ограничения создания (характеристики за XP, ранг навыка ≤ 2).</summary>
    public bool IsCreationPhase { get; set; } = true;

    public int WoundsCurrent { get; set; }
    public int StrainCurrent { get; set; }

    public Guid? HeroicAbilityId { get; set; }
    public HeroicAbilityDef? HeroicAbility { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CharacterSkill> Skills { get; set; } = [];
    public List<CharacterTalent> Talents { get; set; } = [];
    public List<CharacterItem> Items { get; set; } = [];

    public CharacteristicsSet Characteristics => new(Brawn, Agility, Intellect, Cunning, Willpower, Presence);
}

public class CharacterSkill
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid SkillDefId { get; set; }
    public SkillDef? SkillDef { get; set; }
    public int Ranks { get; set; }
    public bool IsCareer { get; set; }
}

public class CharacterTalent
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid TalentDefId { get; set; }
    public TalentDef? TalentDef { get; set; }
    public int Ranks { get; set; } = 1;
}

public class CharacterItem
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid ItemDefId { get; set; }
    public ItemDef? ItemDef { get; set; }
    public int Quantity { get; set; } = 1;
    public ItemState State { get; set; } = ItemState.Carried;
}
