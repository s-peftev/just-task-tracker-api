using JustTaskTracker.WebUI.Domain.Boards.Enums;

namespace JustTaskTracker.WebUI.Domain.Boards;

public record BoardTaskPreviewDto(
    Guid Id,
    string Title,
    int Position,
    int CommentsCount,
    int AttachmentsCount,
    Guid? AssigneeId,
    BoardTaskType Type,
    bool IsDone,
    byte? StoryPoints,
    short? TimeboxHours);
