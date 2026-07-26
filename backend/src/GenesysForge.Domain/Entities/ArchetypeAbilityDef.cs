namespace GenesysForge.Domain.Entities;

/// <summary>
/// Видовая способность архетипа как данные (а не только текст в SafeDescription). Отображается при
/// выборе вида. Исполнение эффектов — задача U-18; здесь <see cref="AutomationKind"/> — только тег.
/// </summary>
public class ArchetypeAbilityDef
{
    public Guid Id { get; set; }
    public Guid ArchetypeId { get; set; }
    /// <summary>Стабильный код способности (для будущего движка эффектов).</summary>
    public string Code { get; set; } = "";
    /// <summary>Русское название способности.</summary>
    public string NameRu { get; set; } = "";
    /// <summary>Оригинальное/английское название (может быть пустым, если в источнике нет).</summary>
    public string NameEn { get; set; } = "";
    /// <summary>Copyright-safe краткое описание-парафраз.</summary>
    public string SafeDescription { get; set; } = "";
    /// <summary>Английское описание — собственный copyright-safe парафраз. Используется в обоих режимах контента.</summary>
    public string DescriptionEn { get; set; } = "";
    public ArchetypeAbilityAutomationKind AutomationKind { get; set; } = ArchetypeAbilityAutomationKind.Manual;

    /// <summary>
    /// Исполняемый тип правила (ROT-SPECIES-01). Единственный источник механики: выводить эффект
    /// из <see cref="Code"/>, <see cref="NameRu"/> или описания запрещено.
    /// </summary>
    public SpeciesAbilityRuleKind RuleKind { get; set; } = SpeciesAbilityRuleKind.Manual;

    /// <summary>
    /// Основной числовой параметр правила: сколько Setback снимается (<c>DarkVision</c> = 2),
    /// сколько урона добавляется (<c>BattleRage</c> = 2, <c>HotTempered</c> = 1), какое значение
    /// устанавливается (<c>SetBaseDefense</c> = 1, <c>SetSilhouette</c> = 0). 0 — параметра нет.
    /// </summary>
    public int RuleValue { get; set; }

    /// <summary>
    /// Дополнительный ограничитель правила: тег источника Setback (<c>darkness</c>), предельная
    /// Encumbrance/Rarity для <c>ConjureMinorItem</c> в формате <c>enc=1;rarity=4</c> и т. п.
    /// Разбирается типизированной стратегией, а не свободным текстом описания.
    /// </summary>
    public string RuleParameters { get; set; } = "";

    /// <summary>Сколько раз способность можно применить в пределах <see cref="UseScope"/>. 0 — без лимита.</summary>
    public int UsesPerScope { get; set; }

    /// <summary>Область сброса счётчика использований.</summary>
    public AbilityUseScope UseScope { get; set; } = AbilityUseScope.None;

    /// <summary>Стоимость активации в Story Point игроков. 0 — активация бесплатна.</summary>
    public int StoryPointCost { get; set; }
}
