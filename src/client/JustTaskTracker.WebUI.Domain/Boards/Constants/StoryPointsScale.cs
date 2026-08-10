namespace JustTaskTracker.WebUI.Domain.Boards.Constants;

public static class StoryPointsScale
{
    public static readonly IReadOnlyList<byte> AllowedValues = [0, 1, 2, 3, 5, 8, 13, 21, 34];

    public static bool IsValid(byte value) => AllowedValues.Contains(value);
}
