using Mirror.Analytics;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.LLM.Copilot;

public class LocalQueryPlanner
{
    private readonly IMirrorRepository _repository;
    private readonly DigitalRhythmService _rhythmService;

    public LocalQueryPlanner(IMirrorRepository repository)
    {
        _repository = repository;
        _rhythmService = new DigitalRhythmService();
    }

    public async Task<MirrorCopilotContext> PlanAndFetchAsync(
        CopilotIntent intent,
        DateTime referenceTimeLocal,
        CancellationToken ct = default)
    {
        DateTime startLocal = referenceTimeLocal.Date;
        DateTime endLocal = startLocal.AddDays(1);

        if (intent == CopilotIntent.DayComparison)
        {
            startLocal = referenceTimeLocal.Date.AddDays(-1);
        }
        else if (intent == CopilotIntent.WeeklyTrends)
        {
            startLocal = referenceTimeLocal.Date.AddDays(-6);
        }

        DateTime startUtc = startLocal.ToUniversalTime();
        DateTime endUtc = endLocal.ToUniversalTime();

        var sessions = await _repository.GetSessionsAsync(startUtc, endUtc, ct);
        var idles = await _repository.GetIdlePeriodsAsync(startUtc, endUtc, ct);
        var switches = await _repository.GetRecentSwitchesAsync(endUtc - startUtc, ct);
        var patterns = await _repository.GetPatternEventsAsync(startUtc, endUtc, ct);
        var baselines = await _repository.GetAllAdaptiveBaselinesAsync(14, ct);

        long activeSeconds = sessions.Sum(s => s.ActiveSeconds);
        long idleSeconds = idles.Sum(i => i.DurationSeconds);
        int switchCount = switches.Count;
        int sessionCount = sessions.Count;
        int uniqueApps = sessions.Select(s => s.AppKey).Distinct().Count();

        // Top apps
        var topApps = sessions
            .GroupBy(s => s.DisplayName)
            .Select(g => new AppUsageSummary(
                g.Key,
                Math.Round(g.Sum(x => x.ActiveSeconds) / 60.0, 1),
                activeSeconds > 0 ? Math.Round((double)g.Sum(x => x.ActiveSeconds) / activeSeconds * 100.0, 1) : 0.0))
            .OrderByDescending(a => a.ActiveMinutes)
            .Take(5)
            .ToList();

        // Top categories
        var topCategories = sessions
            .GroupBy(s => s.Category)
            .Select(g => new CategoryUsageSummary(
                g.Key,
                Math.Round(g.Sum(x => x.ActiveSeconds) / 60.0, 1),
                activeSeconds > 0 ? Math.Round((double)g.Sum(x => x.ActiveSeconds) / activeSeconds * 100.0, 1) : 0.0))
            .OrderByDescending(c => c.ActiveMinutes)
            .Take(4)
            .ToList();

        // Longest session
        var longestSession = sessions.OrderByDescending(s => s.ActiveSeconds).FirstOrDefault();
        string longestAppName = longestSession?.DisplayName ?? "None";
        double longestMinutes = longestSession != null ? Math.Round(longestSession.ActiveSeconds / 60.0, 1) : 0.0;

        // Late night seconds
        long lateNightSeconds = sessions
            .Where(s => s.StartUtc.ToLocalTime().Hour >= 23 || s.StartUtc.ToLocalTime().Hour < 6)
            .Sum(s => s.ActiveSeconds);

        // Rhythm peak
        var rhythmReport = _rhythmService.AnalyzeRhythm(referenceTimeLocal.Date, sessions, switches, baselines);
        string peakPeriod = rhythmReport.PeakPeriod.ToString();

        // Baseline comparison
        string baselineSummary = "Within normal 14-day baseline range";
        var activeBaseline = baselines.FirstOrDefault(b => b.MetricKey == "daily_active_seconds");
        if (activeBaseline != null && !activeBaseline.IsLearningPhase && activeBaseline.StdDev > 0)
        {
            double z = (activeSeconds - activeBaseline.MedianValue) / activeBaseline.StdDev;
            if (z >= 1.5)
                baselineSummary = $"+{z:F1}σ above your 14-day median ({activeBaseline.MedianValue / 3600.0:F1}h)";
            else if (z <= -1.5)
                baselineSummary = $"{z:F1}σ below your 14-day median ({activeBaseline.MedianValue / 3600.0:F1}h)";
        }

        // Verified factual statements for deterministic validation
        var facts = new List<string>
        {
            $"Active time: {activeSeconds / 3600.0:F1} hours",
            $"Application switches: {switchCount}",
            $"Recorded sessions: {sessionCount}",
            $"Unique applications: {uniqueApps}",
            $"Longest single session: {longestAppName} ({longestMinutes:F0} mins)",
            $"Peak activity period: {peakPeriod}",
            $"Late night activity: {lateNightSeconds / 3600.0:F1} hours"
        };

        if (topApps.Count > 0)
        {
            facts.Add($"Top application: {topApps[0].DisplayName} ({topApps[0].ActiveMinutes:F0}m, {topApps[0].PercentageOfTotal:F0}%)");
        }

        string rangeLabel = intent switch
        {
            CopilotIntent.DayComparison => "Yesterday vs Today",
            CopilotIntent.WeeklyTrends => "Past 7 Days",
            _ => referenceTimeLocal.ToString("MMMM dd, yyyy")
        };

        return new MirrorCopilotContext
        {
            DateOrRangeLabel = rangeLabel,
            ActiveHours = Math.Round(activeSeconds / 3600.0, 1),
            IdleHours = Math.Round(idleSeconds / 3600.0, 1),
            SwitchCount = switchCount,
            SessionCount = sessionCount,
            UniqueAppCount = uniqueApps,
            LateNightHours = Math.Round(lateNightSeconds / 3600.0, 1),
            LongestSessionAppName = longestAppName,
            LongestSessionMinutes = longestMinutes,
            TopApps = topApps,
            TopCategories = topCategories,
            DetectedPatterns = patterns.Select(p => p.PatternType.ToString()).Distinct().ToList(),
            BaselineComparison = baselineSummary,
            PeakRhythmPeriod = peakPeriod,
            VerifiedFacts = facts
        };
    }
}
