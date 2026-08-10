using JustTaskTracker.Domain.Auth.Entities;
using JustTaskTracker.Domain.Boards.Enums;
using JustTaskTracker.Domain.Common.Entities;
using JustTaskTracker.Domain.Common.Interfaces;

namespace JustTaskTracker.Domain.Boards.Entities;

public class BoardTask : BaseEntity<Guid>, IPositionedEntity
{
    public required Guid ColumnId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int Position { get; set; }
    public Guid? AssigneeId { get; set; }
    public required Guid ReporterId { get; init; }
    public BoardTaskType Type { get; init; } = BoardTaskType.Story;
    public bool IsDone { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public byte? StoryPoints { get; set; }
    public short? TimeboxHours { get; set; }

    public Column? Column { get; set; }
    public User? Assignee { get; set; }
    public User? Reporter { get; set; }
    public ICollection<BoardTaskComment> Comments { get; set; } = [];
    public ICollection<BoardTaskAttachment> Attachments { get; set; } = [];
}
