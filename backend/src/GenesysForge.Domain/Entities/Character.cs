namespace GenesysForge.Domain.Entities;

public class Character
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Name { get; set; }
    /// <summary>URL портрета (необязательно). Пусто → клиент показывает заглушку.</summary>
    public string? PortraitUrl { get; set; }
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

    /// <summary>
    /// Порог ран, зафиксированный в момент завершения создания (ROT-CRE-02). После completion
    /// изменение Brawn (в том числе талантом Dedication) не пересчитывает порог; сверху
    /// прибавляются только явные модификаторы порога (Toughened и т. п.).
    /// <c>null</c> — персонаж ещё в фазе создания, порог считается динамически.
    /// </summary>
    public int? CreationWoundThreshold { get; set; }
    /// <summary>Порог стрейна, зафиксированный в момент завершения создания. См. <see cref="CreationWoundThreshold"/>.</summary>
    public int? CreationStrainThreshold { get; set; }
    /// <summary>Происхождение сохранённых порогов: точный snapshot, восстановление или оценка.</summary>
    public ThresholdSnapshotProvenance ThresholdSnapshotProvenance { get; set; } = ThresholdSnapshotProvenance.None;
    /// <summary>
    /// Данные персонажа восстановлены неоднозначно и требуют ручной проверки владельцем/GM.
    /// Не блокирует игру, но показывается в UI и экспорте.
    /// </summary>
    public bool RulesReviewRequired { get; set; }

    /// <summary>
    /// Кошелёк персонажа (нейтральные «монеты») — реальная валюта. В режиме <c>StandardMoney</c>
    /// это стартовые карманные деньги 1d100, в режиме <c>CareerPackage</c> — сумма комплекта.
    /// Бюджет стартовых покупок сюда не прибавляется: см. <see cref="StartingPurchaseBudget"/>.
    /// </summary>
    public int Money { get; set; }

    /// <summary>
    /// Код видовой способности, выбранной при создании там, где вид требует выбора
    /// (Half-Catfolk: Claws или Fleet of Paw). Пусто у видов без выбора и у старых персонажей,
    /// которым выбор нужно сделать вручную — автоматически подставлять его нельзя (ROT-SPECIES-01).
    /// </summary>
    public string SpeciesAbilityChoiceCode { get; set; } = "";

    /// <summary>Выбранный при создании режим стартового снаряжения (ROT-CRE-03).</summary>
    public StartingEquipmentMode StartingEquipmentMode { get; set; } = StartingEquipmentMode.StandardMoney;

    /// <summary>
    /// Остаток бюджета стартовых покупок. В режиме <c>StandardMoney</c> начинается с 500 и тратится
    /// только до завершения создания; в режиме <c>CareerPackage</c> равен нулю. Это отдельный от
    /// <see cref="Money"/> счёт — сумма 500 и карманных денег в один баланс не складывается.
    /// </summary>
    public int StartingPurchaseBudget { get; set; }

    public Guid? HeroicAbilityId { get; set; }
    public HeroicAbilityDef? HeroicAbility { get; set; }
    /// <summary>Купленный ранг улучшения героической способности: 0 — базовая, 1 — Improved, 2 — Supreme.</summary>
    public int HeroicUpgradeRank { get; set; }
    /// <summary>Число покупок Duration: каждая добавляет один ход и стоит 1 ability point.</summary>
    public int HeroicDurationRanks { get; set; }
    /// <summary>Число покупок Frequency: каждая добавляет одно применение за сессию и стоит 2 ability points.</summary>
    public int HeroicFrequencyRanks { get; set; }
    /// <summary>Куплено ли улучшение Story, снижающее стоимость активации до 1 Story Point.</summary>
    public bool HeroicStoryUpgrade { get; set; }

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
    public List<CharacterCriticalInjury> CriticalInjuries { get; set; } = [];
    public List<CharacterHeroicSecondaryEffect> HeroicSecondaryEffects { get; set; } = [];

    public CharacteristicsSet Characteristics => new(Brawn, Agility, Intellect, Cunning, Willpower, Presence);

    public int AvailableXp => TotalXp - SpentXp;

    /// <summary>XP, заработанный после создания (сверх стартового XP архетипа).</summary>
    public int EarnedXp => Math.Max(0, TotalXp - (Archetype?.StartingXp ?? 0));

    /// <summary>Всего ability points: по 1 за каждые полные 50 XP сверх стартового XP вида.</summary>
    public int HeroicUpgradePointsTotal => EarnedXp / 50;

    /// <summary>Очки, потраченные на Power, Duration, Frequency, Story и Secondary Effects.</summary>
    public int HeroicUpgradePointsSpent =>
        (HeroicAbility?.Upgrades.Where(u => (int)u.Level <= HeroicUpgradeRank).Sum(u => u.Cost) ?? 0)
        + HeroicDurationRanks
        + HeroicFrequencyRanks * 2
        + (HeroicStoryUpgrade ? 1 : 0)
        + HeroicSecondaryEffects.Count;

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
