using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterItemDto(Guid Id, Guid ItemDefId, string Name, string NameRu, ItemKind Kind, ItemState State, int Quantity,
    int Encumbrance, int SoakBonus, int MeleeDefense, int RangedDefense, int EncumbranceThresholdBonus, int Load,
    string Description, int Price, string SkillName, string Damage, string Crit, string RangeBand, string Properties,
    string DescriptionEn = "",
    /// <summary>
    /// Позиция выбрана активной бронёй (ROT-CMB-02): только она даёт защиту и поглощение.
    /// Прочая надетая броня продолжает считаться в переносимый вес.
    /// </summary>
    bool IsActiveArmor = false);
