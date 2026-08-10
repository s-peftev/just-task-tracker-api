using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Domain.Boards.DTOs.BoardTasks;

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
