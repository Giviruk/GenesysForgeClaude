using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>Откуда навык получил карьерный статус: <c>Career</c>, <c>Species</c> или <c>Talent</c>.</summary>
public record CareerSkillSourceDto(string Source, string SourceName);

/// <summary>
/// Один источник помех к проверке: снаряжение или перегруз. Условный вклад (<c>Condition</c>
/// непусто) показывается, но в автоматический пул не входит.
/// </summary>
/// <param name="Boost">Кости умения от источника (улучшения брони, ROT-EQP-ATT-01).</param>
public record CheckModifierSourceDto(
    string SourceType, string SourceName, string SourceNameRu, int Setback, string Condition,
    int Boost = 0);

public record CharacterSkillDto(Guid SkillDefId, string Name, string NameRu, SkillKind Kind, CharacteristicType Characteristic,
    int Ranks, bool IsCareer, DicePoolDto Pool, int NextRankCost, int FreeRanks,
    IReadOnlyList<CareerSkillSourceDto> CareerSources,
    /// <summary>
    /// Навык исключён из набора навыков системы, но у персонажа остались купленные ранги
    /// (ROT-CLEAN-3.2). Ранги работают, новый ранг купить нельзя.
    /// </summary>
    bool SourceMismatch = false,
    /// <summary>
    /// Безусловные кости помех к этой проверке: снаряжение и перегруз (ROT-ARM-01, ROT-EQP-01).
    /// Ровно столько чёрных кубов роллер подставляет сам.
    /// </summary>
    int SetbackDice = 0,
    /// <summary>Из чего сложились помехи, включая условные вклады.</summary>
    IReadOnlyList<CheckModifierSourceDto>? SetbackSources = null,
    /// <summary>
    /// Безусловные кости умения к этой проверке от установленных улучшений (ROT-EQP-ATT-01).
    /// Ровно столько синих кубов роллер подставляет сам.
    /// </summary>
    int BoostDice = 0);
