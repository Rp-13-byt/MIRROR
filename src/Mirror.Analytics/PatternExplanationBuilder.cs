using Mirror.Core.Domain;
using Mirror.Core.Interfaces;

namespace Mirror.Analytics;

public class PatternExplanationBuilder : IPatternExplanationBuilder
{
    public string BuildExplanation(
        BehavioralPatternType pattern,
        int switchCount,
        int uniqueApps,
        TimeSpan sessionDuration,
        double lateNightMinutes,
        double baselineComparisonMinutes)
    {
        return pattern switch
        {
            BehavioralPatternType.HighSwitchingBurst =>
                $"Several application switches occurred within a short window ({switchCount} switches across {uniqueApps} applications).",

            BehavioralPatternType.ExtendedSingleAppSession =>
                $"One application remained active for an extended continuous period ({(int)sessionDuration.TotalMinutes} minutes).",

            BehavioralPatternType.LateNightUsageSpike =>
                baselineComparisonMinutes > 0
                    ? $"Usage during late hours ({(int)lateNightMinutes} minutes) was higher than your recent local baseline (median {(int)baselineComparisonMinutes} minutes)."
                    : $"Usage during late hours was elevated ({(int)lateNightMinutes} minutes).",

            BehavioralPatternType.RapidReopenPattern =>
                $"The same application was reopened repeatedly within a short period ({switchCount} returns).",

            BehavioralPatternType.CompositeScrollLike =>
                "A continuous session with repeated app reopens and low application diversity was observed during late hours.",

            _ => "Routine computer activity."
        };
    }
}
