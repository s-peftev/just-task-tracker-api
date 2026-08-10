using System.Linq.Expressions;
using JustTaskTracker.Application.Calls.ReadModels;
using JustTaskTracker.Application.Calls.Repositories;
using JustTaskTracker.Application.Users.ReadModels;
using JustTaskTracker.Domain.Boards.DTOs.BoardTasks;
using JustTaskTracker.Domain.Calls.Entities;
using JustTaskTracker.Domain.Calls.Enums;
using JustTaskTracker.Domain.Common.Pagination;
using JustTaskTracker.Persistence.Common;
using JustTaskTracker.Persistence.Common.Extentions;
using Microsoft.EntityFrameworkCore;

namespace JustTaskTracker.Persistence.Calls.Repositories;

public class CallRepository(JustTaskTrackerDbContext context) : Repository<CallSession, Guid>(context), ICallRepository
{
    private static readonly Expression<Func<CallSession, CallSessionWithStateReadModel>> SessionWithStateProjection = s => new CallSessionWithStateReadModel(
        s.Id,
        s.BoardId,
        new UserReadModel(s.CreatedByUser!.Id, s.CreatedByUser.Email, s.CreatedByUser.DisplayName, s.CreatedByUser.ProfilePhotoVersion),
        s.Title,
        s.Topic,
        s.Visibility,
        s.AcsRoomId,
        s.Status,
        s.StartedAtUtc,
        s.AllowedParticipants
            .Select(a => new UserReadModel(a.User!.Id, a.User.Email, a.User.DisplayName, a.User.ProfilePhotoVersion))
            .ToList(),
        s.LinkedTasks
            .Select(t => new BoardTaskLookupDto(t.Task!.Id, t.Task.ColumnId, t.Task.Title, t.Task.Description, t.Task.Type))
            .ToList(),
        s.Participants
            .Where(p => p.LeftAtUtc == null)
            .Select(p => new UserReadModel(p.User!.Id, p.User.Email, p.User.DisplayName, p.User.ProfilePhotoVersion))
            .ToList(),
        s.CurrentPresenterUserId);

    public Task<CallSessionWithStateReadModel?> GetSessionWithStateAsync(Guid callSessionId, CancellationToken ct = default) =>
        _context.CallSessions
            .Where(s => s.Id == callSessionId)
            .Select(SessionWithStateProjection)
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<CallSessionWithStateReadModel>> GetActiveSessionsWithStateForBoardAsync(Guid boardId, CancellationToken ct = default) =>
        await _context.CallSessions
            .Where(s => s.BoardId == boardId && s.Status == CallStatus.Active)
            .Select(SessionWithStateProjection)
            .ToListAsync(ct);

    public Task<PagedList<CallSessionHistoryReadModel>> GetClosedSessionsWithStateForBoardAsync(Guid boardId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        _context.CallSessions
            .Where(s => s.BoardId == boardId && s.Status == CallStatus.Closed)
            .OrderByDescending(s => s.EndedAtUtc)
            .ToPagedAsync(
                s => new CallSessionHistoryReadModel(
                    s.Id,
                    s.Title,
                    s.Topic,
                    s.Visibility,
                    new UserReadModel(s.CreatedByUser!.Id, s.CreatedByUser.Email, s.CreatedByUser.DisplayName, s.CreatedByUser.ProfilePhotoVersion),
                    s.StartedAtUtc,
                    s.EndedAtUtc!.Value,
                    s.LinkedTasks
                        .Select(t => new BoardTaskLookupDto(t.Task!.Id, t.Task.ColumnId, t.Task.Title, t.Task.Description, t.Task.Type))
                        .ToList(),
                    s.Participants
                        .Select(p => new CallParticipantEventReadModel(
                            new UserReadModel(p.User!.Id, p.User.Email, p.User.DisplayName, p.User.ProfilePhotoVersion),
                            p.JoinedAtUtc,
                            p.LeftAtUtc))
                        .ToList(),
                    s.AllowedParticipants
                        .Select(a => new UserReadModel(a.User!.Id, a.User.Email, a.User.DisplayName, a.User.ProfilePhotoVersion))
                        .ToList()),
                pageNumber,
                pageSize,
                ct);

    public Task<CallSession?> GetByAcsRoomIdAsync(string acsRoomId, CancellationToken ct = default) =>
        _context.CallSessions.FirstOrDefaultAsync(s => s.AcsRoomId == acsRoomId, ct);

    public async Task<bool> TryAcquirePresenterAsync(Guid callSessionId, Guid userId, CancellationToken ct = default)
    {
        var rowsAffected = await _context.CallSessions
            .Where(s => s.Id == callSessionId && s.Status == CallStatus.Active && s.CurrentPresenterUserId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.CurrentPresenterUserId, userId), ct);

        return rowsAffected > 0;
    }

    public async Task<bool> TryReleasePresenterAsync(Guid callSessionId, Guid userId, CancellationToken ct = default)
    {
        var rowsAffected = await _context.CallSessions
            .Where(s => s.Id == callSessionId && s.CurrentPresenterUserId == userId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(s => s.CurrentPresenterUserId, (Guid?)null), ct);

        return rowsAffected > 0;
    }
}
