using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class PatternDetector : IPatternDetector
{
    private readonly IPatternExplanationBuilder _explanationBuilder;

    public PatternDetector(IPatternExplanationBuilder? explanationBuilder = null)
    {
        _explanationBuilder = explanationBuilder ?? new PatternExplanationBuilder();
    }

    public IReadOnlyList<PatternEvent> EvaluateRules(
        IReadOnlyList<ActivitySession> recentSessions,
        IReadOnlyList<AppSwitchEvent> recentSwitches,
        UserSettings settings,
        UserBaseline? baseline,
        DateTime evaluationTimeUtc)
    {
        var detected = new List<PatternEvent>();

        // 1. Pattern A: High app-switching burst
        var highSwitchEvent = CheckHighSwitchingBurst(recentSwitches, settings, evaluationTimeUtc);
        if (highSwitchEvent != null) detected.Add(highSwitchEvent);

        // 2. Pattern B: Extended single-app session
        var extendedSessionEvent = CheckExtendedSingleAppSession(recentSessions, settings, evaluationTimeUtc);
        if (extendedSessionEvent != null) detected.Add(extendedSessionEvent);

        // 3. Pattern C: Late-night usage spike
        var lateNightEvent = CheckLateNightSpike(recentSessions, settings, baseline, evaluationTimeUtc);
        if (lateNightEvent != null) detected.Add(lateNightEvent);

        // 4. Pattern D: Rapid reopen pattern
        var rapidReopenEvent = CheckRapidReopen(recentSwitches, settings, evaluationTimeUtc);
        if (rapidReopenEvent != null) detected.Add(rapidReopenEvent);

        // 5. Composite Scroll-Like Pattern
        var compositeEvent = CheckCompositeScrollLike(recentSessions, recentSwitches, settings, evaluationTimeUtc);
        if (compositeEvent != null) detected.Add(compositeEvent);

        return detected;
    }

    private PatternEvent? CheckHighSwitchingBurst(
        IReadOnlyList<AppSwitchEvent> switches,
        UserSettings settings,
        DateTime evaluationTimeUtc)
    {
        DateTime windowStart = evaluationTimeUtc.AddMinutes(-settings.HighSwitchWindowMinutes);
        var inWindow = switches.Where(s => s.TimestampUtc >= windowStart && s.TimestampUtc <= evaluationTimeUtc).ToList();

        if (inWindow.Count >= settings.HighSwitchThreshold)
        {
            var distinctApps = inWindow.Select(s => s.ToAppKey)
                .Concat(inWindow.Select(s => s.FromAppKey))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (distinctApps.Count >= settings.HighSwitchMinApps)
            {
                string explanation = _explanationBuilder.BuildExplanation(
                    BehavioralPatternType.HighSwitchingBurst,
                    inWindow.Count,
                    distinctApps.Count,
                    TimeSpan.FromMinutes(settings.HighSwitchWindowMinutes),
                    0, 0);

                return new PatternEvent
                {
                    PatternType = BehavioralPatternType.HighSwitchingBurst,
                    StartUtc = inWindow.Min(s => s.TimestampUtc),
                    EndUtc = evaluationTimeUtc,
                    DetectedUtc = evaluationTimeUtc,
                    RuleSignal = true,
                    ModelSignal = false,
                    ModelConfidence = 1.0f,
                    Explanation = explanation
                };
            }
        }

        return null;
    }

    private PatternEvent? CheckExtendedSingleAppSession(
        IReadOnlyList<ActivitySession> sessions,
        UserSettings settings,
        DateTime evaluationTimeUtc)
    {
        int thresholdSec = settings.ExtendedSessionMinutes * 60;
        // Check current or recently active session
        var candidate = sessions
            .Where(s => s.EndUtc >= evaluationTimeUtc.AddMinutes(-5))
            .OrderByDescending(s => s.ActiveSeconds)
            .FirstOrDefault();

        if (candidate != null && candidate.ActiveSeconds >= thresholdSec)
        {
            string explanation = _explanationBuilder.BuildExplanation(
                BehavioralPatternType.ExtendedSingleAppSession,
                0, 1,
                TimeSpan.FromSeconds(candidate.ActiveSeconds),
                0, 0);

            return new PatternEvent
            {
                PatternType = BehavioralPatternType.ExtendedSingleAppSession,
                StartUtc = candidate.StartUtc,
                EndUtc = candidate.EndUtc,
                DetectedUtc = evaluationTimeUtc,
                RuleSignal = true,
                ModelSignal = false,
                ModelConfidence = 1.0f,
                Explanation = explanation
            };
        }

        return null;
    }

    private PatternEvent? CheckLateNightSpike(
        IReadOnlyList<ActivitySession> sessions,
        UserSettings settings,
        UserBaseline? baseline,
        DateTime evaluationTimeUtc)
    {
        DateTime localNow = evaluationTimeUtc.ToLocalTime();
        bool isCurrentlyLateNight = localNow.Hour >= settings.LateNightStartHour || localNow.Hour < settings.LateNightEndHour;
        if (!isCurrentlyLateNight) return null;

        // Calculate active minutes in the last 2 hours
        DateTime twoHoursAgo = evaluationTimeUtc.AddHours(-2);
        double lateNightActiveSec = sessions
            .Where(s => s.StartUtc >= twoHoursAgo && s.EndUtc <= evaluationTimeUtc)
            .Sum(s => (double)s.ActiveSeconds);

        double lateNightActiveMinutes = lateNightActiveSec / 60.0;

        // Compare against baseline if available, or static threshold if not
        double baselineMinutes = baseline != null && baseline.HasSufficientHistory
            ? (baseline.MedianLateNightActiveSeconds / 60.0)
            : 20.0;

        if (lateNightActiveMinutes >= 45.0 && lateNightActiveMinutes > (baselineMinutes * 1.5))
        {
            string explanation = _explanationBuilder.BuildExplanation(
                BehavioralPatternType.LateNightUsageSpike,
                0, 0, TimeSpan.Zero,
                lateNightActiveMinutes,
                baseline != null && baseline.HasSufficientHistory ? baselineMinutes : 0);

            return new PatternEvent
            {
                PatternType = BehavioralPatternType.LateNightUsageSpike,
                StartUtc = twoHoursAgo,
                EndUtc = evaluationTimeUtc,
                DetectedUtc = evaluationTimeUtc,
                RuleSignal = true,
                ModelSignal = false,
                ModelConfidence = 1.0f,
                Explanation = explanation
            };
        }

        return null;
    }

    private PatternEvent? CheckRapidReopen(
        IReadOnlyList<AppSwitchEvent> switches,
        UserSettings settings,
        DateTime evaluationTimeUtc)
    {
        DateTime windowStart = evaluationTimeUtc.AddMinutes(-settings.RapidReopenWindowMinutes);
        var inWindow = switches.Where(s => s.TimestampUtc >= windowStart && s.TimestampUtc <= evaluationTimeUtc).ToList();

        // Group returns by destination app
        var reopenGroup = inWindow
            .GroupBy(s => s.ToAppKey, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();

        if (reopenGroup != null && reopenGroup.Count() >= settings.RapidReopenThreshold)
        {
            string explanation = _explanationBuilder.BuildExplanation(
                BehavioralPatternType.RapidReopenPattern,
                reopenGroup.Count(),
                1, TimeSpan.Zero, 0, 0);

            return new PatternEvent
            {
                PatternType = BehavioralPatternType.RapidReopenPattern,
                StartUtc = reopenGroup.Min(s => s.TimestampUtc),
                EndUtc = evaluationTimeUtc,
                DetectedUtc = evaluationTimeUtc,
                RuleSignal = true,
                ModelSignal = false,
                ModelConfidence = 1.0f,
                Explanation = explanation
            };
        }

        return null;
    }

    private PatternEvent? CheckCompositeScrollLike(
        IReadOnlyList<ActivitySession> sessions,
        IReadOnlyList<AppSwitchEvent> switches,
        UserSettings settings,
        DateTime evaluationTimeUtc)
    {
        // 30-minute window
        DateTime windowStart = evaluationTimeUtc.AddMinutes(-30);
        var inWindowSessions = sessions.Where(s => s.EndUtc >= windowStart && s.StartUtc <= evaluationTimeUtc).ToList();
        var inWindowSwitches = switches.Where(s => s.TimestampUtc >= windowStart && s.TimestampUtc <= evaluationTimeUtc).ToList();

        double totalActiveSec = inWindowSessions.Sum(s => (double)s.ActiveSeconds);
        var distinctApps = inWindowSessions.Select(s => s.AppKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        // Check repeated reopen in same window
        int maxReopens = inWindowSwitches
            .GroupBy(s => s.ToAppKey, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Count())
            .DefaultIfEmpty(0)
            .Max();

        DateTime localNow = evaluationTimeUtc.ToLocalTime();
        bool isLateNight = localNow.Hour >= settings.LateNightStartHour || localNow.Hour < settings.LateNightEndHour;

        // Composite signals:
        // 1. Long active time in 30-min window (> 20 min)
        // 2. Low app diversity (<= 2 distinct apps)
        // 3. Repeated returns (>= 3 reopens)
        // 4. Late night hour
        if (totalActiveSec >= 1200 && distinctApps.Count <= 2 && maxReopens >= 3 && isLateNight)
        {
            string explanation = _explanationBuilder.BuildExplanation(
                BehavioralPatternType.CompositeScrollLike,
                maxReopens, distinctApps.Count,
                TimeSpan.FromSeconds(totalActiveSec),
                totalActiveSec / 60.0, 0);

            return new PatternEvent
            {
                PatternType = BehavioralPatternType.CompositeScrollLike,
                StartUtc = windowStart,
                EndUtc = evaluationTimeUtc,
                DetectedUtc = evaluationTimeUtc,
                RuleSignal = true,
                ModelSignal = false,
                ModelConfidence = 1.0f,
                Explanation = explanation
            };
        }

        return null;
    }
}
