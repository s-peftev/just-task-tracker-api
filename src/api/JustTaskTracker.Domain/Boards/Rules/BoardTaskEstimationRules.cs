using JustTaskTracker.Domain.Boards.Constants;
using JustTaskTracker.Domain.Boards.Enums;

namespace JustTaskTracker.Domain.Boards.Rules;

public static class BoardTaskEstimationRules
{
    public static bool CanHaveStoryPoints(BoardTaskType type) => type is BoardTaskType.Story;

    public static bool CanHaveTimebox(BoardTaskType type) => type is BoardTaskType.Spike;

    public static bool Validate(BoardTaskType type, byte? storyPoints, short? timeboxHours) => type switch
    {
        BoardTaskType.Story =>
            timeboxHours is null
            && (storyPoints is null || StoryPointsScale.IsValid(storyPoints.Value)),

        BoardTaskType.Bug =>
            storyPoints is null && timeboxHours is null,

        BoardTaskType.Spike =>
            storyPoints is null
            && (timeboxHours is null || TimeboxScale.IsValid(timeboxHours.Value)),

        _ => false
    };
}
