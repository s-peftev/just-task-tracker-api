using System.Text.Json.Serialization;

namespace JustTaskTracker.WebUI.Domain.Boards.Requests;

public record UpdateBoardTaskTimeboxRequest(
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    short? TimeboxHours);
