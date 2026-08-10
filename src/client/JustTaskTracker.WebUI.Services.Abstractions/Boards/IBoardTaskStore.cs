using JustTaskTracker.WebUI.Domain.Auth;
using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Common.Pagination;

namespace JustTaskTracker.WebUI.Services.Abstractions.Boards;

/// <summary>
/// Scoped store for the board task details modal (single task at a time).
/// </summary>
public interface IBoardTaskStore
{
    Guid? BoardId { get; }
    Guid? ColumnId { get; }
    Guid? TaskId { get; }
    BoardTaskDetailsDto? Task { get; }
    IReadOnlyList<BoardTaskCommentDto> Comments { get; }
    PaginationMetadata CommentsMetadata { get; }
    bool HasMoreComments { get; }
    bool IsLoadingMoreComments { get; }
    bool IsLoading { get; }
    string? ErrorMessage { get; }

    event Action? StateChanged;

    Task LoadAsync(Guid boardId, Guid columnId, Guid taskId, CancellationToken ct = default);

    Task LoadMoreCommentsAsync(CancellationToken ct = default);

    void UpdateTaskTitle(string title);

    void UpdateTaskDescription(string? description);

    void UpdateTaskAssignee(UserDto? assignee);

    void UpdateTaskCompletion(bool isDone, DateTime? completedAtUtc);

    void UpdateTaskStoryPoints(byte? storyPoints);

    void UpdateTaskTimebox(short? timeboxHours);

    void AddAttachment(BoardTaskAttachmentDto attachment);

    void RemoveAttachment(Guid attachmentId);

    void AddComment(BoardTaskCommentDto comment);

    void UpdateComment(Guid commentId, string body, DateTime? lastModifiedAtUtc = null);

    void RemoveComment(Guid commentId);

    void Reset();
}
