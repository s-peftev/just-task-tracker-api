using JustTaskTracker.Domain.Auth.DTOs;
using JustTaskTracker.Domain.Boards.DTOs.Attachments;
using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Domain.Boards.DTOs.BoardTasks;

public record BoardTaskDetailsDto(
    Guid Id,
    Guid ColumnId,
    string ColumnName,
    string Title,
    int Position,
    DateTime CreatedAtUtc,
    UserDto Reporter,
    BoardMemberRole UserRole,
    IReadOnlyList<BoardTaskAttachmentDto> Attachments,
    BoardTaskType Type,
    bool IsDone,
    DateTime? CompletedAtUtc,
    byte? StoryPoints,
    short? TimeboxHours,
    string? Description = null,
    UserDto? Assignee = null);
