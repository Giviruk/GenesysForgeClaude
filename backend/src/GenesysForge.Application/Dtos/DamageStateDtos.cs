using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Смена состояния повреждения (GEN-EQP-DMG-01). Отдельное действие: и Sunder в бою, и порча по
/// сюжету приводят сюда, потому что решение всё равно принимает стол, а не приложение.
/// </summary>
/// <param name="Reason">Пометка: чем повреждено. Пусто — обычная запись без пояснения.</param>
public record SetItemDamageStateRequest(ItemDamageState State, string? Reason = null);

/// <summary>
/// Ремонт по кнопке (GEN-EQP-DMG-01). Броска проверки Механики приложение не делает — решение
/// владельца: правило книги показывается памяткой, а исход определяет стол. Кнопка списывает
/// материалы и возвращает предмету целое состояние.
/// </summary>
/// <param name="Free">Ремонт без списания материалов: своя мастерская, услуга покровителя.</param>
/// <param name="NetAdvantages">
/// Чистые преимущества самостоятельного ремонта: каждое снимает 10 % стоимости материалов.
/// Ноль — обычная цена. Бросок для этого не нужен, число называет стол.
/// </param>
/// <param name="CostOverride">Стоимость материалов, назначенная ведущим; требует причины.</param>
public record RepairItemRequest(
    bool Free = false,
    int NetAdvantages = 0,
    int? CostOverride = null,
    string? OverrideReason = null);
