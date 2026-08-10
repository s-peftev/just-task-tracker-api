using JustTaskTracker.Application.Boards.ReadModels;
using JustTaskTracker.Application.Users.Mappings;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;

namespace JustTaskTracker.Application.Boards.Mappings;

public static class BoardTaskPreviewReadModelMappings
{
    public static BoardTaskPreviewDto ToDto(
        this BoardTaskPreviewReadModel task,
        Func<UserReadModel, string?> profilePhotoUrlResolver) =>
        new(
            task.Id,
            task.Title,
            task.Position,
            task.CommentsCount,
            task.AttachmentsCount,
            task.Assignee.ToNullableDto(profilePhotoUrlResolver),
            task.Type,
            task.IsDone,
            task.StoryPoints,
            task.TimeboxHours);
}
