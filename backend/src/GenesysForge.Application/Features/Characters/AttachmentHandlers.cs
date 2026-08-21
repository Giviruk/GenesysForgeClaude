using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using GenesysForge.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Characters;

/// <summary>
/// Покупка улучшения (ROT-EQP-ATT-01). Улучшение попадает в запас персонажа неустановленным:
/// установка — отдельное действие, у которого свои проверки.
/// </summary>
public class BuyAttachmentHandler(IAppDbContext db) : ICommandHandler<BuyAttachmentCommand, Guid>
{
    public async Task<Guid> Handle(BuyAttachmentCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var def = await db.AttachmentDefs.FirstOrDefaultAsync(
                a => a.Id == req.AttachmentDefId && a.System == c.System && a.OwnerUserId == null, ct)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");

        if (req.PriceOverride is not null && string.IsNullOrWhiteSpace(req.OverrideReason))
            throw new DomainRuleException(
                "Для цены, назначенной вручную, нужна причина.", "trade.override_reason_required");
        if (req.PriceOverride is < 0)
            throw new DomainRuleException("Цена не может быть отрицательной.", "trade.price_negative");

        // Бесценное улучшение обычной покупкой не берётся: цену называет ведущий, и она попадает
        // в историю вместе с причиной.
        if (!req.Free && req.PriceOverride is null && def.Price is null)
            throw new DomainRuleException(
                "У этого улучшения нет обычной цены — её назначает ведущий.", "attachment.priceless");

        var unitPrice = req.PriceOverride ?? def.Price ?? 0;
        var total = req.Free ? 0 : unitPrice;

        var charge = MoneyRules.Charge(total, c.Money)
            ?? throw new DomainRuleException(
                $"Недостаточно средств: нужно {total}, доступно {c.Money} монет.",
                "character.funds.insufficient");
        c.Money -= charge;

        var attachment = new CharacterAttachment
        {
            Id = Guid.NewGuid(),
            CharacterId = c.Id,
            AttachmentDefId = def.Id,
            AttachmentDef = def,
            Provenance = req.Free ? ItemProvenance.Imported : ItemProvenance.Purchased,
        };
        db.CharacterAttachments.Add(attachment);
        c.Attachments.Add(attachment);

        var costNote = charge > 0 ? $", −{charge} монет" : "";
        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.AttachmentBought,
            $"Куплено улучшение «{def.Name}»{costNote}", null,
            new
            {
                attachment = def.Name, code = def.Code, cost = charge,
                listedPrice = def.Price, unitPrice, priceOverride = req.PriceOverride,
                overrideReason = req.OverrideReason, free = req.Free,
            });

        await db.SaveChangesAsync(ct);
        return attachment.Id;
    }
}

/// <summary>
/// Установка улучшения (ROT-EQP-ATT-01). Броска проверки нет по решению владельца: правило книги
/// (около часа и Средняя проверка Механики) показывается подсказкой, а исход определяет стол.
/// Всё, что можно проверить, проверяется до изменения состояния.
/// </summary>
public class InstallAttachmentHandler(IAppDbContext db) : ICommandHandler<InstallAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(InstallAttachmentCommand command, CancellationToken ct = default)
    {
        var req = command.Request;
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);

        var attachment = c.Attachments.FirstOrDefault(a => a.Id == req.CharacterAttachmentId)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");
        if (attachment.AttachmentDef is null)
            throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");
        if (attachment.HostCharacterItemId is not null)
            throw new DomainRuleException(
                "Это улучшение уже стоит на предмете.", "attachment.already_installed");

        var host = c.Items.FirstOrDefault(i => i.Id == req.HostCharacterItemId)
            ?? throw new DomainRuleException("Предмет не найден в инвентаре.", "item.not_found");

        var effective = EffectiveItems.For(c, host);
        AttachmentRules.EnsureCanInstall(
            host.ItemDef!.Kind,
            host.ItemDef.FormTraits,
            effective.HardPoints ?? 0,
            [.. effective.Attachments.Select(a => a.AttachmentDef!)],
            attachment.AttachmentDef,
            HasMagicRank(c),
            req.OverrideReason);

        attachment.HostCharacterItemId = host.Id;
        if (!string.IsNullOrWhiteSpace(req.OverrideReason)) attachment.Note = req.OverrideReason.Trim();

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.AttachmentInstalled,
            $"Улучшение «{attachment.AttachmentDef.Name}» установлено на «{host.ItemDef.Name}»", null,
            new
            {
                attachment = attachment.AttachmentDef.Name, code = attachment.AttachmentDef.Code,
                host = host.ItemDef.Name, hardPointCost = attachment.AttachmentDef.HardPointCost,
                enchantment = attachment.AttachmentDef.IsEnchantment,
                overrideReason = req.OverrideReason,
            });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }

    /// <summary>
    /// Есть ли у персонажа хотя бы один ранг магического навыка. Карьерный статус без рангов
    /// не считается: книга требует именно умения, а не доступа к нему.
    /// </summary>
    private static bool HasMagicRank(Character c) => c.Skills
        .Any(s => s.Ranks > 0 && s.SkillDef is { Kind: SkillKind.Magic });
}

/// <summary>
/// Снятие улучшения. Исход указывается явно: обычное снятие возвращает тот же экземпляр в запас
/// и освобождает слот, уничтожение и порча — записываются, а не подразумеваются.
/// </summary>
public class DetachAttachmentHandler(IAppDbContext db) : ICommandHandler<DetachAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(DetachAttachmentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var attachment = c.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");
        if (attachment.HostCharacterItemId is null)
            throw new DomainRuleException(
                "Это улучшение не стоит ни на одном предмете.", "attachment.not_installed");

        var host = c.Items.FirstOrDefault(i => i.Id == attachment.HostCharacterItemId);
        var name = attachment.AttachmentDef?.Name ?? "улучшение";
        var outcome = command.Request.Outcome;
        if (!Enum.IsDefined(outcome))
            throw new DomainRuleException("Неизвестный исход снятия.", "attachment.outcome_unknown");

        attachment.HostCharacterItemId = null;
        if (outcome == DetachOutcome.Destroyed)
        {
            c.Attachments.Remove(attachment);
            db.CharacterAttachments.Remove(attachment);
        }
        else if (outcome == DetachOutcome.Unusable)
        {
            attachment.Note = string.IsNullOrWhiteSpace(command.Request.Note)
                ? "Испорчено при снятии — установка невозможна."
                : command.Request.Note.Trim();
        }
        else if (!string.IsNullOrWhiteSpace(command.Request.Note))
        {
            attachment.Note = command.Request.Note.Trim();
        }

        CharacterAudit.Record(db, c, command.UserId, CharacterAuditAction.AttachmentDetached,
            $"Улучшение «{name}» снято с «{host?.ItemDef?.Name ?? "предмета"}» ({outcome})", null,
            new { attachment = name, host = host?.ItemDef?.Name, outcome = outcome.ToString(), note = command.Request.Note });

        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Удаление улучшения из запаса без выручки — как «убрать» у предмета.</summary>
public class RemoveAttachmentHandler(IAppDbContext db) : ICommandHandler<RemoveAttachmentCommand, Unit>
{
    public async Task<Unit> Handle(RemoveAttachmentCommand command, CancellationToken ct = default)
    {
        var c = await db.GetOwnedAsync(command.UserId, command.CharacterId, ct: ct);
        var attachment = c.Attachments.FirstOrDefault(a => a.Id == command.AttachmentId)
            ?? throw new DomainRuleException("Улучшение не найдено.", "attachment.not_found");
        if (attachment.HostCharacterItemId is not null)
            throw new DomainRuleException(
                "Сначала снимите улучшение с предмета.", "attachment.installed");

        c.Attachments.Remove(attachment);
        db.CharacterAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
