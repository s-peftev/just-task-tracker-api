using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;

namespace JustTaskTracker.WebUI.Services.Boards.Actions;

internal static class BoardActionSyncKey
{
    public static string Resolve(BoardActionNotification notification) =>
        notification.Type switch
        {
            BoardActionNotificationType.BoardRenamed => "board",

            BoardActionNotificationType.ColumnCreated =>
                $"column:{((ColumnCreatedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnRenamed =>
                $"column:{((ColumnRenamedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnDeleted =>
                $"column:{((ColumnDeletedPayload)notification.Payload).ColumnId}",

            BoardActionNotificationType.ColumnsReordered => "board:columns-order",

            BoardActionNotificationType.TaskCreated =>
                $"task:{((TaskCreatedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TaskUpdated =>
                $"task:{((TaskUpdatedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TaskAssigneeChanged =>
                $"task:{((TaskAssigneeChangedPayload)notification.Payload).BoardTaskId}:assignee",

            BoardActionNotificationType.TaskDeleted =>
                $"task:{((TaskDeletedPayload)notification.Payload).BoardTaskId}",

            BoardActionNotificationType.TasksReordered =>
                $"task:{((TasksReorderedPayload)notification.Payload).BoardTaskId}:position",

            BoardActionNotificationType.TaskCommentsCountChanged =>
                $"task:{((TaskCommentsCountChangedPayload)notification.Payload).BoardTaskId}:comments-count",

            BoardActionNotificationType.TaskAttachmentsCountChanged =>
                $"task:{((TaskAttachmentsCountChangedPayload)notification.Payload).BoardTaskId}:attachments-count",

            BoardActionNotificationType.TaskCompletionChanged =>
                $"task:{((TaskCompletionChangedPayload)notification.Payload).BoardTaskId}:completion",

            BoardActionNotificationType.TaskStoryPointsChanged =>
                $"task:{((TaskStoryPointsChangedPayload)notification.Payload).BoardTaskId}:story-points",

            BoardActionNotificationType.TaskTimeboxChanged =>
                $"task:{((TaskTimeboxChangedPayload)notification.Payload).BoardTaskId}:timebox",

            _ => $"type:{(byte)notification.Type}",
        };
}
