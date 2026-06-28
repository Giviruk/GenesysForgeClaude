namespace GenesysForge.Domain.Entities;

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

    /// <summary>Кошелёк персонажа (нейтральные «монеты»). Стартовый баланс — 500.</summary>
    public int Money { get; set; } = 500;

    public Guid? HeroicAbilityId { get; set; }
    public HeroicAbilityDef? HeroicAbility { get; set; }
    /// <summary>Купленный ранг улучшения героической способности: 0 — базовая, 1 — Improved, 2 — Supreme.</summary>
    public int HeroicUpgradeRank { get; set; }

    // Мотивации и предыстория (U-22). Все опциональны: пусто → null.
    public string? Desire { get; set; }
    public string? Fear { get; set; }
    public string? Strength { get; set; }
    public string? Flaw { get; set; }
    public string? Background { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<CharacterSkill> Skills { get; set; } = [];
    public List<CharacterTalent> Talents { get; set; } = [];
    public List<CharacterItem> Items { get; set; } = [];

    public CharacteristicsSet Characteristics => new(Brawn, Agility, Intellect, Cunning, Willpower, Presence);

    public int AvailableXp => TotalXp - SpentXp;

    /// <summary>XP, заработанный после создания (сверх стартового XP архетипа).</summary>
    public int EarnedXp => Math.Max(0, TotalXp - (Archetype?.StartingXp ?? 0));

    /// <summary>Всего очков улучшения героики: 1 стартовое + по 1 каждые 50 заработанного XP.</summary>
    public int HeroicUpgradePointsTotal => 1 + EarnedXp / 50;

    /// <summary>Очки улучшения, потраченные на купленные ранги выбранной способности.</summary>
    public int HeroicUpgradePointsSpent =>
        HeroicAbility?.Upgrades.Where(u => (int)u.Level <= HeroicUpgradeRank).Sum(u => u.Cost) ?? 0;

    public int GetCharacteristic(CharacteristicType type) => Characteristics.Get(type);

    public void IncreaseCharacteristic(CharacteristicType type)
    {
        switch (type)
        {
            case CharacteristicType.Brawn: Brawn++; break;
            case CharacteristicType.Agility: Agility++; break;
            case CharacteristicType.Intellect: Intellect++; break;
            case CharacteristicType.Cunning: Cunning++; break;
            case CharacteristicType.Willpower: Willpower++; break;
            case CharacteristicType.Presence: Presence++; break;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    public void DecreaseCharacteristic(CharacteristicType type)
    {
        switch (type)
        {
            case CharacteristicType.Brawn: Brawn--; break;
            case CharacteristicType.Agility: Agility--; break;
            case CharacteristicType.Intellect: Intellect--; break;
            case CharacteristicType.Cunning: Cunning--; break;
            case CharacteristicType.Willpower: Willpower--; break;
            case CharacteristicType.Presence: Presence--; break;
            default: throw new ArgumentOutOfRangeException(nameof(type));
        }
    }
}
