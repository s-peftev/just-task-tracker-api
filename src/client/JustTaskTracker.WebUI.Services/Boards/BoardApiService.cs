using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Boards.Enums;
using JustTaskTracker.WebUI.Domain.Boards.Requests;
using JustTaskTracker.WebUI.Services.Abstractions.Boards;
using JustTaskTracker.WebUI.Services.Api;
using JustTaskTracker.WebUI.Domain.Common.Pagination;
using Refit;

namespace JustTaskTracker.WebUI.Services.Boards;

internal class BoardApiService(IBoardApi api) : IBoardApiService
{
    public async Task<PagedList<BoardLookupDto>> GetMyBoardsAsync(GetBoardsForCurrentUserRequest request, CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(request.TextSearchOptions?.Search)
            ? null
            : request.TextSearchOptions.Search;

        var response = await api.GetMyAsync(
            request.PageNumber!.Value,
            request.PageSize!.Value,
            search,
            request.IsArchived,
            request.IsOwned,
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<ActiveOwnedBoardsCountDto> GetActiveOwnedBoardsCountAsync(CancellationToken ct = default)
    {
        var response = await api.GetActiveOwnedCountAsync(ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<BoardDetailsDto> GetBoardByIdAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await api.GetByIdAsync(boardId, ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<PagedList<BoardMemberDto>> GetBoardMembersAsync(
        Guid boardId,
        GetBoardMembersRequest request,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(request.SearchOptions?.Search)
            ? null
            : request.SearchOptions.Search;

        var response = await api.GetMembersAsync(
            boardId,
            request.PageNumber!.Value,
            request.PageSize!.Value,
            search,
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task AddBoardMemberAsync(
        Guid boardId,
        AddBoardMemberRequest request,
        CancellationToken ct = default)
    {
        var response = await api.AddMemberAsync(boardId, request, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardMemberAsync(
        Guid boardId,
        Guid userId,
        UpdateBoardMemberRequest request,
        CancellationToken ct = default)
    {
        var response = await api.UpdateMemberAsync(boardId, userId, request, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task DeleteBoardMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default)
    {
        var response = await api.DeleteMemberAsync(boardId, userId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<BoardDetailsDto> CreateBoardAsync(string name, CancellationToken ct = default)
    {
        var response = await api.CreateAsync(new SaveBoardRequest(name), ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task UpdateBoardAsync(Guid boardId, string name, CancellationToken ct = default)
    {
        var response = await api.UpdateAsync(boardId, new SaveBoardRequest(name), ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateColumnAsync(Guid boardId, Guid columnId, string name, CancellationToken ct = default)
    {
        var response = await api.UpdateColumnAsync(boardId, columnId, new SaveColumnRequest(name), ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task DeleteColumnAsync(Guid boardId, Guid columnId, DeleteColumnRequest request, CancellationToken ct = default)
    {
        var response = await api.DeleteColumnAsync(boardId, columnId, request, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task DeleteBoardAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await api.DeleteAsync(boardId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task LeaveBoardAsync(Guid boardId, CancellationToken ct = default)
    {
        var response = await api.LeaveAsync(boardId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<BoardArchivedDto> ArchiveAndExportBoardAsync(
        Guid boardId,
        BoardExportOptions exportOptions,
        CancellationToken ct = default)
    {
        var response = await api.ArchiveAndExportAsync(boardId, exportOptions, ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<BoardArchiveDownloadDto> GetBoardArchiveDownloadAsync(
        Guid boardId,
        CancellationToken ct = default)
    {
        var response = await api.GetArchiveExportAsync(boardId, ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task ReExportArchivedBoardAsync(
        Guid boardId,
        BoardExportOptions reExportOptions,
        CancellationToken ct = default)
    {
        var response = await api.ReExportArchivedAsync(boardId, reExportOptions, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<ColumnDto> CreateColumnAsync(Guid boardId, string name, CancellationToken ct = default)
    {
        var response = await api.CreateColumnAsync(boardId, new SaveColumnRequest(name), ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task ReorderColumnAsync(
        Guid boardId,
        Guid columnId,
        int position,
        CancellationToken ct = default)
    {
        var response = await api.ReorderColumnAsync(boardId, columnId, new ReorderColumnPositionRequest(position), ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task ReorderTaskAsync(
        Guid boardId,
        Guid targetColumnId,
        Guid taskId,
        int position,
        CancellationToken ct = default)
    {
        var response = await api.ReorderTaskAsync(
            boardId,
            targetColumnId,
            taskId,
            new ReorderTaskPositionRequest(position),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<BoardTaskPreviewDto> CreateTaskAsync(
        Guid boardId,
        Guid columnId,
        string title,
        BoardTaskType type = BoardTaskType.Story,
        CancellationToken ct = default)
    {
        var response = await api.CreateTaskAsync(boardId, columnId, new SaveTaskRequest(title, type), ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<PagedList<BoardTaskLookupDto>> GetBoardTasksLookupAsync(
        Guid boardId,
        Guid columnId,
        GetBoardTasksLookupRequest request,
        CancellationToken ct = default)
    {
        var search = string.IsNullOrWhiteSpace(request.SearchOptions?.Search)
            ? null
            : request.SearchOptions.Search;

        var response = await api.GetTaskLookupListAsync(
            boardId,
            columnId,
            request.PageNumber!.Value,
            request.PageSize!.Value,
            search,
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<BoardTaskDetailsDto> GetBoardTaskByIdAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        CancellationToken ct = default)
    {
        var response = await api.GetTaskByIdAsync(boardId, columnId, taskId, ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task UpdateBoardTaskTitleAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string title,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskTitleAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskTitleRequest(title),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardTaskDescriptionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string? description,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskDescriptionAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskDescriptionRequest(description),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardTaskAssigneeAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid? assigneeId,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskAssigneeAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskAssigneeRequest(assigneeId),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardTaskCompletionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        bool isDone,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskCompletionAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskCompletionRequest(isDone),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardTaskStoryPointsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        byte? storyPoints,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskStoryPointsAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskStoryPointsRequest(storyPoints),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task UpdateBoardTaskTimeboxAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        short? timeboxHours,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskTimeboxAsync(
            boardId,
            columnId,
            taskId,
            new UpdateBoardTaskTimeboxRequest(timeboxHours),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task DeleteBoardTaskAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        CancellationToken ct = default)
    {
        var response = await api.DeleteTaskAsync(boardId, columnId, taskId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<BoardTaskAttachmentDto> UploadBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var streamPart = new StreamPart(content, fileName, contentType);

        var response = await api.UploadTaskAttachmentAsync(
            boardId,
            columnId,
            taskId,
            streamPart,
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task DeleteBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var response = await api.DeleteTaskAttachmentAsync(boardId, columnId, taskId, attachmentId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task<BoardTaskAttachmentFile> DownloadBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        string fileName,
        CancellationToken ct = default)
    {
        using var response = await api.DownloadTaskAttachmentAsync(
            boardId,
            columnId,
            taskId,
            attachmentId,
            ct);

        var (content, contentType) = await ApiResponseGuard.ReadBinarySuccessAsync(response, ct);

        return new BoardTaskAttachmentFile(content, contentType, fileName);
    }

    public async Task<PagedList<BoardTaskCommentDto>> GetBoardTaskCommentsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default)
    {
        var response = await api.GetTaskCommentsAsync(
            boardId,
            columnId,
            taskId,
            pageNumber,
            pageSize,
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task<BoardTaskCommentDto> CreateBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string body,
        CancellationToken ct = default)
    {
        var response = await api.CreateTaskCommentAsync(
            boardId,
            columnId,
            taskId,
            new CreateBoardTaskCommentRequest(body),
            ct);

        return ApiResponseGuard.Unwrap(response);
    }

    public async Task UpdateBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        string body,
        CancellationToken ct = default)
    {
        var response = await api.UpdateTaskCommentAsync(
            boardId,
            columnId,
            taskId,
            commentId,
            new UpdateBoardTaskCommentRequest(body),
            ct);

        ApiResponseGuard.EnsureSuccess(response);
    }

    public async Task DeleteBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        CancellationToken ct = default)
    {
        var response = await api.DeleteTaskCommentAsync(boardId, columnId, taskId, commentId, ct);

        ApiResponseGuard.EnsureSuccess(response);
    }
}
