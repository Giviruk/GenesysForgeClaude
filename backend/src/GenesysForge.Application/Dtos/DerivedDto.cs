namespace GenesysForge.Application.Dtos;

public record DerivedDto(int WoundThreshold, int StrainThreshold, int Soak, int MeleeDefense, int RangedDefense,
    int EncumbranceThreshold, int EncumbranceLoad, bool Encumbered,
    /// <summary>Как сложилась ближняя защита (ROT-CMB-03).</summary>
    DefenseBreakdownDto? MeleeDefenseBreakdown = null,
    /// <summary>Как сложилась дальняя защита.</summary>
    DefenseBreakdownDto? RangedDefenseBreakdown = null,
    /// <summary>Точная цена перегруза (ROT-EQP-01).</summary>
    EncumbranceDto? Encumbrance = null);

/// <summary>Состояние перегруза: сколько помех, остался ли бесплатный манёвр и во что он обходится.</summary>
public record EncumbranceDto(
    int Overload,
    int SetbackDice,
    bool HasFreeManoeuvre,
    int StrainPerManoeuvre,
    int ZeroEncumbranceLoad);

/// <summary>Один источник защиты для объяснения итога.</summary>
public record DefenseSourceDto(string SourceType, string SourceName, int Value);

/// <summary>
/// Разбор канала защиты: победивший провайдер, проигнорированные провайдеры (они не складываются),
/// надбавки и сырое значение до предела.
/// </summary>
public record DefenseBreakdownDto(
    int Raw,
    int Effective,
    bool Capped,
    DefenseSourceDto? Provider,
    List<DefenseSourceDto> IgnoredProviders,
    List<DefenseSourceDto> Increases);

/// <summary>
/// Откуда персонаж берёт числовой рейтинг эффектов заклинания (ROT-MAG-10). Список, а не одно
/// число: талант «Тёмное прозрение» даёт игроку выбор при сборке заклинания, и решать за него
/// приложение не должно.
/// </summary>
/// <param name="Options">Первый — названный правилами системы, дальше — исключения таланта.</param>
public record KnowledgeRatingDto(IReadOnlyList<KnowledgeRatingOptionDto> Options);

/// <summary>Один источник рейтинга: навык, его ранги и основание, по которому он доступен.</summary>
/// <param name="Reason">
/// <c>default</c> — навык из правил системы, <c>darkInsight</c> — исключение таланта.
/// </param>
public record KnowledgeRatingOptionDto(string Skill, string SkillRu, int Ranks, string Reason);
