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
    IReadOnlyList<string>? CareerSkillNames = null,
    /// <summary>
    /// Bare-slug код таланта — ключ связей prerequisite/exclusion. Одинаков для обеих игровых
    /// систем, поэтому клиент сопоставляет им и не разбирает полный код сам.
    /// </summary>
    string LinkCode = "",
    /// <summary>Обязательный талант-предусловие, bare-slug код; пусто — предусловий нет (ROT-TAL-02).</summary>
    string RequiresTalentCode = "",
    /// <summary>Коды несовместимых талантов; отношение симметрично.</summary>
    IReadOnlyList<string>? ExcludesTalentCodes = null,
    /// <summary>Лимит применений и область его сброса; стоимость активации (ROT-TAL-05).</summary>
    int UsesPerScope = 0, AbilityUseScope UseScope = AbilityUseScope.None,
    int StoryPointCost = 0, int StrainCost = 0, string Trigger = "",
    /// <summary>Схема обязательного выбора при покупке ранга (ROT-TAL-03).</summary>
    TalentChoiceKind ChoiceKind = TalentChoiceKind.None,
    int ChoiceCountFirstRank = 0, int ChoiceCountNextRank = 0);
