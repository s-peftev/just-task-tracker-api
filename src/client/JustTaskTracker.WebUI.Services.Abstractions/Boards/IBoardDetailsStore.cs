using JustTaskTracker.WebUI.Domain.Auth;
using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Boards.Enums;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.WebUI.Domain.Boards.Requests;

namespace JustTaskTracker.WebUI.Services.Abstractions.Boards;

/// <summary>
/// Scoped store for a single board details page (columns, tasks, permissions).
/// </summary>
public interface IBoardDetailsStore
{
    Guid? BoardId { get; }
    BoardDetailsDto? Board { get; }
    bool IsLoading { get; }
    string? ErrorMessage { get; }
    bool IsReorderingTasks { get; }
    bool ShowOnlyMyTasks { get; }
    bool IsReadOnly { get; }

    event Action? StateChanged;

    event Action? RemoteBoardNameApplied;

    Task LoadAsync(Guid boardId, CancellationToken ct = default);

    Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default);

    Task<BoardTaskPreviewDto> CreateTaskAsync(
        Guid columnId,
        string title,
        BoardTaskType type = BoardTaskType.Story,
        CancellationToken ct = default);

    void UpdateBoardName(string name);

    void SetBoardArchived(
        DateTime archivedAtUtc,
        BoardExportStatus boardExportStatus,
        BoardExportOptions? exportOptions = null);

    void SetBoardReExportPending(BoardExportOptions reExportOptions);

    void ApplyExportStatusChanged(Guid boardId, BoardExportStatus status);

    void ApplyReExportStatusChanged(
        Guid boardId,
        BoardExportStatus status,
        BoardExportOptions? exportOptions = null);

    void UpdateColumnName(Guid columnId, string name);

    void UpdateTaskTitle(Guid taskId, string title);

    void AdjustTaskCommentsCount(Guid taskId, int delta);

    void AdjustTaskAttachmentsCount(Guid taskId, int delta);

    void UpdateTaskAssignee(Guid taskId, UserDto? assignee);

    void SetShowOnlyMyTasks(bool showOnlyMyTasks);

    Task DeleteColumnAsync(Guid columnId, DeleteColumnRequest request, CancellationToken ct = default);

    Task ReorderColumnAsync(Guid columnId, int position, CancellationToken ct = default);

    Task ReorderTaskAsync(Guid taskId, Guid targetColumnId, int position, CancellationToken ct = default);

    Task DeleteTaskAsync(Guid boardId, Guid columnId, Guid taskId, CancellationToken ct = default);

    void ApplyBoardActionNotification(BoardActionNotification notification, Guid currentUserId);

    void Reset();
}
