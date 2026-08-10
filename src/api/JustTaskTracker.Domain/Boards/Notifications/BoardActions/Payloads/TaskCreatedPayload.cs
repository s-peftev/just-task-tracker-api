using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskCreatedPayload(
    Guid ColumnId,
    Guid BoardTaskId,
    string Title,
    int Position,
    Guid? AssigneeId,
    BoardTaskType Type) : BoardActionPayload;
