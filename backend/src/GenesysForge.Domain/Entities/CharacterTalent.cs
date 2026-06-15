namespace GenesysForge.Domain.Entities;

public class CharacterTalent
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid TalentDefId { get; set; }
    public TalentDef? TalentDef { get; set; }
    public int Ranks { get; set; } = 1;

    /// <summary>
    /// Для талантов с <see cref="TalentDef.GrantsCharacteristic"/> — выбранные игроком характеристики
    /// по одной на ранг, в порядке покупки (CSV из имён <see cref="CharacteristicType"/>, напр. «Brawn,Agility»).
    /// </summary>
    public string GrantedCharacteristics { get; set; } = "";

    /// <summary>Разбор <see cref="GrantedCharacteristics"/> в список характеристик в порядке покупки.</summary>
    public List<CharacteristicType> ParseGrants() =>
        string.IsNullOrWhiteSpace(GrantedCharacteristics)
            ? []
            : GrantedCharacteristics
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(Enum.Parse<CharacteristicType>)
                .ToList();

    /// <summary>Сериализует список характеристик обратно в <see cref="GrantedCharacteristics"/>.</summary>
    public void SetGrants(IEnumerable<CharacteristicType> grants) =>
        GrantedCharacteristics = string.Join(',', grants);
}
