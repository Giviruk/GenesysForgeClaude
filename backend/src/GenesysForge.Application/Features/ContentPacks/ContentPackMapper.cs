using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Features.Campaigns;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.ContentPacks;

/// <summary>Загрузка Content Pack, проверки доступа и сборка DTO с учётом роли (см. §8, §10).</summary>
public static class ContentPackMapper
{
    public static async Task<ContentPack> LoadAsync(
        IAppDbContext db, Guid id, CancellationToken ct, bool tracking = false)
    {
        var query = db.ContentPacks.Include(p => p.Entries).AsQueryable();
        if (!tracking) query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new DomainRuleException("Content Pack не найден.");
    }

    public static async Task<(ContentPack Pack, Campaign Campaign)> GetAsGmAsync(
        IAppDbContext db, Guid userId, Guid id, CancellationToken ct, bool tracking = false)
    {
        var pack = await LoadAsync(db, id, ct, tracking);
        var campaign = await CampaignMapper.GetAsGmAsync(db, userId, pack.CampaignId, ct);
        return (pack, campaign);
    }

    /// <summary>Pack, доступный пользователю на чтение: GM всегда; игрок — только если опубликован.</summary>
    public static async Task<(ContentPack Pack, bool IsGm)> GetViewableAsync(
        IAppDbContext db, Guid userId, Guid id, CancellationToken ct)
    {
        var pack = await LoadAsync(db, id, ct);
        var campaign = await CampaignMapper.GetAccessibleAsync(db, userId, pack.CampaignId, ct);
        var isGm = campaign.GmUserId == userId;
        if (!isGm && !pack.IsPublicToCampaign)
            throw new DomainRuleException("Content Pack не найден.");
        return (pack, isGm);
    }

    public static ContentPackListItemDto ToListItem(ContentPack p) =>
        new(p.Id, p.Name, p.System, p.IsPublicToCampaign, p.Entries.Count, p.UpdatedAt);

    public static ContentPackDetailDto ToDetail(ContentPack p, bool isGm) => new(
        p.Id, p.CampaignId, p.Name, p.Description, p.System, isGm, p.IsPublicToCampaign,
        p.Entries.OrderBy(e => e.SortOrder).ThenBy(e => e.Title)
            .Select(e => ToEntryDto(e, isGm)).ToList(),
        p.CreatedAt, p.UpdatedAt);

    public static ContentPackEntryDto ToEntryDto(ContentPackEntry e, bool isGm) => new(
        e.Id, e.ContentType, e.ContentId, e.Title, e.AllowedState, e.Category,
        e.SafeSummary, e.Source, e.PageRef, isGm ? e.GmNotes : null, e.PlayerNotes, e.Tags, e.SortOrder);

    public static void ApplyEntry(ContentPackEntry e, ContentPackEntryInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            throw new DomainRuleException("Название записи не может быть пустым.");

        e.ContentType = input.ContentType;
        e.ContentId = input.ContentId;
        e.Title = input.Title.Trim();
        e.AllowedState = input.AllowedState;
        e.Category = input.ContentType == ContentEntryType.HouseRule
            ? (input.Category ?? HouseRuleCategory.Custom)
            : HouseRuleCategory.None;
        e.SafeSummary = input.SafeSummary?.Trim() ?? "";
        e.Source = input.Source?.Trim() ?? "";
        e.PageRef = input.PageRef?.Trim() ?? "";
        e.GmNotes = input.GmNotes?.Trim() ?? "";
        e.PlayerNotes = input.PlayerNotes?.Trim() ?? "";
        e.Tags = (input.Tags ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
    }
}
