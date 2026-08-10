using FluentValidation;
using JustTaskTracker.Application.Auth;
using JustTaskTracker.Application.Auth.Repositories;
using JustTaskTracker.Application.Boards.Notifiers;
using JustTaskTracker.Application.Boards.Repositories;
using JustTaskTracker.Application.Common.Behaviors;
using JustTaskTracker.Application.Common.Persistence;
using JustTaskTracker.Application.Common.Utils;
using JustTaskTracker.Domain.Billing.Enums;
using JustTaskTracker.Domain.Boards.Authorization;
using JustTaskTracker.Domain.Boards.Constants;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;
using JustTaskTracker.Domain.Boards.Entities;
using JustTaskTracker.Domain.Boards.Enums;
using JustTaskTracker.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;
using JustTaskTracker.Domain.Common.Results;
using JustTaskTracker.Domain.Common.Results.Errors;
using MediatR;

namespace JustTaskTracker.Application.Boards.Commands.BoardTasks;

public record CreateBoardTaskCommand(Guid BoardId, Guid ColumnId, string Title, BoardTaskType Type)
    : IRequest<Result<BoardTaskPreviewDto>>, IRequireActiveBoard, IRequirePlanLimit
{
    public PlanLimitKind Limit => PlanLimitKind.TasksPerBoard;

    Guid? IRequirePlanLimit.BoardId => BoardId;
}

public class CreateBoardTaskCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IColumnRepository columnRepository,
    IBoardTaskRepository boardTaskRepository,
    IUnitOfWork unitOfWork,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CreateBoardTaskCommand, Result<BoardTaskPreviewDto>>
{
    public async Task<Result<BoardTaskPreviewDto>> Handle(CreateBoardTaskCommand request, CancellationToken ct)
    {
        var currentUserInfo = await userRepository.GetUserInfoByAzureAOIAsync(currentUserAccessor.AzureAdObjectId, ct);

        if (currentUserInfo is null)
            return Result<BoardTaskPreviewDto>.Failure(GeneralErrors.Unauthorized);

        var userRole = await columnRepository.GetUserRoleAsync(request.ColumnId, currentUserAccessor.AzureAdObjectId, ct);

        if (userRole is not { } authorizedRole || !BoardRolePermissions.CanManageTasks(authorizedRole))
            return Result<BoardTaskPreviewDto>.Failure(GeneralErrors.Forbidden);

        var title = request.Title.Trim();

        var position = await boardTaskRepository.GetCountByColumnIdAsync(request.ColumnId, ct);

        var task = new BoardTask
        {
            ColumnId = request.ColumnId,
            Title = title,
            Position = position,
            ReporterId = currentUserInfo.Id,
            Type = request.Type
        };

        boardTaskRepository.Add(task);

        await unitOfWork.SaveChangesAsync(ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskCreated,
            currentUserInfo.Id,
            dateTimeProvider.UtcNow,
            new TaskCreatedPayload(
                request.ColumnId,
                task.Id,
                task.Title,
                task.Position,
                task.AssigneeId,
                task.Type)), ct);

        return Result<BoardTaskPreviewDto>.Success(new BoardTaskPreviewDto(
            task.Id,
            task.Title,
            task.Position,
            0,
            0,
            null,
            task.Type,
            task.IsDone,
            task.StoryPoints,
            task.TimeboxHours));
    }
}

public class CreateBoardTaskCommandValidator : AbstractValidator<CreateBoardTaskCommand>
{
    public CreateBoardTaskCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.ColumnId)
            .NotEmpty();

        RuleFor(x => x.Title)
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("'Title' must not be empty.")
            .MaximumLength(BoardTaskFieldLengths.MaxTitleLength);

        RuleFor(x => x.Type)
            .IsInEnum();
    }
}
