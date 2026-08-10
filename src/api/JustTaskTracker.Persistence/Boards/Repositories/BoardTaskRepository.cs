using JustTaskTracker.Application.Boards.ReadModels;
using JustTaskTracker.Application.Boards.Repositories;
using JustTaskTracker.Application.Common.Helpers;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Auth.DTOs;
using JustTaskTracker.Domain.Boards.DTOs.Attachments;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;
using JustTaskTracker.Domain.Boards.Entities;
using JustTaskTracker.Domain.Boards.Enums;
using JustTaskTracker.Domain.Boards.Enums.SearchFields;
using JustTaskTracker.Domain.Common.Pagination;
using JustTaskTracker.Domain.Common.Searching;
using JustTaskTracker.Persistence.Common;
using JustTaskTracker.Persistence.Common.Extentions;
using Microsoft.EntityFrameworkCore;

namespace JustTaskTracker.Persistence.Boards.Repositories;

public class BoardTaskRepository(JustTaskTrackerDbContext context)
    : Repository<BoardTask, Guid>(context), IBoardTaskRepository
{
    public async Task<(BoardTask? BoardTask, BoardMemberRole? UserRole)> GetBoardTaskWithUserRoleAsync(Guid boardTaskId, Guid azureAdObjectId, CancellationToken ct = default)
    {
        var result = await _dbSet
            .Where(t => t.Id == boardTaskId)
            .Select(t => new
            {
                BoardTask = t,
                UserRole = t.Column!.Board!.Members
                    .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
                    .Select(m => m.Role)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        return (result?.BoardTask, result?.UserRole);
    }

    public async Task<BoardMemberRole?> GetUserRoleAsync(Guid boardTaskId, Guid azureAdObjectId, CancellationToken ct = default) =>
        await _dbSet
            .Where(task => task.Id == boardTaskId)
            .SelectMany(task => task.Column!.Board!.Members)
            .Where(m => m.User!.AzureAdObjectId == azureAdObjectId)
            .Select(m => m.Role)
            .FirstOrDefaultAsync(ct);

    public Task<bool> ExistsInBoardAsync(Guid boardTaskId, Guid boardId, CancellationToken ct = default) =>
        _dbSet.AnyAsync(t => t.Id == boardTaskId && t.Column!.BoardId == boardId, ct);

    public Task<int> CountExistingInBoardAsync(Guid boardId, IReadOnlyList<Guid> boardTaskIds, CancellationToken ct = default) =>
        boardTaskIds.Count is 0
            ? Task.FromResult(0)
            : _dbSet.CountAsync(t => boardTaskIds.Contains(t.Id) && t.Column!.BoardId == boardId, ct);

    public async Task<BoardTaskDetailsReadModel?> GetBoardTaskDetailsAsync(Guid boardTaskId, CancellationToken ct = default) =>
        await _dbSet
            .Where(task => task.Id == boardTaskId)
            .Select(t => new BoardTaskDetailsReadModel(
                t.Id,
                t.ColumnId,
                t.Column!.Name,
                t.Title,
                t.Position,
                t.CreatedAtUtc,
                new UserReadModel(t.Reporter!.Id, t.Reporter.Email, t.Reporter.DisplayName, t.Reporter.ProfilePhotoVersion),
                default,
                t.Attachments
                    .OrderBy(attachment => attachment.Position)
                    .Select(a => new BoardTaskAttachmentReadModel(
                        a.Id,
                        a.OriginalFileName,
                        a.ContentType,
                        a.FileSizeBytes,
                        a.Position,
                        a.CreatedAtUtc,
                        new UserReadModel(a.UploadedBy!.Id, a.UploadedBy.Email, a.UploadedBy.DisplayName, a.UploadedBy.ProfilePhotoVersion)))
                    .ToList(),
                t.Type,
                t.IsDone,
                t.CompletedAtUtc,
                t.StoryPoints,
                t.TimeboxHours,
                t.Description,
                t.Assignee == null
                    ? null
                    : new UserReadModel(t.Assignee.Id, t.Assignee.Email, t.Assignee.DisplayName, t.Assignee.ProfilePhotoVersion)))
            .FirstOrDefaultAsync(ct);

    public async Task<PagedList<BoardTaskLookupDto>> GetBoardTaskLookupListAsync(
        Guid boardId,
        int pageNumber,
        int pageSize,
        TextSearchOptions<BoardTaskSearchField>? searchOptions = null,
        CancellationToken ct = default)
    {
        var fields = SearchFieldsResolver.Resolve(searchOptions?.SearchIn, BoardTaskSearchFields.Map);

        return await _dbSet
            .Where(t => t.Column!.BoardId == boardId)
            .ApplyTextSearch(searchOptions?.Search, fields)
            .OrderByDescending(t => t.LastModifiedAtUtc)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToPagedAsync(
                t => new BoardTaskLookupDto(
                    t.Id,
                    t.ColumnId,
                    t.Title,
                    t.Description,
                    t.Type),
                pageNumber,
                pageSize,
                ct);
    }

    public async Task<IReadOnlyList<BoardTask>> GetListByColumnIdAsync(Guid columnId, CancellationToken ct = default) =>
        await _dbSet
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BoardTask>> GetListByBoardIdAsync(Guid boardId, CancellationToken ct = default) =>
        await _dbSet
            .Where(t => t.Column!.BoardId == boardId)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);

    public async Task<int> GetCountByColumnIdAsync(Guid columnId, CancellationToken ct = default) =>
        await _dbSet.CountAsync(t => t.ColumnId == columnId, ct);

    public async Task<int> CountByBoardIdAsync(Guid boardId, CancellationToken ct = default) =>
        await _dbSet.CountAsync(t => t.Column!.BoardId == boardId, ct);

    public void RemoveRange(IReadOnlyList<BoardTask> tasks)
    {
        foreach (var task in tasks)
            _dbSet.Remove(task);
    }
}
