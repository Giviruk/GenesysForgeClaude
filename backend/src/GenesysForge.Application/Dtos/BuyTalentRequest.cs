using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <param name="Characteristic">
/// Для талантов, увеличивающих характеристику (Dedication), — выбранная характеристика. Иначе null.
/// </param>
public record BuyTalentRequest(Guid TalentDefId, CharacteristicType? Characteristic = null,
    /// <summary>
    /// Стабильные значения обязательного выбора для покупаемого ранга (ROT-TAL-03): имена
    /// характеристик, канонические имена навыков, конфигурация заклинания или спутник.
    /// Для Dedication допускается старое поле <see cref="Characteristic"/> как алиас.
    /// </summary>
    List<string>? Choices = null);
