using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Application.Boards.ReadModels;

public record BoardDetailsReadModel(
    Guid Id,
    string Name,
    DateTime CreatedAtUtc,
    bool IsArchived,
    BoardMemberRole UserRole,
    IReadOnlyList<ColumnReadModel> Columns,
    Guid? OwnerUserId,
    DateTime? ArchivedAtUtc);
