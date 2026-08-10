namespace JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskTimeboxChangedPayload(
    Guid BoardTaskId,
    short? TimeboxHours) : BoardActionPayload;
