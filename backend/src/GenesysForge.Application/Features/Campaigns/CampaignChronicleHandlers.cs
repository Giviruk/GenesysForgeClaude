using GenesysForge.Application.Abstractions;
using GenesysForge.Application.Dtos;
using GenesysForge.Application.Exceptions;
using GenesysForge.Domain;
using GenesysForge.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GenesysForge.Application.Features.Campaigns;

public record GetCampaignChronicleQuery(Guid UserId, Guid CampaignId)
    : IQuery<List<CampaignChronicleChapterDto>>;

public record CreateCampaignChronicleChapterCommand(
    Guid UserId, Guid CampaignId, SaveCampaignChronicleChapterRequest Request)
    : ICommand<CampaignChronicleChapterDto>;

public record UpdateCampaignChronicleChapterCommand(
    Guid UserId, Guid CampaignId, Guid ChapterId, SaveCampaignChronicleChapterRequest Request)
    : ICommand<CampaignChronicleChapterDto>;

public record GetCampaignChronicleHistoryQuery(Guid UserId, Guid CampaignId, Guid ChapterId)
    : IQuery<List<CampaignChronicleRevisionDto>>;

public record RestoreCampaignChronicleRevisionCommand(
    Guid UserId, Guid CampaignId, Guid ChapterId, Guid RevisionId)
    : ICommand<CampaignChronicleChapterDto>;

internal static class CampaignChronicleMapping
{
    public const int MaxContentLength = 200_000;

    public static void Validate(SaveCampaignChronicleChapterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new DomainRuleException("Название главы не может быть пустым.");
        if (request.Title.Trim().Length > 200)
            throw new DomainRuleException("Название главы не может быть длиннее 200 символов.");
        if ((request.Content ?? "").Length > MaxContentLength)
            throw new DomainRuleException("Текст главы не может быть длиннее 200 000 символов.");
    }

    public static CampaignChronicleChapterDto ToDto(CampaignChronicleChapter chapter, string editor) =>
        new(chapter.Id, chapter.Title, chapter.Content, chapter.SortOrder, chapter.CurrentVersion,
            chapter.CreatedAt, chapter.UpdatedAt, editor);
}

public class GetCampaignChronicleHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignChronicleQuery, List<CampaignChronicleChapterDto>>
{
    public async Task<List<CampaignChronicleChapterDto>> Handle(
        GetCampaignChronicleQuery query, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);

        return await db.CampaignChronicleChapters.AsNoTracking()
            .Where(x => x.CampaignId == query.CampaignId)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.CreatedAt)
            .Join(db.Users.AsNoTracking(), chapter => chapter.UpdatedByUserId, user => user.Id,
                (chapter, user) => new CampaignChronicleChapterDto(
                    chapter.Id, chapter.Title, chapter.Content, chapter.SortOrder, chapter.CurrentVersion,
                    chapter.CreatedAt, chapter.UpdatedAt, user.DisplayName))
            .ToListAsync(ct);
    }
}

public class CreateCampaignChronicleChapterHandler(IAppDbContext db)
    : ICommandHandler<CreateCampaignChronicleChapterCommand, CampaignChronicleChapterDto>
{
    public async Task<CampaignChronicleChapterDto> Handle(
        CreateCampaignChronicleChapterCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        CampaignChronicleMapping.Validate(command.Request);

        var nextOrder = (await db.CampaignChronicleChapters
            .Where(x => x.CampaignId == command.CampaignId)
            .MaxAsync(x => (int?)x.SortOrder, ct) ?? -1) + 1;
        var now = DateTime.UtcNow;
        var chapter = new CampaignChronicleChapter
        {
            Id = Guid.NewGuid(),
            CampaignId = command.CampaignId,
            Title = command.Request.Title.Trim(),
            Content = command.Request.Content ?? "",
            SortOrder = nextOrder,
            CurrentVersion = 1,
            CreatedByUserId = command.UserId,
            UpdatedByUserId = command.UserId,
            CreatedAt = now,
            UpdatedAt = now,
        };
        chapter.Revisions.Add(new CampaignChronicleRevision
        {
            Id = Guid.NewGuid(),
            ChapterId = chapter.Id,
            Version = 1,
            Title = chapter.Title,
            Content = chapter.Content,
            EditedByUserId = command.UserId,
            EditedAt = now,
        });
        db.CampaignChronicleChapters.Add(chapter);
        await db.SaveChangesAsync(ct);
        var editor = await db.Users.Where(x => x.Id == command.UserId).Select(x => x.DisplayName).SingleAsync(ct);
        return CampaignChronicleMapping.ToDto(chapter, editor);
    }
}

public class UpdateCampaignChronicleChapterHandler(IAppDbContext db)
    : ICommandHandler<UpdateCampaignChronicleChapterCommand, CampaignChronicleChapterDto>
{
    public async Task<CampaignChronicleChapterDto> Handle(
        UpdateCampaignChronicleChapterCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        CampaignChronicleMapping.Validate(command.Request);
        var chapter = await db.CampaignChronicleChapters.FirstOrDefaultAsync(
                x => x.Id == command.ChapterId && x.CampaignId == command.CampaignId, ct)
            ?? throw new DomainRuleException("Глава хроники не найдена.");
        if (command.Request.ExpectedVersion is { } expected && chapter.CurrentVersion != expected)
            throw new ConflictException("Глава уже изменена другим участником. Обновите хронику и повторите правку.");
        var title = command.Request.Title.Trim();
        var content = command.Request.Content ?? "";
        var editor = await db.Users.Where(x => x.Id == command.UserId).Select(x => x.DisplayName).SingleAsync(ct);
        if (chapter.Title == title && chapter.Content == content)
            return CampaignChronicleMapping.ToDto(chapter, editor);

        var now = DateTime.UtcNow;
        chapter.Title = title;
        chapter.Content = content;
        chapter.CurrentVersion++;
        chapter.UpdatedByUserId = command.UserId;
        chapter.UpdatedAt = now;
        db.CampaignChronicleRevisions.Add(new CampaignChronicleRevision
        {
            Id = Guid.NewGuid(), ChapterId = chapter.Id, Version = chapter.CurrentVersion,
            Title = title, Content = content, EditedByUserId = command.UserId, EditedAt = now,
        });
        await db.SaveChangesAsync(ct);
        return CampaignChronicleMapping.ToDto(chapter, editor);
    }
}

public class GetCampaignChronicleHistoryHandler(IAppDbContext db)
    : IQueryHandler<GetCampaignChronicleHistoryQuery, List<CampaignChronicleRevisionDto>>
{
    public async Task<List<CampaignChronicleRevisionDto>> Handle(
        GetCampaignChronicleHistoryQuery query, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, query.UserId, query.CampaignId, ct);
        var exists = await db.CampaignChronicleChapters.AnyAsync(
            x => x.Id == query.ChapterId && x.CampaignId == query.CampaignId, ct);
        if (!exists) throw new DomainRuleException("Глава хроники не найдена.");

        return await db.CampaignChronicleRevisions.AsNoTracking()
            .Where(x => x.ChapterId == query.ChapterId)
            .OrderByDescending(x => x.Version)
            .Join(db.Users.AsNoTracking(), revision => revision.EditedByUserId, user => user.Id,
                (revision, user) => new CampaignChronicleRevisionDto(
                    revision.Id, revision.Version, revision.Title, revision.Content,
                    revision.EditedAt, user.DisplayName))
            .ToListAsync(ct);
    }
}

public class RestoreCampaignChronicleRevisionHandler(IAppDbContext db)
    : ICommandHandler<RestoreCampaignChronicleRevisionCommand, CampaignChronicleChapterDto>
{
    public async Task<CampaignChronicleChapterDto> Handle(
        RestoreCampaignChronicleRevisionCommand command, CancellationToken ct = default)
    {
        await CampaignMapper.GetAccessibleAsync(db, command.UserId, command.CampaignId, ct);
        var chapter = await db.CampaignChronicleChapters.FirstOrDefaultAsync(
                x => x.Id == command.ChapterId && x.CampaignId == command.CampaignId, ct)
            ?? throw new DomainRuleException("Глава хроники не найдена.");
        var source = await db.CampaignChronicleRevisions.AsNoTracking().FirstOrDefaultAsync(
                x => x.Id == command.RevisionId && x.ChapterId == command.ChapterId, ct)
            ?? throw new DomainRuleException("Версия хроники не найдена.");

        var now = DateTime.UtcNow;
        chapter.Title = source.Title;
        chapter.Content = source.Content;
        chapter.CurrentVersion++;
        chapter.UpdatedByUserId = command.UserId;
        chapter.UpdatedAt = now;
        db.CampaignChronicleRevisions.Add(new CampaignChronicleRevision
        {
            Id = Guid.NewGuid(), ChapterId = chapter.Id, Version = chapter.CurrentVersion,
            Title = chapter.Title, Content = chapter.Content,
            EditedByUserId = command.UserId, EditedAt = now,
        });
        await db.SaveChangesAsync(ct);
        var editor = await db.Users.Where(x => x.Id == command.UserId).Select(x => x.DisplayName).SingleAsync(ct);
        return CampaignChronicleMapping.ToDto(chapter, editor);
    }
}
