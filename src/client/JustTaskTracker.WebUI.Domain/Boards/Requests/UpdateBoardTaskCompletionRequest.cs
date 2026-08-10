using System.Text.Json.Serialization;

namespace JustTaskTracker.WebUI.Domain.Boards.Requests;

public record UpdateBoardTaskCompletionRequest(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    bool IsDone);
