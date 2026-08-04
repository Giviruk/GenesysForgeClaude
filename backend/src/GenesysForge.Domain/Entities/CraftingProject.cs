namespace GenesysForge.Domain.Entities;

/// <summary>
/// Проект изготовления, варки или зачарования (ROT-CRAFT-01, ROT-ALCH-02, ROT-CRAFT-MAGIC-01).
/// Хранит всё, из чего сложился результат: снимок цели, навык, сложность, время, стоимость и её
/// способ, фактический бросок и распределённые траты.
/// </summary>
/// <remarks>
/// Требования по ресурсам — инструменты, компоненты, ингредиенты — по решению владельца остаются
/// описанием: приложение их не списывает и наличия не проверяет. Поэтому здесь нет ни резерва
/// материалов, ни партий ингредиентов — есть <see cref="Requirements"/> и посчитанная стоимость,
/// которую игрок и ведущий читают глазами.
/// </remarks>
public class CraftingProject
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public CraftingKind Kind { get; set; }
    public CraftingProjectStatus Status { get; set; } = CraftingProjectStatus.Draft;

    /// <summary>Целевая запись каталога. У зачарования — основа, которую зачаровывают.</summary>
    public Guid ItemDefId { get; set; }
    public ItemDef? ItemDef { get; set; }
    /// <summary>
    /// Зачаровываемая основа — конкретный экземпляр инвентаря, а не запись каталога: качество
    /// «Превосходное» есть у вещи, а не у строки справочника. У изготовления и варки пусто.
    /// </summary>
    public Guid? BaseCharacterItemId { get; set; }

    /// <summary>Снимок имени цели: запись каталога может быть переименована или уйти в Retired.</summary>
    public string TargetName { get; set; } = "";
    /// <summary>Цена и редкость цели на момент старта — по ним посчитаны стоимость и сложность.</summary>
    public int? TargetPrice { get; set; }
    public int? TargetRarity { get; set; }

    /// <summary>Навык проверки: Механика, Выживание, Алхимия или магический навык зачарования.</summary>
    public string SkillName { get; set; } = "";
    /// <summary>Сложность проверки после всех поправок.</summary>
    public int Difficulty { get; set; }
    /// <summary>Сложность по правилу, до поправки ведущего — для разбора.</summary>
    public int BaseDifficulty { get; set; }
    /// <summary>Причина изменённой сложности; пусто — сложность по правилу.</summary>
    public string DifficultyReason { get; set; } = "";

    /// <summary>Время работы: дни у предмета и зачарования, часы у варки.</summary>
    public int Time { get; set; }
    /// <summary>Время по правилу, до поправок ведущего и трат символов.</summary>
    public int BaseTime { get; set; }
    /// <summary>Причина изменённого времени; пусто — время по правилу.</summary>
    public string TimeReason { get; set; } = "";

    /// <summary>Стоимость компонентов по правилу: половина цены цели, округление вверх.</summary>
    public int ListedCost { get; set; }
    /// <summary>Выбранная доля цены, 50…200 с шагом 25. По умолчанию 100.</summary>
    public int CostPercent { get; set; } = 100;
    /// <summary>Своя цена компонентов; <c>null</c> — считается по доле.</summary>
    public int? CostOverride { get; set; }
    /// <summary>Обязательная причина своей цены.</summary>
    public string CostOverrideReason { get; set; } = "";
    /// <summary>Итоговая стоимость компонентов, посчитанная сервером.</summary>
    public int Cost { get; set; }

    /// <summary>
    /// Инструменты, компоненты и условия своими словами. Ни на что не проверяется — это памятка
    /// игроку и ведущему, а не список к списанию.
    /// </summary>
    public string Requirements { get; set; } = "";
    /// <summary>Что именно должно получиться у зачарования — согласованная способность.</summary>
    public string Intent { get; set; } = "";

    /// <summary>
    /// Грубая работа по решению ведущего: простой предмет сделан Выживанием. Отчаяние на любой
    /// последующей проверке с ним позволяет ведущему сломать его.
    /// </summary>
    public bool RoughSurvival { get; set; }

    // Фактический бросок: символы присылает клиент из роллера, как в продаже (ROT-ECO-01).
    public int NetSuccesses { get; set; }
    public int Advantages { get; set; }
    public int Threats { get; set; }
    public int Triumphs { get; set; }
    public int Despairs { get; set; }

    /// <summary>Созданный экземпляр; <c>null</c> — провал или проект ещё не разрешён.</summary>
    public Guid? CreatedCharacterItemId { get; set; }
    /// <summary>Описание результата: все выбранные траты словами, как они попали в предмет.</summary>
    public string Outcome { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }

    public List<CraftingProjectSpend> Spends { get; set; } = [];
}

/// <summary>Одна выбранная трата символов в проекте.</summary>
public class CraftingProjectSpend
{
    public Guid Id { get; set; }
    public Guid CraftingProjectId { get; set; }
    /// <summary>Код <see cref="CraftingSpendDef"/>.</summary>
    public string SpendCode { get; set; } = "";
    /// <summary>Сколько раз выбрана: больше одного только у повторяемых трат.</summary>
    public int Count { get; set; } = 1;
    /// <summary>Параметр траты: код качества, код зелья или формулировка ведущего.</summary>
    public string Parameter { get; set; } = "";
    /// <summary>Чем оплачена: <c>advantage</c>, <c>threat</c>, <c>triumph</c>, <c>despair</c>.</summary>
    public string PaidWith { get; set; } = "";
    /// <summary>Снимок текста траты — он же попадает в описание предмета.</summary>
    public string TextRu { get; set; } = "";
    public string TextEn { get; set; } = "";
}
