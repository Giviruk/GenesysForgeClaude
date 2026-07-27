using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Добавление предмета. Сумму списания считает сервер по цене каталога (ROT-ECO-01): клиент
/// присылает только предмет и количество.
/// </summary>
/// <param name="Free">
/// Предмет выдан без оплаты (находка, награда, выдача ведущим) — кошелёк не трогается.
/// </param>
/// <param name="PriceOverride">
/// Цена за единицу, назначенная ведущим вместо каталожной. Требует <paramref name="OverrideReason"/>
/// и целиком попадает в историю персонажа.
/// </param>
public record AddItemRequest(
    Guid ItemDefId,
    int Quantity,
    ItemState State,
    bool Free = false,
    int? PriceOverride = null,
    string? OverrideReason = null);

/// <summary>
/// Продажа предмета. Выручку считает сервер по цене каталога и результату проверки: провал не
/// продаёт ничего, 1 успех — 25 %, 2 — 50 %, 3 и больше — 75 % (ROT-ECO-01).
/// </summary>
/// <param name="NetSuccesses">Нетто-успехи проверки Переговоров или Уличной смекалки.</param>
/// <param name="ConditionMultiplier">
/// Явная поправка ведущего за состояние товара. Автоматической скидки правила не дают, поэтому
/// множитель задаётся вручную и требует <paramref name="ConditionReason"/>.
/// </param>
public record SellItemRequest(
    int Quantity,
    int NetSuccesses,
    double? ConditionMultiplier = null,
    string? ConditionReason = null);
