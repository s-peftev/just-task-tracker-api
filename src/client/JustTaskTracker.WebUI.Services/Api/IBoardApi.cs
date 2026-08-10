using JustTaskTracker.WebUI.Domain.Boards;
using JustTaskTracker.WebUI.Domain.Boards.Requests;
using JustTaskTracker.WebUI.Services.Api.Models;
using Refit;
using JustTaskTracker.WebUI.Domain.Common.Pagination;

namespace JustTaskTracker.WebUI.Services.Api;

internal interface IBoardApi
{
    [Get("/api/boards")]
    Task<IApiResponse<ApiEnvelope<PagedList<BoardLookupDto>>>> GetMyAsync(
        int pageNumber,
        int pageSize,
        [AliasAs("SearchOptions.Search")] string? searchOptionsSearch = null,
        bool? isArchived = null,
        bool? isOwned = null,
        CancellationToken ct = default);

    [Get("/api/boards/owned/active/count")]
    Task<IApiResponse<ApiEnvelope<ActiveOwnedBoardsCountDto>>> GetActiveOwnedCountAsync(
        CancellationToken ct = default);

    [Get("/api/boards/{id}")]
    Task<IApiResponse<ApiEnvelope<BoardDetailsDto>>> GetByIdAsync(Guid id, CancellationToken ct = default);

    [Get("/api/boards/{boardId}/members")]
    Task<IApiResponse<ApiEnvelope<PagedList<BoardMemberDto>>>> GetMembersAsync(
        Guid boardId,
        int pageNumber,
        int pageSize,
        [AliasAs("SearchOptions.Search")] string? searchOptionsSearch = null,
        CancellationToken ct = default);

    [Post("/api/boards/{boardId}/members")]
    Task<IApiResponse<ApiEnvelope<object>>> AddMemberAsync(
        Guid boardId,
        [Body] AddBoardMemberRequest request,
        CancellationToken ct = default);

    [Put("/api/boards/{boardId}/members/{userId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateMemberAsync(
        Guid boardId,
        Guid userId,
        [Body] UpdateBoardMemberRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/members/{userId}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteMemberAsync(
        Guid boardId,
        Guid userId,
        CancellationToken ct = default);

    [Post("/api/boards")]
    Task<IApiResponse<ApiEnvelope<BoardDetailsDto>>> CreateAsync([Body] SaveBoardRequest request, CancellationToken ct = default);

    [Put("/api/boards/{id}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateAsync(Guid id, [Body] SaveBoardRequest request, CancellationToken ct = default);

    [Delete("/api/boards/{id}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteAsync(Guid id, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/leave")]
    Task<IApiResponse<ApiEnvelope<object>>> LeaveAsync(Guid boardId, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/archive/export")]
    Task<IApiResponse<ApiEnvelope<BoardArchivedDto>>> ArchiveAndExportAsync(
        Guid boardId,
        [Body] BoardExportOptions exportOptions,
        CancellationToken ct = default);

    [Get("/api/boards/{boardId}/archive/export")]
    Task<IApiResponse<ApiEnvelope<BoardArchiveDownloadDto>>> GetArchiveExportAsync(
        Guid boardId,
        CancellationToken ct = default);

    [Post("/api/boards/{boardId}/archive/re-export")]
    Task<IApiResponse<ApiEnvelope<object>>> ReExportArchivedAsync(
        Guid boardId,
        [Body] BoardExportOptions reExportOptions,
        CancellationToken ct = default);

    [Post("/api/boards/{boardId}/columns")]
    Task<IApiResponse<ApiEnvelope<ColumnDto>>> CreateColumnAsync(Guid boardId, [Body] SaveColumnRequest request, CancellationToken ct = default);

    [Put("/api/boards/{boardId}/columns/{columnId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateColumnAsync(Guid boardId, Guid columnId, [Body] SaveColumnRequest request, CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/columns/{columnId}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteColumnAsync(Guid boardId, Guid columnId, [Body] DeleteColumnRequest request, CancellationToken ct = default);

    [Put("/api/boards/{boardId}/columns/{columnId}/position")]
    Task<IApiResponse<ApiEnvelope<object>>> ReorderColumnAsync(Guid boardId, Guid columnId, [Body] ReorderColumnPositionRequest request, CancellationToken ct = default);

    [Put("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/position")]
    Task<IApiResponse<ApiEnvelope<object>>> ReorderTaskAsync(Guid boardId, Guid columnId, Guid taskId, [Body] ReorderTaskPositionRequest request, CancellationToken ct = default);

    [Post("/api/boards/{boardId}/columns/{columnId}/tasks")]
    Task<IApiResponse<ApiEnvelope<BoardTaskPreviewDto>>> CreateTaskAsync(Guid boardId, Guid columnId, [Body] SaveTaskRequest request, CancellationToken ct = default);

    [Get("/api/boards/{boardId}/columns/{columnId}/tasks")]
    Task<IApiResponse<ApiEnvelope<PagedList<BoardTaskLookupDto>>>> GetTaskLookupListAsync(
        Guid boardId,
        Guid columnId,
        int pageNumber,
        int pageSize,
        [AliasAs("SearchOptions.Search")] string? searchOptionsSearch = null,
        CancellationToken ct = default);

    [Get("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<BoardTaskDetailsDto>>> GetTaskByIdAsync(Guid boardId, Guid columnId, Guid taskId, CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskTitleAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskTitleRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskDescriptionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskDescriptionRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskAssigneeAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskAssigneeRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskCompletionAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskCompletionRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskStoryPointsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskStoryPointsRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskTimeboxAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] UpdateBoardTaskTimeboxRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteTaskAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        CancellationToken ct = default);

    [Multipart]
    [Post("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/attachments")]
    Task<IApiResponse<ApiEnvelope<BoardTaskAttachmentDto>>> UploadTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [AliasAs("file")] StreamPart file,
        CancellationToken ct = default);

    [Get("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/attachments/{attachmentId}")]
    Task<HttpResponseMessage> DownloadTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/attachments/{attachmentId}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteTaskAttachmentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid attachmentId,
        CancellationToken ct = default);

    [Get("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/comments")]
    Task<IApiResponse<ApiEnvelope<PagedList<BoardTaskCommentDto>>>> GetTaskCommentsAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        int pageNumber,
        int pageSize,
        CancellationToken ct = default);

    [Post("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/comments")]
    Task<IApiResponse<ApiEnvelope<BoardTaskCommentDto>>> CreateTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        [Body] CreateBoardTaskCommentRequest request,
        CancellationToken ct = default);

    [Patch("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/comments/{commentId}")]
    Task<IApiResponse<ApiEnvelope<object>>> UpdateTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        [Body] UpdateBoardTaskCommentRequest request,
        CancellationToken ct = default);

    [Delete("/api/boards/{boardId}/columns/{columnId}/tasks/{taskId}/comments/{commentId}")]
    Task<IApiResponse<ApiEnvelope<object>>> DeleteTaskCommentAsync(
        Guid boardId,
        Guid columnId,
        Guid taskId,
        Guid commentId,
        CancellationToken ct = default);
}
