using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Boards.Enums;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads.Positions;
using JustTaskTracker.WebUI.Domain.Boards.Requests;
using JustTaskTracker.WebUI.Services.Abstractions.Boards;
using JustTaskTracker.WebUI.Services.Exceptions;
using Microsoft.Extensions.Logging;

namespace JustTaskTracker.WebUI.Services.Boards.Stores;

internal sealed class BoardDetailsStore(
    IBoardApiService boardApiService,
    IBoardActionSyncGuard syncGuard,
    ILogger<BoardDetailsStore> logger) : IBoardDetailsStore
{
    public Guid? BoardId { get; private set; }
    public BoardDetailsDto? Board { get; private set; }
    public bool IsLoading { get; private set; }
    public string? ErrorMessage { get; private set; }
    public bool IsReorderingTasks { get; private set; }
    public bool ShowOnlyMyTasks { get; private set; }
    public bool IsReadOnly => Board?.IsArchived == true;

    public event Action? StateChanged;

    public event Action? RemoteBoardNameApplied;

    public async Task LoadAsync(Guid boardId, CancellationToken ct = default)
    {
        BoardId = boardId;
        IsLoading = true;
        ErrorMessage = null;
        syncGuard.Reset();
        NotifyStateChanged();

        try
        {
            Board = await boardApiService.GetBoardByIdAsync(boardId, ct);
        }
        catch (ApiServiceException ex)
        {
            Board = null;
            ErrorMessage = ex.Error?.Details is { Count: > 0 } details
                ? string.Join(" ", details)
                : ex.Message;
        }
        catch (Exception ex)
        {
            Board = null;
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<ColumnDto> CreateColumnAsync(string name, CancellationToken ct = default)
    {
        if (BoardId is not { } boardId || Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        var column = await boardApiService.CreateColumnAsync(boardId, name, ct);
        AddColumn(column);

        return column;
    }

    public async Task<BoardTaskPreviewDto> CreateTaskAsync(
        Guid columnId,
        string title,
        BoardTaskType type = BoardTaskType.Story,
        CancellationToken ct = default)
    {
        if (BoardId is not { } boardId || Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        var task = await boardApiService.CreateTaskAsync(boardId, columnId, title, type, ct);
        AddTask(columnId, task);

        return task;
    }

    public async Task DeleteColumnAsync(Guid columnId, DeleteColumnRequest request, CancellationToken ct = default)
    {
        if (BoardId is not { } boardId || Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        var column = Board.Columns.FirstOrDefault(c => c.Id == columnId)
            ?? throw new InvalidOperationException("Column was not found in the loaded board.");

        await boardApiService.DeleteColumnAsync(boardId, columnId, request, ct);
        ApplyColumnDeletion(column, request);
    }

    public void UpdateBoardName(string name)
    {
        if (Board is null)
            return;

        Board = Board with { Name = name };
        NotifyStateChanged();
    }

    public void SetBoardArchived(
        DateTime archivedAtUtc,
        BoardExportStatus boardExportStatus,
        BoardExportOptions? exportOptions = null)
    {
        if (Board is null)
            return;

        Board = Board with
        {
            IsArchived = true,
            ArchivedAtUtc = archivedAtUtc,
            BoardExportStatus = boardExportStatus,
            ExportOptions = exportOptions,
        };

        NotifyStateChanged();
    }

    public void SetBoardReExportPending(BoardExportOptions reExportOptions)
    {
        if (Board is null)
            return;

        Board = Board with
        {
            ReExportStatus = BoardExportStatus.Requested,
            ReExportOptions = reExportOptions,
        };

        NotifyStateChanged();
    }

    public void ApplyExportStatusChanged(Guid boardId, BoardExportStatus status)
    {
        if (Board is null || Board.Id != boardId)
            return;

        Board = Board with { BoardExportStatus = status };
        NotifyStateChanged();
    }

    public void ApplyReExportStatusChanged(
        Guid boardId,
        BoardExportStatus status,
        BoardExportOptions? exportOptions = null)
    {
        if (Board is null || Board.Id != boardId)
            return;

        Board = exportOptions is null
            ? Board with { ReExportStatus = status }
            : Board with
            {
                ReExportStatus = status,
                ExportOptions = exportOptions,
                ReExportOptions = null,
            };

        NotifyStateChanged();
    }

    public void UpdateColumnName(Guid columnId, string name)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Select(column => column.Id == columnId ? column with { Name = name } : column)
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    public void UpdateTaskTitle(Guid taskId, string title)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Select(column => column with
            {
                BoardTasks = column.BoardTasks
                    .Select(task => task.Id == taskId ? task with { Title = title } : task)
                    .ToList()
            })
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    public void AdjustTaskCommentsCount(Guid taskId, int delta)
    {
        if (Board is null || delta == 0)
            return;

        UpdateTaskPreview(taskId, task => task with
        {
            CommentsCount = Math.Max(0, task.CommentsCount + delta),
        });
    }

    public void AdjustTaskAttachmentsCount(Guid taskId, int delta)
    {
        if (Board is null || delta == 0)
            return;

        UpdateTaskPreview(taskId, task => task with
        {
            AttachmentsCount = Math.Max(0, task.AttachmentsCount + delta),
        });
    }

    public void UpdateTaskAssigneeId(Guid taskId, Guid? assigneeId)
    {
        if (Board is null)
            return;

        UpdateTaskPreview(taskId, task => task with { AssigneeId = assigneeId });
    }

    public void SetShowOnlyMyTasks(bool showOnlyMyTasks)
    {
        if (ShowOnlyMyTasks == showOnlyMyTasks)
            return;

        ShowOnlyMyTasks = showOnlyMyTasks;
        NotifyStateChanged();
    }

    public async Task ReorderColumnAsync(Guid columnId, int position, CancellationToken ct = default)
    {
        if (BoardId is not { } boardId || Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        var orderedColumns = Board.Columns
            .OrderBy(column => column.Position)
            .ToList();

        var currentIndex = orderedColumns.FindIndex(column => column.Id == columnId);

        if (currentIndex < 0)
            throw new InvalidOperationException("Column was not found on the board.");

        if (currentIndex == position)
            return;

        var previousOrder = orderedColumns
            .Select(column => column.Id)
            .ToList();

        ApplyColumnMove(columnId, position);

        try
        {
            await boardApiService.ReorderColumnAsync(boardId, columnId, position, ct);
        }
        catch
        {
            ApplyColumnOrder(previousOrder);
            throw;
        }
    }

    public async Task DeleteTaskAsync(Guid boardId, Guid columnId, Guid taskId, CancellationToken ct = default)
    {
        await boardApiService.DeleteBoardTaskAsync(boardId, columnId, taskId, ct);

        if (BoardId == boardId && Board is not null)
            RemoveTaskLocally(columnId, taskId);
    }

    public async Task ReorderTaskAsync(Guid taskId, Guid targetColumnId, int position, CancellationToken ct = default)
    {
        if (BoardId is not { } boardId || Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        var sourceColumn = Board.Columns.FirstOrDefault(column => column.BoardTasks.Any(task => task.Id == taskId));

        if (sourceColumn is null)
            throw new InvalidOperationException("Task was not found on the loaded board.");

        var task = sourceColumn.BoardTasks.First(t => t.Id == taskId);

        if (sourceColumn.Id == targetColumnId && task.Position == position)
            return;

        IsReorderingTasks = true;

        var snapshot = CaptureTaskOrderSnapshot();

        ApplyTaskMove(taskId, targetColumnId, position);

        try
        {
            await boardApiService.ReorderTaskAsync(boardId, targetColumnId, taskId, position, ct);
        }
        catch
        {
            RestoreTaskOrderSnapshot(snapshot);
            throw;
        }
        finally
        {
            IsReorderingTasks = false;
            NotifyStateChanged();
        }
    }

    public void ApplyBoardActionNotification(BoardActionNotification notification, Guid currentUserId)
    {
        if (!syncGuard.TryAccept(notification, BoardId, currentUserId))
            return;

        var applied = notification.Type switch
        {
            BoardActionNotificationType.BoardRenamed => ApplyBoardRenamed((BoardRenamedPayload)notification.Payload),
            BoardActionNotificationType.ColumnCreated => ApplyColumnCreated((ColumnCreatedPayload)notification.Payload),
            BoardActionNotificationType.ColumnRenamed => ApplyColumnRenamed((ColumnRenamedPayload)notification.Payload),
            BoardActionNotificationType.ColumnDeleted => ApplyColumnDeleted((ColumnDeletedPayload)notification.Payload),
            BoardActionNotificationType.ColumnsReordered => ApplyColumnsReordered((ColumnsReorderedPayload)notification.Payload),
            BoardActionNotificationType.TaskCreated => ApplyTaskCreated((TaskCreatedPayload)notification.Payload),
            BoardActionNotificationType.TaskUpdated => ApplyTaskUpdated((TaskUpdatedPayload)notification.Payload),
            BoardActionNotificationType.TaskDeleted => ApplyTaskDeleted((TaskDeletedPayload)notification.Payload),
            BoardActionNotificationType.TasksReordered => ApplyTasksReordered((TasksReorderedPayload)notification.Payload),
            BoardActionNotificationType.TaskCommentsCountChanged =>
                ApplyTaskCommentsCountChanged((TaskCommentsCountChangedPayload)notification.Payload),
            BoardActionNotificationType.TaskAttachmentsCountChanged =>
                ApplyTaskAttachmentsCountChanged((TaskAttachmentsCountChangedPayload)notification.Payload),
            BoardActionNotificationType.TaskCompletionChanged =>
                ApplyTaskCompletionChanged((TaskCompletionChangedPayload)notification.Payload),
            BoardActionNotificationType.TaskStoryPointsChanged =>
                ApplyTaskStoryPointsChanged((TaskStoryPointsChangedPayload)notification.Payload),
            BoardActionNotificationType.TaskTimeboxChanged =>
                ApplyTaskTimeboxChanged((TaskTimeboxChangedPayload)notification.Payload),
            _ => LogUnhandledBoardAction(notification.Type),
        };

        if (!applied)
            return;

        syncGuard.MarkApplied(notification);
        NotifyStateChanged();
    }

    public void Reset()
    {
        BoardId = null;
        Board = null;
        IsLoading = false;
        ErrorMessage = null;
        ShowOnlyMyTasks = false;
        syncGuard.Reset();
        NotifyStateChanged();
    }

    private bool ApplyBoardRenamed(BoardRenamedPayload payload)
    {
        if (Board is null)
            return false;

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            logger.LogWarning("Ignored board rename notification with an empty name.");
            return false;
        }

        Board = Board with { Name = payload.Name };
        RemoteBoardNameApplied?.Invoke();
        return true;
    }

    private bool ApplyColumnCreated(ColumnCreatedPayload payload)
    {
        if (Board is null)
            return false;

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            logger.LogWarning("Ignored column created notification with an empty name.");
            return false;
        }

        if (Board.Columns.Any(column => column.Id == payload.ColumnId))
            return true;

        var column = new ColumnDto(payload.ColumnId, payload.Name, payload.Position, []);

        Board = Board with
        {
            Columns = Board.Columns
                .Append(column)
                .OrderBy(column => column.Position)
                .ToList(),
        };

        return true;
    }

    private bool ApplyColumnRenamed(ColumnRenamedPayload payload)
    {
        if (Board is null)
            return false;

        if (string.IsNullOrWhiteSpace(payload.Name))
        {
            logger.LogWarning("Ignored column renamed notification with an empty name.");
            return false;
        }

        if (Board.Columns.All(column => column.Id != payload.ColumnId))
            return false;

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column => column.Id == payload.ColumnId ? column with { Name = payload.Name } : column)
                .ToList(),
        };

        return true;
    }

    private bool ApplyColumnDeleted(ColumnDeletedPayload payload)
    {
        if (Board is null)
            return false;

        var deletedColumn = Board.Columns.FirstOrDefault(column => column.Id == payload.ColumnId);

        if (deletedColumn is not null)
        {
            if (payload.TasksDisposition == DeleteColumnTasksDisposition.MoveToColumn
                && payload.TargetColumnId is { } targetColumnId
                && payload.MovePlacement is { } movePlacement)
            {
                var columns = MoveTasksLocally(
                    Board.Columns.ToList(),
                    deletedColumn.BoardTasks,
                    targetColumnId,
                    movePlacement);

                columns = columns
                    .Where(column => column.Id != payload.ColumnId)
                    .ToList();

                Board = Board with { Columns = columns };
            }
            else
            {
                Board = Board with
                {
                    Columns = Board.Columns
                        .Where(column => column.Id != payload.ColumnId)
                        .ToList(),
                };
            }
        }

        ApplyColumnPositions(payload.RemainingColumns);

        if (payload.MovedTasks is { Count: > 0 } && payload.TargetColumnId is { } movedTargetColumnId)
            ApplyColumnTaskPositions(movedTargetColumnId, payload.MovedTasks);

        return true;
    }

    private bool ApplyColumnsReordered(ColumnsReorderedPayload payload)
    {
        if (Board is null)
            return false;

        ApplyColumnPositions(payload.Columns);
        return true;
    }

    private bool ApplyTaskCreated(TaskCreatedPayload payload)
    {
        if (Board is null)
            return false;

        if (string.IsNullOrWhiteSpace(payload.Title))
        {
            logger.LogWarning("Ignored task created notification with an empty title.");
            return false;
        }

        if (Board.Columns.All(column => column.Id != payload.ColumnId))
            return false;

        if (Board.Columns.Any(column => column.BoardTasks.Any(task => task.Id == payload.BoardTaskId)))
            return true;

        var task = new BoardTaskPreviewDto(
            payload.BoardTaskId,
            payload.Title,
            payload.Position,
            0,
            0,
            payload.AssigneeId,
            payload.Type,
            false,
            null,
            null);

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column =>
                {
                    if (column.Id != payload.ColumnId)
                        return column;

                    var tasks = column.BoardTasks
                        .OrderBy(boardTask => boardTask.Position)
                        .ToList();

                    var insertIndex = Math.Clamp(payload.Position, 0, tasks.Count);
                    tasks.Insert(insertIndex, task);

                    return column with
                    {
                        BoardTasks = tasks
                            .Select((boardTask, index) => boardTask with { Position = index })
                            .ToList(),
                    };
                })
                .ToList(),
        };

        return true;
    }

    private bool ApplyTaskUpdated(TaskUpdatedPayload payload)
    {
        if (Board is null)
            return false;

        if (string.IsNullOrWhiteSpace(payload.Title))
        {
            logger.LogWarning("Ignored task updated notification with an empty title.");
            return false;
        }

        if (!TryGetTask(payload.BoardTaskId, out _))
            return false;

        UpdateTaskPreviewSilent(
            payload.BoardTaskId,
            task => task with
            {
                Title = payload.Title,
                AssigneeId = payload.AssigneeId,
            });

        return true;
    }

    private bool ApplyTaskDeleted(TaskDeletedPayload payload)
    {
        if (Board is null)
            return false;

        if (Board.Columns.All(column => column.Id != payload.ColumnId))
            return false;

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column =>
                {
                    if (column.Id != payload.ColumnId)
                        return column;

                    return column with
                    {
                        BoardTasks = column.BoardTasks
                            .Where(task => task.Id != payload.BoardTaskId)
                            .ToList(),
                    };
                })
                .ToList(),
        };

        ApplyColumnTaskPositions(payload.ColumnId, payload.RemainingTasks);
        return true;
    }

    private bool ApplyTasksReordered(TasksReorderedPayload payload)
    {
        if (Board is null)
            return false;

        if (!TryGetTask(payload.BoardTaskId, out _))
            return false;

        var tasksById = Board.Columns
            .SelectMany(column => column.BoardTasks)
            .ToDictionary(task => task.Id);

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column =>
                {
                    if (column.Id == payload.SourceColumnId)
                    {
                        return column with
                        {
                            BoardTasks = BuildColumnTasksFromPositions(
                                payload.SourceColumnTasks,
                                tasksById),
                        };
                    }

                    if (column.Id == payload.TargetColumnId)
                    {
                        return column with
                        {
                            BoardTasks = BuildColumnTasksFromPositions(
                                payload.TargetColumnTasks,
                                tasksById),
                        };
                    }

                    return column;
                })
                .ToList(),
        };

        return true;
    }

    private bool ApplyTaskCommentsCountChanged(TaskCommentsCountChangedPayload payload)
    {
        if (Board is null)
            return false;

        SetTaskCommentsCount(payload.BoardTaskId, payload.CommentsCount);
        return true;
    }

    private bool ApplyTaskAttachmentsCountChanged(TaskAttachmentsCountChangedPayload payload)
    {
        if (Board is null)
            return false;

        SetTaskAttachmentsCount(payload.BoardTaskId, payload.AttachmentsCount);
        return true;
    }

    private bool ApplyTaskCompletionChanged(TaskCompletionChangedPayload payload)
    {
        if (Board is null)
            return false;

        if (!TryGetTask(payload.BoardTaskId, out _))
            return false;

        UpdateTaskPreviewSilent(payload.BoardTaskId, task => task with
        {
            IsDone = payload.IsDone,
        });

        return true;
    }

    private bool ApplyTaskStoryPointsChanged(TaskStoryPointsChangedPayload payload)
    {
        if (Board is null)
            return false;

        if (!TryGetTask(payload.BoardTaskId, out _))
            return false;

        UpdateTaskPreviewSilent(payload.BoardTaskId, task => task with
        {
            StoryPoints = payload.StoryPoints,
        });

        return true;
    }

    private bool ApplyTaskTimeboxChanged(TaskTimeboxChangedPayload payload)
    {
        if (Board is null)
            return false;

        if (!TryGetTask(payload.BoardTaskId, out _))
            return false;

        UpdateTaskPreviewSilent(payload.BoardTaskId, task => task with
        {
            TimeboxHours = payload.TimeboxHours,
        });

        return true;
    }

    private void SetTaskCommentsCount(Guid taskId, int commentsCount)
    {
        if (Board is null)
            return;

        UpdateTaskPreviewSilent(taskId, task => task with
        {
            CommentsCount = Math.Max(0, commentsCount),
        });
    }

    private void SetTaskAttachmentsCount(Guid taskId, int attachmentsCount)
    {
        if (Board is null)
            return;

        UpdateTaskPreviewSilent(taskId, task => task with
        {
            AttachmentsCount = Math.Max(0, attachmentsCount),
        });
    }

    private void ApplyColumnPositions(IReadOnlyList<BoardActionColumnPosition> columnPositions)
    {
        if (Board is null || columnPositions.Count == 0)
            return;

        var columnsById = Board.Columns.ToDictionary(column => column.Id);
        var orderedColumnIds = columnPositions
            .OrderBy(position => position.Position)
            .Select(position => position.ColumnId)
            .Where(columnsById.ContainsKey)
            .ToList();

        if (orderedColumnIds.Count == 0)
            return;

        var reorderedColumns = orderedColumnIds
            .Select((id, index) => columnsById[id] with { Position = index })
            .ToList();

        var missingColumns = Board.Columns
            .Where(column => !orderedColumnIds.Contains(column.Id))
            .OrderBy(column => column.Position);

        Board = Board with
        {
            Columns = reorderedColumns
                .Concat(missingColumns)
                .ToList(),
        };
    }

    private void ApplyColumnTaskPositions(Guid columnId, IReadOnlyList<BoardActionTaskPosition> taskPositions)
    {
        if (Board is null || taskPositions.Count == 0)
            return;

        var column = Board.Columns.FirstOrDefault(boardColumn => boardColumn.Id == columnId);

        if (column is null)
            return;

        var tasksById = Board.Columns
            .SelectMany(boardColumn => boardColumn.BoardTasks)
            .ToDictionary(task => task.Id);

        var orderedTasks = taskPositions
            .OrderBy(position => position.Position)
            .Where(position => tasksById.ContainsKey(position.BoardTaskId))
            .Select(position => tasksById[position.BoardTaskId] with { Position = position.Position })
            .ToList();

        if (orderedTasks.Count == 0)
            return;

        var orderedTaskIds = orderedTasks
            .Select(task => task.Id)
            .ToHashSet();

        var remainingTasks = column.BoardTasks
            .Where(task => !orderedTaskIds.Contains(task.Id))
            .OrderBy(task => task.Position)
            .ToList();

        var mergedTasks = orderedTasks
            .Concat(remainingTasks)
            .Select((task, index) => task with { Position = index })
            .ToList();

        Board = Board with
        {
            Columns = Board.Columns
                .Select(boardColumn => boardColumn.Id == columnId
                    ? boardColumn with { BoardTasks = mergedTasks }
                    : boardColumn)
                .ToList(),
        };
    }

    private static List<BoardTaskPreviewDto> BuildColumnTasksFromPositions(
        IReadOnlyList<BoardActionTaskPosition> taskPositions,
        IReadOnlyDictionary<Guid, BoardTaskPreviewDto> tasksById)
    {
        return taskPositions
            .OrderBy(position => position.Position)
            .Where(position => tasksById.ContainsKey(position.BoardTaskId))
            .Select(position => tasksById[position.BoardTaskId] with { Position = position.Position })
            .ToList();
    }

    private bool TryGetTask(Guid taskId, out BoardTaskPreviewDto task)
    {
        task = default!;

        if (Board is null)
            return false;

        foreach (var column in Board.Columns)
        {
            var foundTask = column.BoardTasks.FirstOrDefault(boardTask => boardTask.Id == taskId);

            if (foundTask is not null)
            {
                task = foundTask;
                return true;
            }
        }

        return false;
    }

    private void UpdateTaskPreviewSilent(Guid taskId, Func<BoardTaskPreviewDto, BoardTaskPreviewDto> update)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Select(column => column with
            {
                BoardTasks = column.BoardTasks
                    .Select(task => task.Id == taskId ? update(task) : task)
                    .ToList(),
            })
            .ToList();

        Board = Board with { Columns = columns };
    }

    private bool LogUnhandledBoardAction(BoardActionNotificationType type)
    {
        logger.LogDebug("Unhandled board action notification type {Type}.", type);
        return false;
    }

    private void ApplyColumnDeletion(ColumnDto deletedColumn, DeleteColumnRequest request)
    {
        if (Board is null)
            return;

        var columns = Board.Columns.ToList();
        var deletedPosition = deletedColumn.Position;

        if (request.TasksDisposition == DeleteColumnTasksDisposition.MoveToColumn
            && deletedColumn.BoardTasks.Count > 0
            && request.TargetColumnId is { } targetColumnId)
        {
            columns = MoveTasksLocally(
                columns,
                deletedColumn.BoardTasks,
                targetColumnId,
                request.MovePlacement!.Value);
        }

        columns = columns
            .Where(column => column.Id != deletedColumn.Id)
            .Select(column => column.Position > deletedPosition
                ? column with { Position = column.Position - 1 }
                : column)
            .OrderBy(column => column.Position)
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    private static List<ColumnDto> MoveTasksLocally(
        List<ColumnDto> columns,
        IReadOnlyList<BoardTaskPreviewDto> tasksToMove,
        Guid targetColumnId,
        ColumnTaskMovePlacement placement)
    {
        var targetColumn = columns.First(column => column.Id == targetColumnId);
        var targetTasks = targetColumn.BoardTasks.ToList();
        IReadOnlyList<BoardTaskPreviewDto> updatedTargetTasks;

        if (placement == ColumnTaskMovePlacement.Start)
        {
            var offset = tasksToMove.Count;
            var shiftedTargetTasks = targetTasks
                .Select(task => task with { Position = task.Position + offset })
                .ToList();
            var movedTasks = tasksToMove
                .Select((task, index) => task with { Position = index })
                .ToList();

            updatedTargetTasks = movedTasks
                .Concat(shiftedTargetTasks)
                .OrderBy(task => task.Position)
                .ToList();
        }
        else
        {
            var startPosition = targetTasks.Count;
            var movedTasks = tasksToMove
                .Select((task, index) => task with { Position = startPosition + index })
                .ToList();

            updatedTargetTasks = targetTasks
                .Concat(movedTasks)
                .ToList();
        }

        return columns
            .Select(column => column.Id == targetColumnId
                ? column with { BoardTasks = updatedTargetTasks }
                : column)
            .ToList();
    }

    private void AddColumn(ColumnDto column)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Append(column)
            .OrderBy(c => c.Position)
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    private void RemoveTaskLocally(Guid columnId, Guid taskId)
    {
        if (Board is null)
            return;

        var column = Board.Columns.FirstOrDefault(c => c.Id == columnId);

        if (column is null || column.BoardTasks.All(task => task.Id != taskId))
            return;

        var columns = Board.Columns
            .Select(boardColumn =>
            {
                if (boardColumn.Id != columnId)
                    return boardColumn;

                var tasks = boardColumn.BoardTasks
                    .Where(task => task.Id != taskId)
                    .OrderBy(task => task.Position)
                    .Select((task, index) => task with { Position = index })
                    .ToList();

                return boardColumn with { BoardTasks = tasks };
            })
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    private void AddTask(Guid columnId, BoardTaskPreviewDto task)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Select(column =>
            {
                if (column.Id != columnId)
                    return column;

                var tasks = column.BoardTasks
                    .OrderBy(t => t.Position)
                    .Append(task)
                    .Select((t, index) => t with { Position = index })
                    .ToList();

                return column with { BoardTasks = tasks };
            })
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    private void ApplyColumnMove(Guid columnId, int newIndex)
    {
        if (Board is null)
            return;

        var orderedColumns = Board.Columns
            .OrderBy(column => column.Position)
            .ToList();

        var currentIndex = orderedColumns.FindIndex(boardColumn => boardColumn.Id == columnId);
        var column = orderedColumns[currentIndex];
        orderedColumns.RemoveAt(currentIndex);
        orderedColumns.Insert(newIndex, column);

        ApplyColumnOrder(orderedColumns.Select(boardColumn => boardColumn.Id).ToList());
    }

    private void ApplyColumnOrder(IReadOnlyList<Guid> columnIds)
    {
        if (Board is null)
            return;

        var columnsById = Board.Columns.ToDictionary(column => column.Id);

        var reorderedColumns = columnIds
            .Select((id, index) => columnsById[id] with { Position = index })
            .ToList();

        Board = Board with { Columns = reorderedColumns };
        NotifyStateChanged();
    }

    private void ApplyTaskMove(Guid taskId, Guid targetColumnId, int newIndex)
    {
        if (Board is null)
            return;

        var columns = Board.Columns.ToList();
        var sourceColumn = columns.First(column => column.BoardTasks.Any(task => task.Id == taskId));
        var movedTask = sourceColumn.BoardTasks.First(task => task.Id == taskId);

        if (sourceColumn.Id == targetColumnId)
        {
            var orderedTasks = sourceColumn.BoardTasks
                .OrderBy(task => task.Position)
                .ToList();

            orderedTasks.RemoveAll(task => task.Id == taskId);
            orderedTasks.Insert(newIndex, movedTask);

            var reorderedTasks = orderedTasks
                .Select((task, index) => task with { Position = index })
                .ToList();

            columns = columns
                .Select(column => column.Id == targetColumnId
                    ? column with { BoardTasks = reorderedTasks }
                    : column)
                .ToList();
        }
        else
        {
            var sourceTasks = sourceColumn.BoardTasks
                .Where(task => task.Id != taskId)
                .OrderBy(task => task.Position)
                .Select((task, index) => task with { Position = index })
                .ToList();

            var targetColumn = columns.First(column => column.Id == targetColumnId);
            var targetTasks = targetColumn.BoardTasks
                .OrderBy(task => task.Position)
                .ToList();

            targetTasks.Insert(newIndex, movedTask);

            var reorderedTargetTasks = targetTasks
                .Select((task, index) => task with { Position = index })
                .ToList();

            columns = columns
                .Select(column =>
                {
                    if (column.Id == sourceColumn.Id)
                        return column with { BoardTasks = sourceTasks };

                    if (column.Id == targetColumnId)
                        return column with { BoardTasks = reorderedTargetTasks };

                    return column;
                })
                .ToList();
        }

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }

    private TaskOrderSnapshot CaptureTaskOrderSnapshot()
    {
        if (Board is null)
            throw new InvalidOperationException("Board details are not loaded.");

        return new TaskOrderSnapshot(
            Board.Columns
                .Select(column => (column.Id, (IReadOnlyList<BoardTaskPreviewDto>)column.BoardTasks.ToList()))
                .ToList());
    }

    private void RestoreTaskOrderSnapshot(TaskOrderSnapshot snapshot)
    {
        if (Board is null)
            return;

        var tasksByColumnId = snapshot.Columns.ToDictionary(entry => entry.ColumnId, entry => entry.Tasks);

        Board = Board with
        {
            Columns = Board.Columns
                .Select(column => column with { BoardTasks = tasksByColumnId[column.Id] })
                .ToList()
        };

        NotifyStateChanged();
    }

    private sealed record TaskOrderSnapshot(
        IReadOnlyList<(Guid ColumnId, IReadOnlyList<BoardTaskPreviewDto> Tasks)> Columns);

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private void UpdateTaskPreview(Guid taskId, Func<BoardTaskPreviewDto, BoardTaskPreviewDto> update)
    {
        if (Board is null)
            return;

        var columns = Board.Columns
            .Select(column => column with
            {
                BoardTasks = column.BoardTasks
                    .Select(task => task.Id == taskId ? update(task) : task)
                    .ToList()
            })
            .ToList();

        Board = Board with { Columns = columns };
        NotifyStateChanged();
    }
}
