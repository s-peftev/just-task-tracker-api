namespace JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions;

public enum BoardActionNotificationType : byte
{
    BoardRenamed = 1,
    ColumnCreated = 2,
    ColumnRenamed = 3,
    ColumnDeleted = 4,
    ColumnsReordered = 5,
    TaskCreated = 6,
    TaskUpdated = 7,
    TaskDeleted = 8,
    TasksReordered = 9,
    TaskCommentsCountChanged = 10,
    TaskAttachmentsCountChanged = 11,
    TaskDescriptionChanged = 12,
    TaskAssigneeChanged = 13,
    TaskAttachmentAdded = 14,
    TaskAttachmentRemoved = 15,
    TaskCompletionChanged = 16,
    TaskStoryPointsChanged = 17,
    TaskTimeboxChanged = 18,
}
