using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record TalentDefDto(Guid Id, string Name, string NameRu, int Tier, bool IsRanked, TalentCategory Category, GenesysSetting Setting,
    string Activation, string Description, string SafeDescription, string Source,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus, bool IsCustom,
    bool GrantsCharacteristic, string DescriptionEn = "",
    /// <summary>Английская подпись тайминга активации — стабильнее локализованной строки.</summary>
    string ActivationEn = "",
    /// <summary>Талант применим вне своего хода (Out-of-turn Incidental, ROT-TAL-01).</summary>
    bool CanUseOutOfTurn = false,
    /// <summary>Навыки, которые талант делает карьерными, пока принадлежит персонажу (ROT-TAL-04).</summary>
    IReadOnlyList<string>? CareerSkillNames = null);
