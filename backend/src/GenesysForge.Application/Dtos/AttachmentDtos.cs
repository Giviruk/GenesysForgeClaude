using GenesysForge.Domain;

namespace GenesysForge.Application.Dtos;

/// <summary>
/// Улучшение справочника (ROT-EQP-ATT-01). Совместимость приходит признаками формы: клиент
/// показывает, к чему улучшение подходит, но решение принимает сервер.
/// </summary>
/// <param name="Price">Цена; <c>null</c> — бесценно, обычная покупка недоступна.</param>
public record AttachmentDefDto(
    Guid Id,
    string Code,
    string Name,
    string NameRu,
    int HardPointCost,
    int? Price,
    int Rarity,
    bool IsEnchantment,
    ItemKind HostKind,
    WeaponFormTraits RequiredTraits,
    WeaponFormTraits RequiredAnyTraits,
    WeaponFormTraits ForbiddenTraits,
    string Description,
    string DescriptionEn,
    string Source,
    IReadOnlyList<AttachmentEffectDto> Effects);

/// <param name="Executed">
/// Эффект действительно считается приложением. <c>false</c> — правило верно записано, но
/// исполнить его нечем (нужен рантайм столкновения) или это автоматический символ.
/// </param>
public record AttachmentEffectDto(
    AttachmentEffectKind Kind,
    string QualityCode,
    string SkillName,
    int Value,
    int Increment,
    AttachmentEffectCondition Condition,
    string Note,
    bool Executed);

/// <summary>Экземпляр улучшения у персонажа: в запасе или на предмете.</summary>
public record CharacterAttachmentDto(
    Guid Id,
    Guid AttachmentDefId,
    string Name,
    string NameRu,
    int HardPointCost,
    bool IsEnchantment,
    int? Price,
    int Rarity,
    Guid? HostCharacterItemId,
    string Note,
    IReadOnlyList<AttachmentEffectDto> Effects);

/// <summary>Покупка улучшения. Сумму считает сервер по цене каталога (ROT-ECO-01).</summary>
public record BuyAttachmentRequest(
    Guid AttachmentDefId,
    bool Free = false,
    int? PriceOverride = null,
    string? OverrideReason = null);

/// <summary>
/// Установка улучшения на предмет. Броска проверки нет — правило книги показывается подсказкой,
/// а результат определяет ведущий за столом (решение владельца).
/// </summary>
/// <param name="OverrideReason">
/// Причина, по которой чары ставит персонаж без ранга магического навыка. Пусто — обычная проверка
/// требования.
/// </param>
public record InstallAttachmentRequest(
    Guid CharacterAttachmentId,
    Guid HostCharacterItemId,
    string? OverrideReason = null);

/// <summary>Чем кончилось снятие улучшения.</summary>
public enum DetachOutcome
{
    /// <summary>Улучшение снято целым и вернулось в запас.</summary>
    Returned = 0,

    /// <summary>Улучшение уничтожено при снятии.</summary>
    Destroyed = 1,

    /// <summary>Улучшение осталось, но больше не годится к установке.</summary>
    Unusable = 2,
}

/// <param name="Outcome">Исход снятия: возвращено, уничтожено или испорчено.</param>
public record DetachAttachmentRequest(DetachOutcome Outcome = DetachOutcome.Returned, string? Note = null);
