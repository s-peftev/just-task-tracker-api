using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Boards.Enums;
using JustTaskTracker.WebUI.Domain.Boards.Requests;
using JustTaskTracker.WebUI.Domain.Common.Pagination;

namespace JustTaskTracker.WebUI.Services.Abstractions.Boards;

public interface IBoardApiService
{
    Task<PagedList<BoardLookupDto>> GetMyBoardsAsync(GetBoardsForCurrentUserRequest request, CancellationToken ct = default);

    Task<ActiveOwnedBoardsCountDto> GetActiveOwnedBoardsCountAsync(CancellationToken ct = default);

    Task<BoardDetailsDto> GetBoardByIdAsync(Guid boardId, CancellationToken ct = default);

    Task<PagedList<BoardMemberDto>> GetBoardMembersAsync(
        Guid boardId,
        GetBoardMembersRequest request,
        CancellationToken ct = default);

    Task AddBoardMemberAsync(
        Guid boardId,
        AddBoardMemberRequest request,
        CancellationToken ct = default);

    Task UpdateBoardMemberAsync(
        Guid boardId,
        Guid userId,
        UpdateBoardMemberRequest request,
        CancellationToken ct = default);

    Task DeleteBoardMemberAsync(Guid boardId, Guid userId, CancellationToken ct = default);

    Task<BoardDetailsDto> CreateBoardAsync(string name, CancellationToken ct = default);
    Task UpdateBoardAsync(Guid boardId, string name, CancellationToken ct = default);
    Task UpdateColumnAsync(Guid boardId, Guid columnId, string name, CancellationToken ct = default);
    Task DeleteBoardAsync(Guid boardId, CancellationToken ct = default);

    Task LeaveBoardAsync(Guid boardId, CancellationToken ct = default);

    Task<BoardArchivedDto> ArchiveAndExportBoardAsync(
        Guid boardId,
        BoardExportOptions exportOptions,
        CancellationToken ct = default);

    Task<BoardArchiveDownloadDto> GetBoardArchiveDownloadAsync(
        Guid boardId,
        CancellationToken ct = default);

    Task ReExportArchivedBoardAsync(
        Guid boardId,
        BoardExportOptions reExportOptions,
        CancellationToken ct = default);

    Task DeleteColumnAsync(Guid boardId, Guid columnId, DeleteColumnRequest request, CancellationToken ct = default);
    Task<ColumnDto> CreateColumnAsync(Guid boardId, string name, CancellationToken ct = default);

    Task ReorderColumnAsync(
        Guid boardId,
        Guid columnId,
        int position,
        CancellationToken ct = default);

    Task ReorderTaskAsync(
        Guid boardId,
        Guid targetColumnId,
        Guid taskId,
        int position,
        CancellationToken ct = default);

    Task<BoardTaskPreviewDto> CreateTaskAsync(
        Guid boardId,
        Guid columnId,
        string title,
        BoardTaskType type = BoardTaskType.Story,
        CancellationToken ct = default);

    Task<PagedList<BoardTaskLookupDto>> GetBoardTasksLookupAsync(
        Guid boardId,
        Guid columnId,
        GetBoardTasksLookupRequest request,
        CancellationToken ct = default);

    Task<BoardTaskDetailsDto> GetBoardTaskByIdAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        CancellationToken ct = default);

    Task UpdateBoardTaskTitleAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string title,
        CancellationToken ct = default);

    Task UpdateBoardTaskDescriptionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string? description,
        CancellationToken ct = default);

    Task UpdateBoardTaskAssigneeAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid? assigneeId,
        CancellationToken ct = default);

    Task UpdateBoardTaskCompletionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        bool isDone,
        CancellationToken ct = default);

    Task UpdateBoardTaskStoryPointsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        byte? storyPoints,
        CancellationToken ct = default);

    Task UpdateBoardTaskTimeboxAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        short? timeboxHours,
        CancellationToken ct = default);

    Task DeleteBoardTaskAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        CancellationToken ct = default);

    Task<BoardTaskAttachmentDto> UploadBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task DeleteBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken ct = default);

    Task<BoardTaskAttachmentFile> DownloadBoardTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        string fileName,
        CancellationToken ct = default);

    Task<PagedList<BoardTaskCommentDto>> GetBoardTaskCommentsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    Task<BoardTaskCommentDto> CreateBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        string body,
        CancellationToken ct = default);

    Task UpdateBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        string body,
        CancellationToken ct = default);

    Task DeleteBoardTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        CancellationToken ct = default);
}
