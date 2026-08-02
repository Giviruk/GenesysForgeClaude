using GenesysForge.Application.Abstractions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Common;

/// <summary>
/// Базовое улучшение именного оружия (ROT-HA-02): выбирается вместе с формой и живёт только вместе
/// с героической способностью. Экземпляра в инвентаре у него нет — оно ничего не стоит, не занимает
/// слотов и не устанавливается физически, поэтому редкость, цена, проверка чар и ранг магического
/// навыка к нему не применяются. Совместимость считается теми же предикатами, что и обычная
/// установка, по подтверждённым признакам формы.
/// </summary>
public static class SignatureBaseAttachment
{
    /// <summary>
    /// Находит и проверяет выбранное улучшение. Улучшение обязательно: без него именное оружие
    /// не собрано, а угадывать за игрока нельзя.
    /// </summary>
    public static async Task<AttachmentDef> ResolveAsync(
        IAppDbContext db, Character c, Guid userId,
        SignatureWeaponProfile profile, WeaponCraftsmanship craftsmanship, WeaponFormTraits traits,
        Guid? attachmentDefId, CancellationToken ct)
    {
        if (attachmentDefId is not { } defId)
            throw new DomainRuleException(
                "Выберите базовое улучшение именного оружия.", "heroic.weapon.attachment_required");

        // Улучшение должно быть доступно именно этому персонажу: встроенное или собственный кастом
        // той же системы из видимого набора — та же проверка, что у навыка Paragon.
        var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(db, userId, c.System, c.Id, ct: ct);
        var def = await db.AttachmentDefs.Include(a => a.Effects).FirstOrDefaultAsync(a =>
            a.Id == defId
            && a.System == c.System
            && !a.Retired
            && (a.OwnerUserId == null
                || (a.OwnerUserId == userId
                    && (a.HomebrewPackId == null || visiblePackIds.Contains(a.HomebrewPackId.Value)))), ct);
        if (def is null)
            throw new DomainRuleException(
                "Улучшение недоступно персонажу.", "heroic.weapon.attachment_not_available");

        var qualityCodes = SignatureWeaponProfiles.QualitiesFor(profile, craftsmanship)
            .Select(q => q.Code).ToList();
        HeroicParameterRules.EnsureCanBeBaseAttachment(traits, qualityCodes, def);
        return def;
    }
}
