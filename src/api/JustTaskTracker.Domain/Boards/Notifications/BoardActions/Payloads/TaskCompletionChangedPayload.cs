namespace JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskCompletionChangedPayload(
    Guid BoardTaskId,
    bool IsDone,
    DateTime? CompletedAtUtc) : BoardActionPayload;
