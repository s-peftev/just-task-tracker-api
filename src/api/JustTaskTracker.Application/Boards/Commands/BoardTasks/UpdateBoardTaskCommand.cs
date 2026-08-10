using FluentValidation;
using JustTaskTracker.Application.Auth;
using JustTaskTracker.Application.Auth.Repositories;
using JustTaskTracker.Application.Boards.Notifiers;
using JustTaskTracker.Application.Boards.Repositories;
using JustTaskTracker.Application.Common.Behaviors;
using JustTaskTracker.Application.Common.Models;
using JustTaskTracker.Application.Common.Persistence;
using JustTaskTracker.Application.Common.Utils;
using JustTaskTracker.Application.Users.Mappings;
using JustTaskTracker.Application.Users.ProfilePhotos;
using JustTaskTracker.Domain.Auth.DTOs;
using JustTaskTracker.Domain.Boards.Authorization;
using JustTaskTracker.Domain.Boards.Constants;
using JustTaskTracker.Domain.Boards.Errors;
using JustTaskTracker.Domain.Boards.Notifications.BoardActions;
using JustTaskTracker.Domain.Boards.Notifications.BoardActions.Payloads;
using JustTaskTracker.Domain.Boards.Rules;
using JustTaskTracker.Domain.Common.Results;
using JustTaskTracker.Domain.Common.Results.Errors;
using MediatR;

namespace JustTaskTracker.Application.Boards.Commands.BoardTasks;

public record UpdateBoardTaskCommand(
    Guid BoardId,
    Guid BoardTaskId,
    PatchField<string> Title = default,
    PatchField<string?> Description = default,
    PatchField<Guid?> AssigneeId = default,
    PatchField<bool> IsDone = default,
    PatchField<byte?> StoryPoints = default,
    PatchField<short?> TimeboxHours = default)
    : IRequest<Result>, IRequireActiveBoard;

public class UpdateBoardTaskCommandHandler(
    ICurrentUserAccessor currentUserAccessor,
    IUserRepository userRepository,
    IBoardRepository boardRepository,
    IBoardTaskRepository boardTaskRepository,
    IUnitOfWork unitOfWork,
    IBoardActionNotifier boardActionNotifier,
    IDateTimeProvider dateTimeProvider,
    IProfilePhotoService profilePhotoService)
    : IRequestHandler<UpdateBoardTaskCommand, Result>
{
    public async Task<Result> Handle(UpdateBoardTaskCommand request, CancellationToken ct)
    {
        var currentUserInfo = await userRepository.GetUserInfoByAzureAOIAsync(currentUserAccessor.AzureAdObjectId, ct);

        if (currentUserInfo is null)
            return Result.Failure(GeneralErrors.Unauthorized);

        var (boardTask, userRole) = await boardTaskRepository.GetBoardTaskWithUserRoleAsync(request.BoardTaskId, currentUserAccessor.AzureAdObjectId, ct);

        if (userRole is not { } authorizedRole)
            return Result.Failure(GeneralErrors.Forbidden);

        if (boardTask is null)
            return Result.Failure(GeneralErrors.NotFound);

        var canManageTasks = BoardRolePermissions.CanManageTasks(authorizedRole);
        var isAssignee = boardTask.AssigneeId == currentUserInfo.Id;
        var hasManageFields = request.Title.IsSpecified
            || request.Description.IsSpecified
            || request.AssigneeId.IsSpecified
            || request.StoryPoints.IsSpecified
            || request.TimeboxHours.IsSpecified;

        if (hasManageFields && !canManageTasks)
            return Result.Failure(GeneralErrors.Forbidden);

        if (request.IsDone.IsSpecified && !canManageTasks && !isAssignee)
            return Result.Failure(GeneralErrors.Forbidden);

        var hasChanges = false;
        var descriptionChanged = false;
        var assigneeChanged = false;
        var completionChanged = false;
        var storyPointsChanged = false;
        var timeboxChanged = false;

        if (request.Title.IsSpecified)
        {
            var title = request.Title.Value!.Trim();

            if (!string.Equals(boardTask.Title, title, StringComparison.Ordinal))
            {
                boardTask.Title = title;
                hasChanges = true;
            }
        }

        if (request.Description.IsSpecified)
        {
            var description = string.IsNullOrWhiteSpace(request.Description.Value)
                ? null
                : request.Description.Value!.Trim();

            if (!string.Equals(boardTask.Description, description, StringComparison.Ordinal))
            {
                boardTask.Description = description;
                hasChanges = true;
                descriptionChanged = true;
            }
        }

        if (request.AssigneeId.IsSpecified)
        {
            var assigneeId = request.AssigneeId.Value;

            if (boardTask.AssigneeId != assigneeId)
            {
                if (assigneeId is { } assignedUserId
                    && !await boardRepository.IsBoardMemberAsync(request.BoardId, assignedUserId, ct))
                {
                    return Result.Failure(BoardTasksErrors.AssigneeNotBoardMember);
                }

                boardTask.AssigneeId = assigneeId;
                hasChanges = true;
                assigneeChanged = true;
            }
        }

        if (request.IsDone.IsSpecified)
        {
            var isDone = request.IsDone.Value;

            if (boardTask.IsDone != isDone)
            {
                boardTask.IsDone = isDone;
                boardTask.CompletedAtUtc = isDone ? dateTimeProvider.UtcNow : null;
                hasChanges = true;
                completionChanged = true;
            }
        }

        if (request.StoryPoints.IsSpecified && boardTask.StoryPoints != request.StoryPoints.Value)
        {
            boardTask.StoryPoints = request.StoryPoints.Value;
            hasChanges = true;
            storyPointsChanged = true;
        }

        if (request.TimeboxHours.IsSpecified && boardTask.TimeboxHours != request.TimeboxHours.Value)
        {
            boardTask.TimeboxHours = request.TimeboxHours.Value;
            hasChanges = true;
            timeboxChanged = true;
        }

        if (request.StoryPoints.IsSpecified || request.TimeboxHours.IsSpecified)
        {
            if (!BoardTaskEstimationRules.Validate(boardTask.Type, boardTask.StoryPoints, boardTask.TimeboxHours))
                return Result.Failure(BoardTasksErrors.EstimationNotAllowedForTaskType);
        }

        if (!hasChanges)
            return Result.Success();

        await unitOfWork.SaveChangesAsync(ct);

        await boardActionNotifier.NotifyAsync(new BoardActionNotification(
            request.BoardId,
            BoardActionNotificationType.TaskUpdated,
            currentUserInfo.Id,
            dateTimeProvider.UtcNow,
            new TaskUpdatedPayload(
                boardTask.ColumnId,
                boardTask.Id,
                boardTask.Title,
                boardTask.AssigneeId)), ct);

        if (descriptionChanged)
        {
            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskDescriptionChanged,
                currentUserInfo.Id,
                dateTimeProvider.UtcNow,
                new TaskDescriptionChangedPayload(boardTask.Id, boardTask.Description)), ct);
        }

        if (assigneeChanged)
        {
            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskAssigneeChanged,
                currentUserInfo.Id,
                dateTimeProvider.UtcNow,
                new TaskAssigneeChangedPayload(boardTask.Id, await ResolveAssigneeDtoAsync(boardTask.AssigneeId, ct))), ct);
        }

        if (completionChanged)
        {
            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskCompletionChanged,
                currentUserInfo.Id,
                dateTimeProvider.UtcNow,
                new TaskCompletionChangedPayload(boardTask.Id, boardTask.IsDone, boardTask.CompletedAtUtc)), ct);
        }

        if (storyPointsChanged)
        {
            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskStoryPointsChanged,
                currentUserInfo.Id,
                dateTimeProvider.UtcNow,
                new TaskStoryPointsChangedPayload(boardTask.Id, boardTask.StoryPoints)), ct);
        }

        if (timeboxChanged)
        {
            await boardActionNotifier.NotifyAsync(new BoardActionNotification(
                request.BoardId,
                BoardActionNotificationType.TaskTimeboxChanged,
                currentUserInfo.Id,
                dateTimeProvider.UtcNow,
                new TaskTimeboxChangedPayload(boardTask.Id, boardTask.TimeboxHours)), ct);
        }

        return Result.Success();
    }

    private async Task<UserDto?> ResolveAssigneeDtoAsync(Guid? assigneeId, CancellationToken ct)
    {
        if (assigneeId is not { } id)
            return null;

        var assigneeInfo = await userRepository.GetUserInfoByIdAsync(id, ct);

        return assigneeInfo.ToNullableDto(user =>
            user.ProfilePhotoVersion is null ? null : profilePhotoService.BuildThumbnailUrl(user.Id, user.ProfilePhotoVersion));
    }
}

public class UpdateBoardTaskCommandValidator : AbstractValidator<UpdateBoardTaskCommand>
{
    public UpdateBoardTaskCommandValidator()
    {
        RuleFor(x => x.BoardId)
            .NotEmpty();

        RuleFor(x => x.BoardTaskId)
            .NotEmpty();

        RuleFor(x => x)
            .Must(command => command.Title.IsSpecified
                || command.Description.IsSpecified
                || command.AssigneeId.IsSpecified
                || command.IsDone.IsSpecified
                || command.StoryPoints.IsSpecified
                || command.TimeboxHours.IsSpecified)
            .WithMessage("At least one field must be provided for update.");

        When(x => x.Title.IsSpecified, () =>
        {
            RuleFor(x => x.Title.Value)
                .Must(title => !string.IsNullOrWhiteSpace(title))
                .WithMessage("'Title' must not be empty.");

            When(x => !string.IsNullOrWhiteSpace(x.Title.Value), () =>
            {
                RuleFor(x => x.Title.Value)
                    .Must(title => title!.Trim().Length <= BoardTaskFieldLengths.MaxTitleLength)
                    .WithMessage($"'Title' must be {BoardTaskFieldLengths.MaxTitleLength} characters or fewer.");
            });
        });

        When(x => x.Description.IsSpecified && !string.IsNullOrWhiteSpace(x.Description.Value), () =>
        {
            RuleFor(x => x.Description.Value)
                .Must(description => description!.Trim().Length <= BoardTaskFieldLengths.MaxDescriptionLength)
                .WithMessage($"'Description' must be {BoardTaskFieldLengths.MaxDescriptionLength} characters or fewer.");
        });

        When(x => x.AssigneeId is { IsSpecified: true, Value: not null }, () =>
        {
            RuleFor(x => x.AssigneeId.Value)
                .NotEmpty();
        });

        When(x => x.StoryPoints is { IsSpecified: true, Value: not null }, () =>
        {
            RuleFor(x => x.StoryPoints.Value)
                .Must(storyPoints => StoryPointsScale.IsValid(storyPoints!.Value))
                .WithMessage("'StoryPoints' must be a valid Fibonacci scale value.");
        });

        When(x => x.TimeboxHours is { IsSpecified: true, Value: not null }, () =>
        {
            RuleFor(x => x.TimeboxHours.Value)
                .Must(timeboxHours => TimeboxScale.IsValid(timeboxHours!.Value))
                .WithMessage($"'TimeboxHours' must be between {TimeboxScale.MinHours} and {TimeboxScale.MaxHours}.");
        });
    }
}
