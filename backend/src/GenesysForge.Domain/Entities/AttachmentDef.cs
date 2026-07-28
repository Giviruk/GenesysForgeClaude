namespace GenesysForge.Domain.Entities;

/// <summary>
/// Улучшение предмета (ROT-EQP-ATT-01): отдельный тип контента, а не запись снаряжения. Раньше
/// улучшения лежали в каталоге предметов обычным «снаряжением» с ценой и весом — их можно было
/// купить и положить в рюкзак, но не установить, и ни одно правило из них не работало.
/// </summary>
public class AttachmentDef : IContentDef
{
    public Guid Id { get; set; }
    public GameSystem System { get; set; }

    /// <summary>Стабильный код встроенного контента.</summary>
    public string Code { get; set; } = "";
    public required string Name { get; set; }
    public string NameRu { get; set; } = "";

    /// <summary>Сколько слотов улучшений (HP) занимает. Ноль — занимает место в предмете, но не слот.</summary>
    public int HardPointCost { get; set; }

    /// <summary>
    /// Цена. <c>null</c> — предмет бесценен: обычная покупка и продажа недоступны, цену называет
    /// ведущий. Это не то же самое, что цена ноль.
    /// </summary>
    public int? Price { get; set; }

    public int Rarity { get; set; }

    /// <summary>
    /// Улучшение — чары. Ставить его может только тот, у кого есть хотя бы один ранг магического
    /// навыка: одного карьерного статуса без рангов недостаточно.
    /// </summary>
    public bool IsEnchantment { get; set; }

    /// <summary>К какому виду предметов подходит.</summary>
    public ItemKind HostKind { get; set; }

    /// <summary>Признаки, которые предмет обязан иметь все сразу. <c>None</c> — требований нет.</summary>
    public WeaponFormTraits RequiredTraits { get; set; }

    /// <summary>Хотя бы один из признаков. <c>None</c> — требований нет.</summary>
    public WeaponFormTraits RequiredAnyTraits { get; set; }

    /// <summary>Признаки, при которых улучшение не ставится (деревянная кромка у рунного клинка).</summary>
    public WeaponFormTraits ForbiddenTraits { get; set; }

    public List<AttachmentEffect> Effects { get; set; } = [];

    public string Description { get; set; } = "";
    public string SafeDescription { get; set; } = "";
    public string DescriptionEn { get; set; } = "";
    public string Source { get; set; } = "";

    public Guid? OwnerUserId { get; set; }
    public Guid? HomebrewPackId { get; set; }
    public bool Retired { get; set; }
}

/// <summary>
/// Один типизированный эффект улучшения. Механика хранится видом и числами, а не текстом:
/// «получает Проникающее 2» должно считаться, а не читаться.
/// </summary>
public class AttachmentEffect
{
    public Guid Id { get; set; }
    public Guid AttachmentDefId { get; set; }

    public AttachmentEffectKind Kind { get; set; }

    /// <summary>Код качества справочника для эффектов, работающих с качествами.</summary>
    public string QualityCode { get; set; } = "";

    /// <summary>
    /// Код противоположного качества для <see cref="AttachmentEffectKind.GrantQualityOrCancelOpposite"/>.
    /// </summary>
    public string OppositeQualityCode { get; set; } = "";

    /// <summary>Английское имя навыка для эффектов, работающих с проверками.</summary>
    public string SkillName { get; set; } = "";

    /// <summary>Основное значение: рейтинг качества, число костей, величина прибавки.</summary>
    public int Value { get; set; }

    /// <summary>
    /// На сколько растёт уже имеющееся качество у <see cref="AttachmentEffectKind.GrantOrIncreaseQuality"/>.
    /// </summary>
    public int Increment { get; set; }

    public AttachmentEffectCondition Condition { get; set; }

    /// <summary>
    /// Пояснение для эффектов, которые приложение не исполняет (нужен рантайм столкновения) и для
    /// автоматических символов. Показывается игроку, чтобы правило не потерялось.
    /// </summary>
    public string Note { get; set; } = "";
}
