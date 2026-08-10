using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Domain.Boards.DTOs.BoardTasks;

public record BoardTaskLookupDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    string? Description,
    BoardTaskType Type,
    bool IsDone,
    byte? StoryPoints,
    short? TimeboxHours);
