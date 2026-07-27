namespace GenesysForge.Domain.Entities;

/// <summary>
/// Один сохранённый выбор игрока для конкретного ранга таланта (ROT-TAL-03).
/// Значение хранится стабильным идентификатором, а отображаемое имя — только снимком для UI:
/// переименование справочника не должно менять смысл уже сделанного выбора.
/// </summary>
public class CharacterTalentChoice
{
    public Guid Id { get; set; }
    public Guid CharacterTalentId { get; set; }

    /// <summary>Номер ранга, к которому относится выбор; 0 — первый купленный ранг.</summary>
    public int RankIndex { get; set; }

    /// <summary>Тип выбора — определяет, как читать <see cref="Value"/>.</summary>
    public TalentChoiceKind Kind { get; set; }

    /// <summary>
    /// Стабильное значение выбора: имя <c>CharacteristicType</c>, каноническое имя навыка,
    /// сериализованная конфигурация заклинания или идентификатор спутника.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>Отображаемое имя на момент выбора — снимок для листа и печати.</summary>
    public string DisplayName { get; set; } = "";
}
