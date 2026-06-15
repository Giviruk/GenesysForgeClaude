using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <param name="Characteristic">
/// Для талантов, увеличивающих характеристику (Dedication), — выбранная характеристика. Иначе null.
/// </param>
public record BuyTalentRequest(Guid TalentDefId, CharacteristicType? Characteristic = null);
