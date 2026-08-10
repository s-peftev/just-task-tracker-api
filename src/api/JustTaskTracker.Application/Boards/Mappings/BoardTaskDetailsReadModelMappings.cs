using JustTaskTracker.Application.Boards.ReadModels;
using JustTaskTracker.Application.Users.Mappings;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;
using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Application.Boards.Mappings;

public static class BoardTaskDetailsReadModelMappings
{
    public static BoardTaskDetailsDto ToDto(
        this BoardTaskDetailsReadModel task,
        Func<UserReadModel, string?> profilePhotoUrlResolver,
        BoardMemberRole userRole) =>
        new(
            task.Id,
            task.ColumnId,
            task.ColumnName,
            task.Title,
            task.Position,
            task.CreatedAtUtc,
            task.Reporter.ToDto(profilePhotoUrlResolver),
            userRole,
            task.Attachments.Select(attachment => attachment.ToDto(profilePhotoUrlResolver)).ToList(),
            task.Type,
            task.IsDone,
            task.CompletedAtUtc,
            task.StoryPoints,
            task.TimeboxHours,
            task.Description,
            task.Assignee.ToNullableDto(profilePhotoUrlResolver));
}
