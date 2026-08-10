using JustTaskTracker.WebUI.Domain.Boards.Enums;

namespace JustTaskTracker.WebUI.Domain.Boards.Requests;

public record SaveTaskRequest(string Title, BoardTaskType Type);
