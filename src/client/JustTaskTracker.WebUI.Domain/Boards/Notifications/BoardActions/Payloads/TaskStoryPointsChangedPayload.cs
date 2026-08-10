namespace JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskStoryPointsChangedPayload(
    Guid BoardTaskId,
    byte? StoryPoints) : BoardActionPayload;
