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
/// <param name="Craftsmanship">
/// Качество изготовления экземпляра (ROT-WPN-02). Задаётся только здесь и дальше не меняется;
/// цену с его учётом считает сервер. У снаряжения допустима только обычная работа.
/// </param>
/// <param name="PricePercent">
/// Доля цены экземпляра при торге: 50…200 % с шагом 25. Взаимоисключающа с
/// <paramref name="PriceOverride"/>; сумму по ней считает сервер от цены каталога, поэтому
/// «сколько списать» клиент по-прежнему не присылает.
/// </param>
public record AddItemRequest(
    Guid ItemDefId,
    int Quantity,
    ItemState State,
    bool Free = false,
    int? PriceOverride = null,
    string? OverrideReason = null,
    WeaponCraftsmanship Craftsmanship = WeaponCraftsmanship.Steel,
    int? PricePercent = null);

/// <summary>
/// Продажа предмета. Сумму всегда считает сервер от цены каталога — клиент её не назначает.
/// <para>
/// Режимов три, и они взаимоисключающие. По проверке (<paramref name="NetSuccesses"/>): провал не
/// продаёт ничего, 1 успех — 25 %, 2 — 50 %, 3 и больше — 75 % (ROT-ECO-01). Без проверки
/// (<paramref name="Percent"/>): игрок просто продаёт предмет за указанную долю цены. По
/// договорной цене (<paramref name="PriceOverride"/>): цена за штуку задаётся вручную и требует
/// причины. Если не указано ничего, предмет продаётся за полную цену каталога.
/// </para>
/// </summary>
/// <param name="NetSuccesses">Нетто-успехи проверки Переговоров или Уличной смекалки.</param>
/// <param name="Percent">Доля цены каталога, 0–100, для продажи без проверки.</param>
/// <param name="PriceOverride">
/// Цена за единицу, назначенная вместо каталожной (сделка по договорённости). Требует
/// <paramref name="OverrideReason"/> и целиком попадает в историю персонажа.
/// </param>
/// <param name="OverrideReason">Причина назначенной вручную цены.</param>
/// <param name="ConditionMultiplier">
/// Явная поправка ведущего за состояние товара. Автоматической скидки правила не дают, поэтому
/// множитель задаётся вручную и требует <paramref name="ConditionReason"/>.
/// </param>
public record SellItemRequest(
    int Quantity,
    int? NetSuccesses = null,
    int? Percent = null,
    int? PriceOverride = null,
    string? OverrideReason = null,
    double? ConditionMultiplier = null,
    string? ConditionReason = null);
