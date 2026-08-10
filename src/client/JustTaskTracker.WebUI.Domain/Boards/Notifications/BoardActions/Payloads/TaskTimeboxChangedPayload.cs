namespace JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskTimeboxChangedPayload(
    Guid BoardTaskId,
    short? TimeboxHours) : BoardActionPayload;
