namespace JustTaskTracker.Application.Boards.ReadModels;

public record ColumnReadModel(
    Guid Id,
    string Name,
    int Position,
    IReadOnlyList<BoardTaskPreviewReadModel> BoardTasks);
