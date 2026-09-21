namespace Mirror.LLM.Copilot;

/// <summary>
/// Strictly typed, privacy-guaranteed local context container for Mirror Copilot.
/// Architecturally forbids raw window titles, URLs, document paths, or keystroke telemetry.
/// </summary>
public record MirrorCopilotContext
{
    public string DateOrRangeLabel { get; init; } = "Today";
    public double ActiveHours { get; init; }
    public double IdleHours { get; init; }
    public int SwitchCount { get; init; }
    public int SessionCount { get; init; }
    public int UniqueAppCount { get; init; }
    public double LateNightHours { get; init; }
    public string LongestSessionAppName { get; init; } = "None";
    public double LongestSessionMinutes { get; init; }
    public IReadOnlyList<AppUsageSummary> TopApps { get; init; } = Array.Empty<AppUsageSummary>();
    public IReadOnlyList<CategoryUsageSummary> TopCategories { get; init; } = Array.Empty<CategoryUsageSummary>();
    public IReadOnlyList<string> DetectedPatterns { get; init; } = Array.Empty<string>();
    public string BaselineComparison { get; init; } = "Within normal baseline ranges";
    public string PeakRhythmPeriod { get; init; } = "Morning";
    public IReadOnlyList<string> VerifiedFacts { get; init; } = Array.Empty<string>();
}

public record AppUsageSummary(string DisplayName, double ActiveMinutes, double PercentageOfTotal);

public record CategoryUsageSummary(string CategoryName, double ActiveMinutes, double PercentageOfTotal);
