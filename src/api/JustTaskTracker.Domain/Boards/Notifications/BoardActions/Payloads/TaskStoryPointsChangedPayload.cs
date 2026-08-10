namespace JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;

public record TaskStoryPointsChangedPayload(
    Guid BoardTaskId,
    byte? StoryPoints) : BoardActionPayload;
