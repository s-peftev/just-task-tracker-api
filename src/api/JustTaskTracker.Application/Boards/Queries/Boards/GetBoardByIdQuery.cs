using JustTaskTracker.Application.Auth;
using JustTaskTracker.Application.Billing.Abstractions;
using JustTaskTracker.Application.Boards.Mappings;
using JustTaskTracker.Application.Boards.Repositories;
using JustTaskTracker.Application.Common.ExternalProviders;
using JustTaskTracker.Application.Users.ProfilePhotos;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.Authorization;
using JustTaskTracker.Domain.Boards.DTOs.Boards;
using JustTaskTracker.Domain.Common.Results;
using JustTaskTracker.Domain.Common.Results.Errors;
using MediatR;

namespace JustTaskTracker.Application.Boards.Queries.Boards;

public record GetBoardByIdQuery(Guid BoardId) : IRequest<Result<BoardDetailsDto>>;

public class GetBoardByIdQueryHandler(
    ICurrentUserAccessor currentUserAccessor,
    IBoardRepository boardRepository,
    IBoardExportService boardExportService,
    IEntitlementService entitlementService,
    IProfilePhotoService profilePhotoService)
    : IRequestHandler<GetBoardByIdQuery, Result<BoardDetailsDto>>
{
    public async Task<Result<BoardDetailsDto>> Handle(GetBoardByIdQuery request, CancellationToken ct)
    {
        var userRole = await boardRepository.GetUserRoleAsync(request.BoardId, currentUserAccessor.AzureAdObjectId, ct);

        if (userRole is not { } authorizedRole || !BoardRolePermissions.CanViewBoard(authorizedRole))
            return Result<BoardDetailsDto>.Failure(GeneralErrors.Forbidden);

        var board = await boardRepository.GetBoardDetailsByIdAsync(request.BoardId, currentUserAccessor.AzureAdObjectId, ct);

        if (board is null)
            return Result<BoardDetailsDto>.Failure(GeneralErrors.NotFound);

        if (board.OwnerUserId is null)
            return Result<BoardDetailsDto>.Failure(GeneralErrors.NotFound);

        var ownerEntitlements = await entitlementService.GetEntitlementsAsync(board.OwnerUserId.Value, ct);
        var exportInfo = board.IsArchived
            ? await boardExportService.GetBoardExportInfoAsync(board.Id, ct)
            : null;

        Func<UserReadModel, string?> profilePhotoUrlResolver = user =>
            user.ProfilePhotoVersion is null
                ? null
                : profilePhotoService.BuildThumbnailUrl(user.Id, user.ProfilePhotoVersion);

        return Result<BoardDetailsDto>.Success(
            board.ToDto(ownerEntitlements.Limits.ToBoardLimits(), exportInfo, profilePhotoUrlResolver));
    }
}
