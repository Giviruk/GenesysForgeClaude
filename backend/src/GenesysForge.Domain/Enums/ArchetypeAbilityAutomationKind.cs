namespace GenesysForge.Domain.Entities;

/// <summary>
/// Уровень автоматизации видовой способности (см. §2.3 плана доработок). Сейчас — только
/// классификация для UI/будущего движка эффектов (U-18); исполнение эффектов здесь не делается.
/// </summary>
public enum ArchetypeAbilityAutomationKind
{
    /// <summary>Постоянный числовой эффект — потенциально считается автоматически.</summary>
    Passive,
    /// <summary>Активируется ценой (очко сюжета/усталость и т.п.) — показывается, применяет игрок/ведущий.</summary>
    ActivationCost,
    /// <summary>Эффект с длительностью (вешается на сцену/персонажа).</summary>
    TimedEffect,
    /// <summary>Только текст/подсказка.</summary>
    Manual,
    /// <summary>Требует решения ведущего.</summary>
    RequiresGmDecision,
}
