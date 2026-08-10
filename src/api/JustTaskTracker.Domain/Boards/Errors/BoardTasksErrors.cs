using JustTaskTracker.Domain.Common.Enums;
using JustTaskTracker.Domain.Common.Results;

namespace JustTaskTracker.Domain.Boards.Errors;

public static class BoardTasksErrors
{
    public static readonly Error AssigneeNotBoardMember = new(
        nameof(AssigneeNotBoardMember),
        ErrorType.Business,
        ["The assignee must be a member of the board."]);

    public static readonly Error InvalidPosition = new(
        nameof(InvalidPosition),
        ErrorType.Validation,
        ["Task position is out of range."]);

    public static readonly Error TooManyAttachments = new(
        nameof(TooManyAttachments),
        ErrorType.Business,
        ["The maximum number of attachments for this task has been reached."]);

    public static readonly Error EstimationNotAllowedForTaskType = new(
        nameof(EstimationNotAllowedForTaskType),
        ErrorType.Business,
        ["Estimation is not allowed for this task type."]);
}
