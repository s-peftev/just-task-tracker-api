namespace JustTaskTracker.WebUI.Domain.Boards;

public static class BoardTaskPreviewDtoExtensions
{
    public static bool IsAssignedToCurrentUser(this BoardTaskPreviewDto task, Guid userId) =>
        task.Assignee?.Id == userId;
}
