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
    bool Repeatable = false,
    /// <summary>
    /// Направления, которым доступна запись (ROT-MAG-01). Клиент фильтрует по этому списку и не
    /// собирает матрицу доступности сам — иначе две реализации разъедутся.
    /// </summary>
    IReadOnlyList<string>? AllowedSkills = null,
    /// <summary>Число из <see cref="Difficulty"/>: базовая сложность или надбавка эффекта.</summary>
    int DifficultyIncrease = 0,
    /// <summary>Коды эффектов, вместе с которыми этот выбрать нельзя.</summary>
    IReadOnlyList<string>? Exclusions = null,
    /// <summary>Как эффект применяется: сразу, свойством, активацией преимуществ, очком сюжета.</summary>
    SpellResolutionKind Resolution = SpellResolutionKind.OnSuccess,
    /// <summary>Запись из опциональной книги (Expanded Player's Guide).</summary>
    bool IsOptional = false,
    /// <summary>Числа эффекта берутся из рангов Знания заклинателя (ROT-MAG-10).</summary>
    bool UsesKnowledgeRating = false,
    /// <summary>Свойства, получающие рейтинг по Знанию; пусто — рейтинг не у свойства.</summary>
    IReadOnlyList<SpellRatedQualityDto>? RatedQualities = null);

/// <summary>
/// Свойство, чей рейтинг равен рангам Знания заклинателя (ROT-MAG-10). Имена резолвятся по
/// справочнику качеств на сервере: хранить их второй раз в записи заклинания незачем.
/// </summary>
public record SpellRatedQualityDto(string Code, string NameRu, string NameEn);
