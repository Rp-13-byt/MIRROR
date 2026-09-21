using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public enum TimeOfDayPeriod
{
    Morning,    // 06:00 - 12:00
    Afternoon,  // 12:00 - 18:00
    Evening,    // 18:00 - 23:00
    LateNight   // 23:00 - 06:00
}

public record PeriodActivitySummary
{
    public TimeOfDayPeriod Period { get; init; }
    public string PeriodLabel { get; init; } = string.Empty;
    public string TimeRangeLabel { get; init; } = string.Empty;
    public long ActiveSeconds { get; init; }
    public double ActiveHours => Math.Round(ActiveSeconds / 3600.0, 1);
    public double PercentageOfDay { get; init; }
    public int SwitchCount { get; init; }
    public string DominantCategory { get; init; } = "None";
    public string DominantApp { get; init; } = "None";
    public double BaselineExpectedHours { get; init; }
    public double DeviationHours => Math.Round(ActiveHours - BaselineExpectedHours, 1);
}

public record DigitalRhythmReport
{
    public DateTime Date { get; init; }
    public long TotalActiveSeconds { get; init; }
    public double TotalActiveHours => Math.Round(TotalActiveSeconds / 3600.0, 1);
    public IReadOnlyList<PeriodActivitySummary> Periods { get; init; } = Array.Empty<PeriodActivitySummary>();
    public TimeOfDayPeriod PeakPeriod { get; init; }
    public string RhythmAnalysis { get; init; } = string.Empty;
}

public class DigitalRhythmService
{
    public DigitalRhythmReport AnalyzeRhythm(
        DateTime localDate,
        IReadOnlyList<ActivitySession> daySessions,
        IReadOnlyList<AppSwitchEvent> daySwitches,
        IReadOnlyList<AdaptiveBaseline>? baselines = null)
    {
        long totalSeconds = daySessions.Sum(s => s.ActiveSeconds);
        var periodSummaries = new List<PeriodActivitySummary>();

        var periods = new[]
        {
            (TimeOfDayPeriod.Morning, "Morning", "06:00 - 12:00", 6, 12),
            (TimeOfDayPeriod.Afternoon, "Afternoon", "12:00 - 18:00", 12, 18),
            (TimeOfDayPeriod.Evening, "Evening", "18:00 - 23:00", 18, 23),
            (TimeOfDayPeriod.LateNight, "Late Night", "23:00 - 06:00", 23, 6)
        };

        foreach (var (period, label, timeRange, startHour, endHour) in periods)
        {
            var matchingSessions = daySessions.Where(s => IsInPeriod(s.StartUtc.ToLocalTime(), startHour, endHour)).ToList();
            var matchingSwitches = daySwitches.Where(sw => IsInPeriod(sw.TimestampUtc.ToLocalTime(), startHour, endHour)).ToList();

            long periodActiveSec = matchingSessions.Sum(s => s.ActiveSeconds);
            double percentage = totalSeconds > 0 ? (double)periodActiveSec / totalSeconds * 100.0 : 0.0;

            string domCategory = matchingSessions
                .GroupBy(s => s.Category)
                .OrderByDescending(g => g.Sum(x => x.ActiveSeconds))
                .FirstOrDefault()?.Key ?? "None";

            string domApp = matchingSessions
                .GroupBy(s => s.DisplayName)
                .OrderByDescending(g => g.Sum(x => x.ActiveSeconds))
                .FirstOrDefault()?.Key ?? "None";

            double expectedHours = 0.0;
            if (baselines != null)
            {
                string key = $"rhythm_{period.ToString().ToLowerInvariant()}_hours";
                var b = baselines.FirstOrDefault(x => x.MetricKey == key);
                if (b != null) expectedHours = Math.Round(b.MedianValue, 1);
            }

            periodSummaries.Add(new PeriodActivitySummary
            {
                Period = period,
                PeriodLabel = label,
                TimeRangeLabel = timeRange,
                ActiveSeconds = periodActiveSec,
                PercentageOfDay = Math.Round(percentage, 1),
                SwitchCount = matchingSwitches.Count,
                DominantCategory = domCategory,
                DominantApp = domApp,
                BaselineExpectedHours = expectedHours
            });
        }

        var peak = periodSummaries.OrderByDescending(p => p.ActiveSeconds).FirstOrDefault()?.Period ?? TimeOfDayPeriod.Morning;
        var peakSummary = periodSummaries.First(p => p.Period == peak);

        string analysis = totalSeconds == 0
            ? "No activity recorded for this date."
            : $"Your primary activity block occurred in the {peakSummary.PeriodLabel} ({peakSummary.ActiveHours:F1} hours, {peakSummary.PercentageOfDay:F0}% of daily total), primarily concentrated in {peakSummary.DominantApp}.";

        return new DigitalRhythmReport
        {
            Date = localDate,
            TotalActiveSeconds = totalSeconds,
            Periods = periodSummaries,
            PeakPeriod = peak,
            RhythmAnalysis = analysis
        };
    }

    private static bool IsInPeriod(DateTime localTime, int startHour, int endHour)
    {
        int hour = localTime.Hour;
        if (startHour < endHour)
        {
            return hour >= startHour && hour < endHour;
        }
        else
        {
            // Wraps around midnight (e.g., 23 to 6)
            return hour >= startHour || hour < endHour;
        }
    }
}
