using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

public record SetSignatureWeaponUpgradesCommand(
    Guid UserId, Guid CharacterId, SetSignatureWeaponUpgradesRequest Request) : ICommand<Unit>;

/// <summary>
/// Выбор Improved и Supreme именного оружия (ROT-HA-05). Improved даёт ровно одно — Укреплённое
/// либо древнюю работу; Supreme добавляет два слота и одно бесплатное установленное улучшение
/// редкости не выше девяти. Оба выбора фиксируются при покупке и дальше не меняются: это не
/// настройка, а потраченное очко способности.
/// </summary>
public class SetSignatureWeaponUpgradesHandler(IAppDbContext db)
    : ICommandHandler<SetSignatureWeaponUpgradesCommand, Unit>
{
    public async Task<Unit> Handle(SetSignatureWeaponUpgradesCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        if (c.RequiredHeroicParameter != HeroicParameterKind.SignatureWeapon)
            throw new DomainRuleException(
                "У персонажа нет способности с именным оружием.", "heroic.weapon.not_applicable");
        if (c.SignatureWeapon is not { } weapon)
            throw new DomainRuleException("Именное оружие ещё не создано.", "heroic.weapon.missing");

        var req = command.Request;

        if (req.Improvement is { } improvement && improvement != SignatureWeaponImprovement.None)
        {
            if (c.HeroicUpgradeRank < 1)
                throw new DomainRuleException(
                    "Улучшение Improved ещё не куплено.", "heroic.weapon.improvement_not_bought");
            // Выбор фиксируется при покупке: переиграть его позже нельзя даже во время создания —
            // иначе древняя работа и Укреплённое становились бы переключателем.
            if (weapon.Improvement != SignatureWeaponImprovement.None && weapon.Improvement != improvement)
                throw new DomainRuleException(
                    "Выбор улучшения именного оружия уже сделан и не меняется.",
                    "heroic.weapon.improvement_immutable");
            if (!Enum.IsDefined(improvement))
                throw new DomainRuleException(
                    "Неизвестный вариант улучшения.", "heroic.weapon.improvement_unknown");
            weapon.Improvement = improvement;
        }

        if (req.SupremeAttachmentDefId is { } defId)
        {
            if (c.HeroicUpgradeRank < 2)
                throw new DomainRuleException(
                    "Улучшение Supreme ещё не куплено.", "heroic.weapon.supreme_not_bought");
            if (weapon.SupremeAttachmentDefId is { } existing && existing != defId)
                throw new DomainRuleException(
                    "Бесплатное улучшение Supreme уже выбрано и не меняется.",
                    "heroic.weapon.supreme_immutable");

            var visiblePackIds = await HomebrewVisibility.GetVisiblePackIdsAsync(
                db, command.UserId, c.System, c.Id, ct: ct);
            var def = await db.AttachmentDefs.Include(a => a.Effects).FirstOrDefaultAsync(a =>
                a.Id == defId
                && a.System == c.System
                && !a.Retired
                && (a.OwnerUserId == null
                    || (a.OwnerUserId == command.UserId
                        && (a.HomebrewPackId == null || visiblePackIds.Contains(a.HomebrewPackId.Value)))), ct);
            if (def is null)
                throw new DomainRuleException(
                    "Улучшение недоступно персонажу.", "heroic.weapon.attachment_not_available");

            // Слоты считаются уже с прибавкой Supreme и с поправкой выбранной работы: древняя
            // отнимает слот, и бесплатное улучшение должно поместиться в то, что осталось.
            var spec = SignatureWeaponProfiles.Get(weapon.Profile);
            var hardPoints = HeroicParameterRules.HardPoints(
                spec.HardPoints, weapon.EffectiveCraftsmanship, c.HeroicUpgradeRank);
            HeroicParameterRules.EnsureCanBeSupremeAttachment(
                weapon.FormTraits, hardPoints, weapon.BaseAttachment?.Code, def);

            weapon.SupremeAttachmentDefId = def.Id;
            weapon.SupremeAttachment = def;
        }

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.SignatureWeaponReplaced,
            $"Улучшение именного оружия: {weapon.Improvement}",
            data: new
            {
                improvement = weapon.Improvement.ToString(),
                supremeAttachment = weapon.SupremeAttachment?.Code,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
