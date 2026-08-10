using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Application.Boards.ReadModels;

public record BoardTaskPreviewReadModel(
    Guid Id,
    string Title,
    int Position,
    int CommentsCount,
    int AttachmentsCount,
    UserReadModel? Assignee,
    BoardTaskType Type,
    bool IsDone,
    byte? StoryPoints,
    short? TimeboxHours);
