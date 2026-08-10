using JustTaskTracker.Domain.Boards.Constants;
using JustTaskTracker.Domain.Boards.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace JustTaskTracker.Persistence.Boards.Configurations;

public class BoardTaskConfiguration : IEntityTypeConfiguration<BoardTask>
{
    public void Configure(EntityTypeBuilder<BoardTask> builder)
    {
        builder.Property(t => t.Title).HasMaxLength(BoardTaskFieldLengths.MaxTitleLength);
        builder.Property(t => t.Description).HasMaxLength(BoardTaskFieldLengths.MaxDescriptionLength);

        builder.Property(t => t.Type)
            .HasConversion<byte>();

        builder.HasOne(t => t.Column)
            .WithMany(c => c.Tasks)
            .HasForeignKey(t => t.ColumnId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Assignee)
            .WithMany()
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(t => t.Reporter)
            .WithMany()
            .HasForeignKey(t => t.ReporterId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(t => new { t.ColumnId, t.Position })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");
    }
}
