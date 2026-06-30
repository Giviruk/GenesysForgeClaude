namespace GenesysForge.Domain.Entities;

public class TalentDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента. У кастома пусто.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название.</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    public int Tier { get; set; }
    public bool IsRanked { get; set; }
    /// <summary>Пригодность к сеттингам (флаги). Определяет, в каких системах талант доступен.</summary>
    public GenesysSetting Setting { get; set; } = GenesysSetting.Any;
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница. Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    public string Activation { get; set; } = "Пассивный";
    /// <summary>
    /// Талант увеличивает выбранную характеристику на 1 за каждый ранг (Dedication / «Повышение»).
    /// При покупке игрок выбирает характеристику; одну и ту же дважды этим талантом увеличить нельзя.
    /// </summary>
    public bool GrantsCharacteristic { get; set; }
    // Пассивные бонусы, применяемые автоматически за каждый ранг.
    public int WoundBonus { get; set; }
    public int StrainBonus { get; set; }
    public int SoakBonus { get; set; }
    public int MeleeDefenseBonus { get; set; }
    public int RangedDefenseBonus { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
}
