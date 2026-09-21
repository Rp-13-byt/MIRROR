namespace Mirror.Analytics;

/// <summary>
/// Multi-dimensional behavioral representation of an activity block.
/// Never contains raw window titles, URLs, or keystrokes.
/// </summary>
public record SessionFingerprint
{
    public double DurationMinutes { get; init; }
    public double SwitchRatePerMinute { get; init; }
    public int UniqueAppCount { get; init; }
    public int ReopenCount { get; init; }
    public int InterruptionCount { get; init; }
    public double ActiveRatio { get; init; }
    public string DominantCategory { get; init; } = "General";

    public static SessionFingerprint Create(
        double durationMinutes,
        int totalSwitches,
        int uniqueApps,
        int reopens,
        int interruptions,
        double activeRatio,
        string dominantCategory)
    {
        double safeDuration = Math.Max(durationMinutes, 0.1);
        return new SessionFingerprint
        {
            DurationMinutes = Math.Round(durationMinutes, 1),
            SwitchRatePerMinute = Math.Round(totalSwitches / safeDuration, 2),
            UniqueAppCount = uniqueApps,
            ReopenCount = reopens,
            InterruptionCount = interruptions,
            ActiveRatio = Math.Clamp(Math.Round(activeRatio, 2), 0.0, 1.0),
            DominantCategory = dominantCategory
        };
    }
}
