namespace JustTaskTracker.Domain.Boards.Constants;

public static class TimeboxScale
{
    public const short MinHours = 1;
    public const short MaxHours = 999;

    public static bool IsValid(short value) => value is >= MinHours and <= MaxHours;
}
