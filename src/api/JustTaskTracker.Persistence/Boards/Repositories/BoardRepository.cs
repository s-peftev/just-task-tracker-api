using JustTaskTracker.Application.Boards.ReadModels;
using JustTaskTracker.Application.Boards.Repositories;
using JustTaskTracker.Application.Common.Helpers;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.DTOs.Archiving;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;
using JustTaskTracker.Domain.Boards.DTOs.Boards;
using JustTaskTracker.Domain.Boards.DTOs.Columns;
using JustTaskTracker.Domain.Boards.Entities;
using JustTaskTracker.Domain.Boards.Enums;
using JustTaskTracker.Domain.Boards.Enums.SearchFields;
using JustTaskTracker.Domain.Common.Pagination;
using JustTaskTracker.Domain.Common.Searching;
using JustTaskTracker.Persistence.Common;
using JustTaskTracker.Persistence.Common.Extentions;
using Microsoft.EntityFrameworkCore;

namespace JustTaskTracker.Persistence.Boards.Repositories;

public class BoardRepository(JustTaskTrackerDbContext context)
    : Repository<Board, Guid>(context), IBoardRepository
{
    public void AddMember(BoardMember member) =>
        _context.BoardMembers.Add(member);

    public void RemoveMember(BoardMember member) =>
        _context.BoardMembers.Remove(member);

    public async Task<BoardMember?> GetMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default) =>
        await _context.BoardMembers
            .FirstOrDefaultAsync(member => member.BoardId == boardId && member.UserId == userId, ct);

    public async Task<BoardMember?> GetMemberByAzureAOIAsync(Guid boardId, Guid azureAdObjectId, CancellationToken ct = default) =>
        await _context.BoardMembers
            .FirstOrDefaultAsync(
                m => m.BoardId == boardId && m.User!.AzureAdObjectId == azureAdObjectId,
                ct);

    public async Task<BoardMemberRole?> GetUserRoleAsync(Guid boardId, Guid azureAdObjectId, CancellationToken ct = default) =>
        await _dbSet
            .Where(b => b.Id == boardId)
            .SelectMany(b => b.Members)
            .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync(ct);

    public async Task<(Board? Board, BoardMemberRole? UserRole)> GetBoardWithUserRoleAsync(Guid boardId, Guid azureAdObjectId, CancellationToken ct = default)
    {
        var result = await _dbSet
            .Where(b => b.Id == boardId)
            .Select(b => new
            {
                Board = b,
                UserRole = b.Members
                    .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
                    .Select(m => m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return (result?.Board, result?.UserRole);
    }

    public async Task<BoardDetailsReadModel?> GetBoardDetailsByIdAsync(Guid boardId, Guid azureAdObjectId, CancellationToken ct = default) =>
        await _dbSet
            .Where(b => b.Id == boardId)
            .Select(b => new BoardDetailsReadModel(
                b.Id,
                b.Name,
                b.CreatedAtUtc,
                b.IsArchived,
                b.Members
                    .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
                    .Select(m => m.Role)
                    .First(),
                b.Columns
                    .OrderBy(c => c.Position)
                    .Select(c => new ColumnDto(
                        c.Id,
                        c.Name,
                        c.Position,
                        c.Tasks
                            .OrderBy(t => t.Position)
                            .Select(t => new BoardTaskPreviewDto(
                                t.Id,
                                t.Title,
                                t.Position,
                                t.Comments.Count,
                                t.Attachments.Count,
                                t.AssigneeId,
                                t.Type,
                                t.IsDone,
                                t.StoryPoints,
                                t.TimeboxHours)))),
                b.Members
                    .Where(m => m.Role == BoardMemberRole.Owner)
                    .Select(m => (Guid?)m.UserId)
                    .FirstOrDefault(),
                b.ArchivedAtUtc))
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

    public async Task<PagedList<BoardLookupReadModel>> GetBoardsByUserAzureAOIAsync(
        Guid azureAdObjectId,
        int pageNumber,
        int pageSize,
        TextSearchOptions<BoardSearchField>? searchOptions = null,
        bool? isArchived = null,
        bool? isOwned = null,
        CancellationToken ct = default)
    {
        var fields = SearchFieldsResolver.Resolve(searchOptions?.SearchIn, BoardSearchFields.Map);

        return await _dbSet
            .Where(b => b.Members.Any(m => m.User!.AzureAdObjectId == azureAdObjectId))
            .Where(b => isArchived == null || b.IsArchived == isArchived)
            .Where(b => isOwned == null
                || (isOwned.Value
                    ? b.Members.Any(m =>
                        m.User!.AzureAdObjectId == azureAdObjectId
                        && m.Role == BoardMemberRole.Owner)
                    : b.Members.Any(m =>
                        m.User!.AzureAdObjectId == azureAdObjectId
                        && m.Role != BoardMemberRole.Owner)))
            .ApplyTextSearch(searchOptions?.Search, fields)
            .Select(b => new
            {
                Board = b,
                BoardActivity = b.LastModifiedAtUtc ?? b.CreatedAtUtc,

                ColumnsActivity = b.Columns
                    .Select(c => (DateTime?)(c.LastModifiedAtUtc ?? c.CreatedAtUtc))
                    .Max(),

                TasksActivity = b.Columns
                    .SelectMany(c => c.Tasks)
                    .Select(t => (DateTime?)(t.LastModifiedAtUtc ?? t.CreatedAtUtc))
                    .Max()
            })
            .Select(x => new
            {
                x.Board,
                x.TasksActivity,
                // Most recent activity of the board itself or any of its columns (null columns activity loses the comparison)
                BoardOrColumnsActivity = x.ColumnsActivity > x.BoardActivity ? x.ColumnsActivity.Value : x.BoardActivity
            })
            .Select(x => new
            {
                x.Board,
                // Fold task activity into the running max to get the overall most recent activity
                LastActivity = x.TasksActivity > x.BoardOrColumnsActivity ? x.TasksActivity.Value : x.BoardOrColumnsActivity
            })
            .OrderByDescending(x => x.LastActivity)
            .ToPagedAsync(
                x => new BoardLookupReadModel(
                    x.Board.Id,
                    x.Board.Name,
                    x.Board.IsArchived,
                    x.Board.Members
                        .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
                        .Select(m => m.Role)
                        .First(),
                    x.Board.Members
                        .Where(m => m.Role == BoardMemberRole.Owner)
                        .Select(m => m.User!.Email)
                        .First(),
                    x.Board.Members
                        .Where(m => m.Role == BoardMemberRole.Owner)
                        .Select(m => m.User!.DisplayName)
                        .FirstOrDefault(),
                    x.Board.ArchivedAtUtc),
                pageNumber,
                pageSize,
                ct);
    }

    public async Task<bool> IsBoardMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default) =>
        await _context.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == userId, ct);

    public Task<int> CountBoardMembersAsync(Guid boardId, IReadOnlyList<Guid> userIds, CancellationToken ct = default) =>
        userIds.Count is 0
            ? Task.FromResult(0)
            : _context.BoardMembers.CountAsync(m => m.BoardId == boardId && userIds.Contains(m.UserId), ct);

    public async Task<bool> IsArchivedAsync(Guid boardId, CancellationToken ct = default) =>
        await _dbSet.AnyAsync(b => b.Id == boardId && b.IsArchived, ct);

    public async Task<Guid?> GetOwnerUserIdAsync(Guid boardId, CancellationToken ct = default) =>
        await _context.BoardMembers
            .Where(m => m.BoardId == boardId && m.Role == BoardMemberRole.Owner)
            .Select(m => (Guid?)m.UserId)
            .FirstOrDefaultAsync(ct);

    public async Task<int> CountActiveOwnedBoardsByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.BoardMembers
            .CountAsync(
                m => m.UserId == userId
                     && m.Role == BoardMemberRole.Owner
                     && !m.Board!.IsArchived,
                ct);

    public async Task<int> CountMembersByBoardIdAsync(Guid boardId, CancellationToken ct = default) =>
        await _context.BoardMembers.CountAsync(m => m.BoardId == boardId, ct);

    public async Task<IReadOnlyDictionary<Guid, BoardMemberRole>> GetUserRolesForArchivedBoardsAsync(IReadOnlyList<Guid> boardIds, Guid azureAdObjectId, CancellationToken ct = default)
    {
        if (boardIds.Count is 0)
            return new Dictionary<Guid, BoardMemberRole>();

        return await _context.BoardMembers
            .Where(m => boardIds.Contains(m.BoardId)
                && m.User!.AzureAdObjectId == azureAdObjectId
                && m.Board!.IsArchived)
            .Select(m => new { m.BoardId, m.Role })
            .ToDictionaryAsync(x => x.BoardId, x => x.Role, ct);
    }

    public async Task<PagedList<BoardMemberReadModel>> GetMembersInfoPagedAsync(
        Guid boardId,
        int pageNumber,
        int pageSize,
        TextSearchOptions<BoardMemberSearchField>? searchOptions = null,
        CancellationToken ct = default)
    {
        var fields = SearchFieldsResolver.Resolve(searchOptions?.SearchIn, BoardMemberSearchFields.Map);

        return await _context.BoardMembers
            .Where(member => member.BoardId == boardId)
            .ApplyTextSearch(searchOptions?.Search, fields)
            .OrderBy(member => member.Role)
            .ThenBy(member => member.JoinedAtUtc)
            .ThenBy(member => member.UserId)
            .ToPagedAsync(
                member => new BoardMemberReadModel(
                    new UserReadModel(
                        member.User!.Id,
                        member.User.Email,
                        member.User.DisplayName,
                        member.User.ProfilePhotoVersion),
                    member.User.GlobalRoles.Select(r => r.Role).ToList(),
                    member.Role,
                    member.JoinedAtUtc),
                pageNumber,
                pageSize,
                ct);
    }

    public async Task<IReadOnlyList<BoardMemberIdentity>> GetMemberIdentitiesAsync(Guid boardId, CancellationToken ct = default) =>
        await _context.BoardMembers
            .Where(member => member.BoardId == boardId)
            .Select(member => new BoardMemberIdentity(member.UserId, member.User!.AzureAdObjectId, member.Role))
            .ToListAsync(ct);

    public async Task<BoardExportRawData?> GetBoardExportRawDataAsync(Guid boardId, BoardExportOptions options, CancellationToken ct = default)
    {
        var board = await _dbSet
            .Where(b => b.Id == boardId && b.IsArchived)
            .Select(b => new BoardExportBoardDto(
                b.Id,
                b.Name,
                b.CreatedAtUtc,
                b.IsArchived,
                b.ArchivedAtUtc,
                b.Columns.Count,
                b.Columns.SelectMany(c => c.Tasks).Count()))
            .FirstOrDefaultAsync(ct);

        if (board is null)
            return null;

        var columnData = await _context.Columns
            .Where(c => c.BoardId == boardId)
            .OrderBy(c => c.Position)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Position,
                Tasks = c.Tasks
                    .OrderBy(t => t.Position)
                    .Select(t => new
                    {
                        t.Id,
                        t.Title,
                        t.Position,
                        t.CreatedAtUtc,
                        t.LastModifiedAtUtc,
                        t.Description,
                        Reporter = new BoardExportUserDto(
                            t.Reporter!.Id,
                            t.Reporter.Email,
                            t.Reporter.DisplayName),
                        Assignee = t.Assignee == null
                            ? null
                            : new BoardExportUserDto(
                                t.Assignee.Id,
                                t.Assignee.Email,
                                t.Assignee.DisplayName)
                    })
                    .ToList()
            })
            .ToListAsync(ct);

        Dictionary<Guid, List<BoardExportCommentDto>> commentsByTask = [];

        if (options.IncludeComments)
        {
            var comments = await _context.BoardTaskComments
                .Where(c => c.BoardTask!.Column!.BoardId == boardId)
                .OrderBy(c => c.CreatedAtUtc)
                .Select(c => new
                {
                    c.BoardTaskId,
                    Comment = new BoardExportCommentDto(
                        c.Id,
                        c.Body,
                        c.CreatedAtUtc,
                        c.LastModifiedAtUtc,
                        new BoardExportUserDto(c.Author!.Id, c.Author.Email, c.Author.DisplayName))
                })
                .ToListAsync(ct);

            commentsByTask = comments
                .GroupBy(x => x.BoardTaskId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Comment).ToList());
        }

        Dictionary<Guid, List<BoardExportRawAttachmentData>> attachmentsByTask = [];

        if (options.IncludeAttachments)
        {
            var attachments = await _context.BoardTaskAttachments
                .Where(a => a.BoardTask!.Column!.BoardId == boardId)
                .OrderBy(a => a.Position)
                .Select(a => new
                {
                    a.BoardTaskId,
                    Attachment = new BoardExportRawAttachmentData(
                        a.Id,
                        a.OriginalFileName,
                        a.ContentType,
                        a.FileSizeBytes,
                        a.Position,
                        a.CreatedAtUtc,
                        new BoardExportUserDto(a.UploadedBy!.Id, a.UploadedBy.Email, a.UploadedBy.DisplayName),
                        a.BlobName)
                })
                .ToListAsync(ct);

            attachmentsByTask = attachments
                .GroupBy(x => x.BoardTaskId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Attachment).ToList());
        }

        List<BoardExportMemberDto>? members = null;

        if (options.IncludeMembers)
        {
            members = await _context.BoardMembers
                .Where(m => m.BoardId == boardId)
                .OrderBy(m => m.Role)
                .ThenBy(m => m.JoinedAtUtc)
                .Select(m => new BoardExportMemberDto(
                    new BoardExportUserDto(m.User!.Id, m.User.Email, m.User.DisplayName),
                    m.Role.ToString(),
                    m.JoinedAtUtc))
                .ToListAsync(ct);
        }

        var rawColumns = columnData
            .Select(c => new BoardExportRawColumnData(
                c.Id,
                c.Name,
                c.Position,
                c.Tasks
                    .Select(t => new BoardExportRawTaskData(
                        t.Id,
                        t.Title,
                        t.Position,
                        t.CreatedAtUtc,
                        t.LastModifiedAtUtc,
                        t.Reporter,
                        t.Assignee,
                        options.IncludeDescriptions ? t.Description : null,
                        commentsByTask.TryGetValue(t.Id, out var tc) ? tc : options.IncludeComments ? [] : null,
                        attachmentsByTask.TryGetValue(t.Id, out var ta) ? ta : options.IncludeAttachments ? [] : null))
                    .ToList()))
            .ToList();

        return new BoardExportRawData(board, rawColumns, members);
    }
}