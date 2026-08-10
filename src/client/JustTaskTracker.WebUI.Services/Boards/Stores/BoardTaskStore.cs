using JustTaskTracker.WebUI.Domain.Auth;
using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Common.Pagination;
using JustTaskTracker.WebUI.Services.Abstractions.Boards;
using JustTaskTracker.WebUI.Services.Exceptions;

namespace JustTaskTracker.WebUI.Services.Boards.Stores;

internal sealed class BoardTaskStore(IBoardApiService boardApiService) : IBoardTaskStore
{
    private const int CommentsPageNumber = 1;
    private const int CommentsPageSize = 10;

    private CancellationTokenSource? _loadCts;

    public Guid? BoardId { get; private set; }
    public Guid? ColumnId { get; private set; }
    public Guid? TaskId { get; private set; }
    public BoardTaskDetailsDto? Task { get; private set; }
    public IReadOnlyList<BoardTaskCommentDto> Comments { get; private set; } = [];
    public PaginationMetadata CommentsMetadata { get; private set; } = new();
    public bool HasMoreComments => Comments.Count < CommentsMetadata.TotalCount;
    public bool IsLoadingMoreComments { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }

    public event Action? StateChanged;

    public async Task LoadAsync(Guid boardId, Guid columnId, Guid taskId, CancellationToken ct = default)
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _loadCts = linkedCts;

        BoardId = boardId;
        ColumnId = columnId;
        TaskId = taskId;
        Task = null;
        Comments = [];
        CommentsMetadata = new();
        IsLoadingMoreComments = false;
        IsLoading = true;
        ErrorMessage = null;
        NotifyStateChanged();

        try
        {
            Task = await boardApiService.GetBoardTaskByIdAsync(boardId, columnId, taskId, linkedCts.Token);

            try
            {
                await LoadCommentsPageAsync(boardId, columnId, taskId, CommentsPageNumber, replaceExisting: true, linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                Comments = [];
                CommentsMetadata = new();
            }
        }
        catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
        {
            // Superseded by reset or a newer load.
        }
        catch (ApiServiceException ex)
        {
            Task = null;
            Comments = [];
            CommentsMetadata = new();
            ErrorMessage = ex.Error?.Details is { Count: > 0 } details
                ? string.Join(" ", details)
                : ex.Message;
        }
        catch (Exception ex)
        {
            Task = null;
            Comments = [];
            CommentsMetadata = new();
            ErrorMessage = ex.Message;
        }
        finally
        {
            if (ReferenceEquals(_loadCts, linkedCts))
            {
                IsLoading = false;
                linkedCts.Dispose();
                _loadCts = null;
                NotifyStateChanged();
            }
            else
            {
                linkedCts.Dispose();
            }
        }
    }

    public async Task LoadMoreCommentsAsync(CancellationToken ct = default)
    {
        if (!HasMoreComments
            || IsLoadingMoreComments
            || BoardId is not { } boardId
            || ColumnId is not { } columnId
            || TaskId is not { } taskId)
        {
            return;
        }

        IsLoadingMoreComments = true;
        NotifyStateChanged();

        try
        {
            await LoadCommentsPageAsync(
                boardId,
                columnId,
                taskId,
                CommentsMetadata.CurrentPage + 1,
                replaceExisting: false,
                ct);
        }
        finally
        {
            IsLoadingMoreComments = false;
            NotifyStateChanged();
        }
    }

    public void UpdateTaskTitle(string title)
    {
        if (Task is not { } task)
            return;

        Task = task with { Title = title };
        NotifyStateChanged();
    }

    public void UpdateTaskDescription(string? description)
    {
        if (Task is not { } task)
            return;

        Task = task with { Description = description };
        NotifyStateChanged();
    }

    public void UpdateTaskAssignee(UserDto? assignee)
    {
        if (Task is not { } task)
            return;

        Task = task with { Assignee = assignee };
        NotifyStateChanged();
    }

    public void UpdateTaskCompletion(bool isDone, DateTime? completedAtUtc)
    {
        if (Task is not { } task)
            return;

        Task = task with { IsDone = isDone, CompletedAtUtc = completedAtUtc };
        NotifyStateChanged();
    }

    public void UpdateTaskStoryPoints(byte? storyPoints)
    {
        if (Task is not { } task)
            return;

        Task = task with { StoryPoints = storyPoints };
        NotifyStateChanged();
    }

    public void UpdateTaskTimebox(short? timeboxHours)
    {
        if (Task is not { } task)
            return;

        Task = task with { TimeboxHours = timeboxHours };
        NotifyStateChanged();
    }

    public void AddAttachment(BoardTaskAttachmentDto attachment)
    {
        if (Task is not { } task)
            return;

        var attachments = task.Attachments
            .Append(attachment)
            .OrderBy(a => a.Position)
            .ToList();

        Task = task with { Attachments = attachments };
        NotifyStateChanged();
    }

    public void RemoveAttachment(Guid attachmentId)
    {
        if (Task is not { } task)
            return;

        var attachments = task.Attachments
            .Where(attachment => attachment.Id != attachmentId)
            .OrderBy(attachment => attachment.Position)
            .Select((attachment, index) => attachment with { Position = index })
            .ToList();

        Task = task with { Attachments = attachments };
        NotifyStateChanged();
    }

    public void AddComment(BoardTaskCommentDto comment)
    {
        if (Comments.Any(existing => existing.Id == comment.Id))
            return;

        Comments = Comments
            .Append(comment)
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .ToList();

        CommentsMetadata = new PaginationMetadata
        {
            CurrentPage = CommentsMetadata.CurrentPage,
            PageSize = CommentsMetadata.PageSize,
            TotalCount = CommentsMetadata.TotalCount + 1,
        };

        NotifyStateChanged();
    }

    public void UpdateComment(Guid commentId, string body, DateTime? lastModifiedAtUtc = null)
    {
        var comment = Comments.FirstOrDefault(existing => existing.Id == commentId);

        if (comment is null)
            return;

        var updatedComment = comment with
        {
            Body = body,
            LastModifiedAtUtc = lastModifiedAtUtc ?? DateTime.UtcNow,
        };

        Comments = Comments
            .Select(existing => existing.Id == commentId ? updatedComment : existing)
            .ToList();

        NotifyStateChanged();
    }

    public void RemoveComment(Guid commentId)
    {
        if (Comments.All(comment => comment.Id != commentId))
            return;

        Comments = Comments
            .Where(comment => comment.Id != commentId)
            .ToList();

        CommentsMetadata = new PaginationMetadata
        {
            CurrentPage = CommentsMetadata.CurrentPage,
            PageSize = CommentsMetadata.PageSize,
            TotalCount = Math.Max(0, CommentsMetadata.TotalCount - 1),
        };

        NotifyStateChanged();
    }

    public void Reset()
    {
        _loadCts?.Cancel();
        _loadCts?.Dispose();
        _loadCts = null;

        BoardId = null;
        ColumnId = null;
        TaskId = null;
        Task = null;
        Comments = [];
        CommentsMetadata = new();
        IsLoadingMoreComments = false;
        IsLoading = false;
        ErrorMessage = null;
        NotifyStateChanged();
    }

    private async Task LoadCommentsPageAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        int pageNumber,
        bool replaceExisting,
        CancellationToken ct)
    {
        var paged = await boardApiService.GetBoardTaskCommentsAsync(
            boardId,
            columnId,
            taskId,
            pageNumber,
            CommentsPageSize,
            ct);

        Comments = replaceExisting
            ? paged.Items.ToList()
            : MergeComments(Comments, paged.Items);

        CommentsMetadata = paged.Metadata;
    }

    private static IReadOnlyList<BoardTaskCommentDto> MergeComments(
        IReadOnlyList<BoardTaskCommentDto> existing,
        IReadOnlyList<BoardTaskCommentDto> incoming) =>
        existing
            .Concat(incoming)
            .GroupBy(comment => comment.Id)
            .Select(group => group.First())
            .OrderByDescending(comment => comment.CreatedAtUtc)
            .ThenByDescending(comment => comment.Id)
            .ToList();

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
