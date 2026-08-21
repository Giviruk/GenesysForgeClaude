using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

public record CharacterTalentDto(Guid TalentDefId, string Name, string NameRu, int Tier, bool IsRanked, int Ranks,
    string Activation, string Description,
    int WoundBonus, int StrainBonus, int SoakBonus, int MeleeDefenseBonus, int RangedDefenseBonus,
    bool GrantsCharacteristic, IReadOnlyList<CharacteristicType> GrantedCharacteristics, string DescriptionEn = "",
    /// <summary>Сохранённые выборы по рангам (ROT-TAL-03).</summary>
    IReadOnlyList<CharacterTalentChoiceDto>? Choices = null,
    /// <summary>Талант требует выбора, которого нет; эффект заблокирован до ручного исправления.</summary>
    bool NeedsChoice = false,
    /// <summary>Английский тайминг активации и возможность применения вне хода (ROT-TAL-01).</summary>
    string ActivationEn = "", bool CanUseOutOfTurn = false,
    /// <summary>Стабильный bare-код определения таланта для структурных правил UI.</summary>
    string LinkCode = "");
