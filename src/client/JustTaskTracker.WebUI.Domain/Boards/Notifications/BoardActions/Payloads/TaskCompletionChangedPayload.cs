namespace JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskCompletionChangedPayload(
    Guid BoardTaskId,
    bool IsDone,
    DateTime? CompletedAtUtc) : BoardActionPayload;
