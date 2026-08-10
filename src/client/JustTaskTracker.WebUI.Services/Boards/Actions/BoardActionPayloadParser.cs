using System.Text.Json;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.WebUI.Domain.Boards.Notifications.BoardActions.Payloads;

namespace JustTaskTracker.WebUI.Services.Boards.Actions;

internal static class BoardActionPayloadParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static BoardActionPayload Parse(BoardActionNotificationType type, JsonElement payload) =>
        type switch
        {
            BoardActionNotificationType.BoardRenamed =>
                payload.Deserialize<BoardRenamedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.ColumnCreated =>
                payload.Deserialize<ColumnCreatedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.ColumnRenamed =>
                payload.Deserialize<ColumnRenamedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.ColumnDeleted =>
                payload.Deserialize<ColumnDeletedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.ColumnsReordered =>
                payload.Deserialize<ColumnsReorderedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskCreated =>
                payload.Deserialize<TaskCreatedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskUpdated =>
                payload.Deserialize<TaskUpdatedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskDeleted =>
                payload.Deserialize<TaskDeletedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TasksReordered =>
                payload.Deserialize<TasksReorderedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskCommentsCountChanged =>
                payload.Deserialize<TaskCommentsCountChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskAttachmentsCountChanged =>
                payload.Deserialize<TaskAttachmentsCountChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskDescriptionChanged =>
                payload.Deserialize<TaskDescriptionChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskAssigneeChanged =>
                payload.Deserialize<TaskAssigneeChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskAttachmentAdded =>
                payload.Deserialize<TaskAttachmentAddedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskAttachmentRemoved =>
                payload.Deserialize<TaskAttachmentRemovedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskCompletionChanged =>
                payload.Deserialize<TaskCompletionChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskStoryPointsChanged =>
                payload.Deserialize<TaskStoryPointsChangedPayload>(Options)
                ?? throw CreateParseException(type),

            BoardActionNotificationType.TaskTimeboxChanged =>
                payload.Deserialize<TaskTimeboxChangedPayload>(Options)
                ?? throw CreateParseException(type),

            _ => throw new NotSupportedException($"Board action notification type '{type}' is not supported."),
        };

    private static InvalidOperationException CreateParseException(BoardActionNotificationType type) =>
        new($"Failed to deserialize board action payload for type '{type}'.");
}
