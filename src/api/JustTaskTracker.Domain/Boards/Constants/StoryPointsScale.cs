namespace JustTaskTracker.Domain.Boards.Constants;

public static class StoryPointsScale
{
    public static readonly IReadOnlySet<byte> AllowedValues = new HashSet<byte> { 0, 1, 2, 3, 5, 8, 13, 21, 34 };
    public static bool IsValid(byte value) => AllowedValues.Contains(value);
}
