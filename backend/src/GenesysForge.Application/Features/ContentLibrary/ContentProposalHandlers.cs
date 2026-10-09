using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Common;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Application.Features.HomebrewPacks;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentLibrary;

public class ProposeContentHandler(IAppDbContext db) : ICommandHandler<ProposeContentCommand, Unit>
{
    public async Task<Unit> Handle(ProposeContentCommand q, CancellationToken ct = default)
    {
        var campaign = await CampaignMapper.GetAccessibleAsync(db, q.UserId, q.CampaignId, ct);
        if (campaign.GmUserId == q.UserId) throw new DomainRuleException("Мастер подключает контент напрямую.");
        var req = q.Request;
        if ((req.PackId != null) == (req.EntryType != null || req.EntryId != null) || (req.PackId == null && (req.EntryType == null || req.EntryId == null)))
            throw new DomainRuleException("Укажите либо набор, либо один элемент.");
        GameSystem system;
        HomebrewPackCampaign? packLink = null;
        CampaignContentItem? itemLink = null;
        if (req.PackId is { } packId)
        {
            var pack = await HomebrewPackMapper.GetOwnedAsync(db, q.UserId, packId, ct);
            system = pack.System;
            packLink = await db.HomebrewPackCampaigns.SingleOrDefaultAsync(x => x.CampaignId == campaign.Id && x.HomebrewPackId == packId, ct);
            if (packLink?.Status == ContentConnectionStatus.Active) throw new DomainRuleException("Набор уже подключён.");
        }
        else
        {
            var def = (await ContentDefinitions.LoadAsync(db, owner: q.UserId, ct: ct)).SingleOrDefault(x => x.Type == req.EntryType && x.Id == req.EntryId)
                ?? throw new DomainRuleException("Свой элемент не найден.");
            system = def.System;
            itemLink = await db.CampaignContentItems.SingleOrDefaultAsync(x => x.CampaignId == campaign.Id && x.EntryType == req.EntryType && x.EntryId == req.EntryId, ct);
            if (itemLink?.Status == ContentConnectionStatus.Active) throw new DomainRuleException("Элемент уже подключён.");
        }
        var closed = await db.CampaignSystemSettings.AnyAsync(x => x.CampaignId == campaign.Id && x.System == system && !x.IsOpen, ct);
        if (closed && !await (from cc in db.CampaignCharacters join c in db.Characters on cc.CharacterId equals c.Id where cc.CampaignId == campaign.Id && c.System == system select c.Id).AnyAsync(ct))
            throw new DomainRuleException("Система закрыта, и в кампании нет её персонажей.");
        if (req.PackId is { } id)
        {
            if (packLink == null)
            {
                packLink = new HomebrewPackCampaign { Id = Guid.NewGuid(), CampaignId = campaign.Id, HomebrewPackId = id };
                db.HomebrewPackCampaigns.Add(packLink);
            }
            packLink.Status = ContentConnectionStatus.Pending; packLink.IsEnabled = false; packLink.ProposedByUserId = q.UserId;
        }
        else
        {
            if (itemLink == null)
            {
                itemLink = new CampaignContentItem { Id = Guid.NewGuid(), CampaignId = campaign.Id, EntryType = req.EntryType!.Value, EntryId = req.EntryId!.Value };
                db.CampaignContentItems.Add(itemLink);
            }
            itemLink.Status = ContentConnectionStatus.Pending; itemLink.IsEnabled = false; itemLink.ProposedByUserId = q.UserId;
        }
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class DecideContentHandler(IAppDbContext db) : ICommandHandler<DecideContentCommand, Unit>
{
    public async Task<Unit> Handle(DecideContentCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAsGmAsync(db, q.UserId, q.CampaignId, ct);
        if (q.Decision != "approve" && q.Decision != "decline") throw new DomainRuleException("Неизвестное решение.");
        var approve = q.Decision == "approve";
        if (q.Kind == "pack")
        {
            var row = await db.HomebrewPackCampaigns.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.HomebrewPackId == q.TargetId, ct)
                ?? throw new DomainRuleException("Предложение не найдено.");
            if (row.Status != ContentConnectionStatus.Pending) throw new DomainRuleException("Предложение уже рассмотрено.");
            row.Status = approve ? ContentConnectionStatus.Active : ContentConnectionStatus.Declined; row.IsEnabled = approve;
        }
        else if (q.Kind == "item")
        {
            var row = await db.CampaignContentItems.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.EntryId == q.TargetId, ct)
                ?? throw new DomainRuleException("Предложение не найдено.");
            if (row.Status != ContentConnectionStatus.Pending) throw new DomainRuleException("Предложение уже рассмотрено.");
            row.Status = approve ? ContentConnectionStatus.Active : ContentConnectionStatus.Declined; row.IsEnabled = approve;
        }
        else throw new DomainRuleException("Неизвестный тип предложения.");
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
public class WithdrawContentHandler(IAppDbContext db) : ICommandHandler<WithdrawContentCommand, Unit>
{
    public async Task<Unit> Handle(WithdrawContentCommand q, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, q.UserId, q.CampaignId, ct);
        if (q.Kind == "pack")
        {
            var row = await db.HomebrewPackCampaigns.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.HomebrewPackId == q.TargetId && x.ProposedByUserId == q.UserId && x.Status == ContentConnectionStatus.Pending, ct)
                ?? throw new DomainRuleException("Своё ожидающее предложение не найдено.");
            db.CampaignPackEntryStates.RemoveRange(await db.CampaignPackEntryStates.Where(x => x.HomebrewPackCampaignId == row.Id).ToListAsync(ct));
            db.HomebrewPackCampaigns.Remove(row);
        }
        else if (q.Kind == "item")
        {
            var row = await db.CampaignContentItems.SingleOrDefaultAsync(x => x.CampaignId == q.CampaignId && x.EntryId == q.TargetId && x.ProposedByUserId == q.UserId && x.Status == ContentConnectionStatus.Pending, ct)
                ?? throw new DomainRuleException("Своё ожидающее предложение не найдено.");
            db.CampaignContentItems.Remove(row);
        }
        else throw new DomainRuleException("Неизвестный тип предложения.");
        await db.SaveChangesAsync(ct); return Unit.Value;
    }
}
