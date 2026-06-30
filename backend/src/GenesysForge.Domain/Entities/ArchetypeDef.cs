namespace GenesysForge.Domain.Entities;

public class ArchetypeDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }
    /// <summary>Стабильный код встроенного контента.</summary>
    public string Code { get; set; } = "";
    /// <summary>Оригинальное/английское название.</summary>
    public required string Name { get; set; }
    /// <summary>Русское название.</summary>
    public string NameRu { get; set; } = "";
    public int Brawn { get; set; }
    public int Agility { get; set; }
    public int Intellect { get; set; }
    public int Cunning { get; set; }
    public int Willpower { get; set; }
    public int Presence { get; set; }
    public int WoundBase { get; set; }
    public int StrainBase { get; set; }
    public int StartingXp { get; set; }
    /// <summary>Полное (private) описание-парафраз. Отдаётся в режиме ContentMode.PrivateFull.</summary>
    public string Description { get; set; } = "";
    /// <summary>Copyright-safe краткое описание для публичной версии (ContentMode.PublicSafe).</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Ссылка на источник: книга/раздел/страница. Доступна в обоих режимах.</summary>
    public string Source { get; set; } = "";
    /// <summary>Null для встроенного контента, UserId для пользовательского homebrew.</summary>
    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
    /// <summary>
    /// Устаревший встроенный вид: не предлагается при создании персонажа, но остаётся в БД ради
    /// уже созданных персонажей (FK). Выставляется сидом для built-in видов, которых больше нет в каталоге.
    /// </summary>
    public bool Retired { get; set; }

    /// <summary>Видовые способности (структурно). См. <see cref="ArchetypeAbilityDef"/>.</summary>
    public List<ArchetypeAbilityDef> Abilities { get; set; } = [];
    /// <summary>Стартовые навыки вида. См. <see cref="ArchetypeStartingSkill"/>.</summary>
    public List<ArchetypeStartingSkill> StartingSkills { get; set; } = [];
}
