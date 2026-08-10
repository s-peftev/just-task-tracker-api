using System.Text.Json.Serialization;

namespace JustTaskTracker.WebUI.Domain.Boards.Requests;

public record UpdateBoardTaskStoryPointsRequest(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    byte? StoryPoints);
