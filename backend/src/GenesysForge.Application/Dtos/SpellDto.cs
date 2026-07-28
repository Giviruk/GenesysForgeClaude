using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record SpellDto(Guid Id, string MagicSkill, SpellEntryKind Kind, string ParentEffect,
    string NameRu, string NameEn, string Difficulty, string Description, string SafeDescription,
    string Source, bool IsCustom, string DescriptionEn = "",
    /// <summary>
    /// Навык, которому эффект доступен исключительно; пусто — доступен нескольким. По нему
    /// считается скидка священного символа (ROT-MAG-IMP-01).
    /// </summary>
    string RestrictedSkill = "",
    /// <summary>
    /// Эффект можно добавлять к одному заклинанию несколько раз, каждый раз повышая сложность
    /// (Дистанция, Размер, Увеличение силуэта).
    /// </summary>
    bool Repeatable = false);
