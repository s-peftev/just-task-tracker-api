using JustTaskTracker.Application.Boards.ReadModels;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.DTOs.Columns;

namespace JustTaskTracker.Application.Boards.Mappings;

public static class ColumnReadModelMappings
{
    public static ColumnDto ToDto(
        this ColumnReadModel column,
        Func<UserReadModel, string?> profilePhotoUrlResolver) =>
        new(
            column.Id,
            column.Name,
            column.Position,
            column.BoardTasks.Select(task => task.ToDto(profilePhotoUrlResolver)).ToList());
}
