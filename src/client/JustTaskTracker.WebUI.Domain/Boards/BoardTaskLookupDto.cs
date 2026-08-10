using JustTaskTracker.WebUI.Domain.Boards.Enums;

namespace JustTaskTracker.WebUI.Domain.Boards;

public record BoardTaskLookupDto(
    Guid Id,
    Guid ColumnId,
    string Title,
    string? Description,
    BoardTaskType Type);
