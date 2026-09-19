namespace Mirror.Reporting;

public record WeeklyReportAppUsage(string DisplayName, string Category, int ActiveSeconds, double Percentage);

public record WeeklyReportData
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int TotalActiveSeconds { get; init; }
    public int TotalIdleSeconds { get; init; }
    public int TotalSwitches { get; init; }
    public int LateNightSeconds { get; init; }
    public int FlowSessionsCount { get; init; }
    public int FlowTotalSeconds { get; init; }
    public int PatternEventsCount { get; init; }
    public required string BaselineStatus { get; init; }
    public List<WeeklyReportAppUsage> TopApps { get; init; } = new();

    public string DateRangeString => $"{StartDate:MMM dd, yyyy} - {EndDate:MMM dd, yyyy}";
    public string FormattedTotalActive => $"{TotalActiveSeconds / 3600}h {(TotalActiveSeconds % 3600) / 60}m";
    public string FormattedDailyAvg => $"{(TotalActiveSeconds / 7) / 3600}h {((TotalActiveSeconds / 7) % 3600) / 60}m/day";
    public string FormattedFlowTime => $"{FlowTotalSeconds / 3600}h {(FlowTotalSeconds % 3600) / 60}m";
    public string FormattedLateNight => $"{LateNightSeconds / 3600}h {(LateNightSeconds % 3600) / 60}m";
}