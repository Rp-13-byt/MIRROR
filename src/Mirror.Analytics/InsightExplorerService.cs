using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class InsightExplorerService : IInsightExplorerService
{
    public PatternEvidence BuildEvidence(PatternEvent patternEvent, UserSettings? settings = null, UserBaseline? baseline = null)
    {
        var category = patternEvent.ModelConfidence switch
        {
            >= 0.85f => ConfidenceCategory.High,
            >= 0.65f => ConfidenceCategory.Moderate,
            _ => ConfidenceCategory.Tentative
        };

        string ruleEvidence = patternEvent.PatternType switch
        {
            BehavioralPatternType.HighSwitchingBurst =>
                $"{patternEvent.SwitchCount} app switches across {patternEvent.UniqueApps} applications within {Math.Max(1, (int)(patternEvent.EndUtc - patternEvent.StartUtc).TotalMinutes)} minutes",
            BehavioralPatternType.ExtendedSingleAppSession =>
                $"Continuous engagement for {Math.Round(TimeSpan.FromSeconds(patternEvent.DurationSeconds).TotalMinutes, 1)} minutes in a single application",
            BehavioralPatternType.RapidReopenPattern =>
                $"Frequent reopening cycle detected with {patternEvent.SwitchCount} switches within active window",
            BehavioralPatternType.LateNightUnwindPattern =>
                $"Usage during designated late-night window ({patternEvent.LateNightMinutes:F0} minutes)",
            BehavioralPatternType.CompositeScrollLike =>
                $"High frequency alternation across {patternEvent.UniqueApps} apps consistent with content scanning",
            _ => $"Rule triggered based on session parameters at {patternEvent.StartUtc:HH:mm} UTC"
        };

        string plainExplanation = patternEvent.PatternType switch
        {
            BehavioralPatternType.HighSwitchingBurst =>
                "Frequent switching between multiple applications without sustained dwell time. Often reflects multi-tasking, cross-referencing information, or interruption.",
            BehavioralPatternType.ExtendedSingleAppSession =>
                "Deep immersion in a single application without task switching.",
            BehavioralPatternType.RapidReopenPattern =>
                "An application was closed or minimized and reopened within moments.",
            BehavioralPatternType.LateNightUnwindPattern =>
                "Digital activity recorded during your configured evening or night rest period.",
            BehavioralPatternType.CompositeScrollLike =>
                "Rapid alternating usage patterns across interactive windows.",
            BehavioralPatternType.Uncertain =>
                "The local AI model observed mixed signals and abstained from classifying this period.",
            _ => string.IsNullOrWhiteSpace(patternEvent.Explanation)
                ? "Observed pattern in your local application usage."
                : patternEvent.Explanation
        };

        return new PatternEvidence
        {
            PatternType = patternEvent.PatternType,
            StartUtc = patternEvent.StartUtc,
            EndUtc = patternEvent.EndUtc,
            SwitchCount = patternEvent.SwitchCount,
            DistinctApplicationCount = patternEvent.UniqueApps,
            SessionDurationSeconds = patternEvent.DurationSeconds,
            ReopenCount = patternEvent.PatternType == BehavioralPatternType.RapidReopenPattern ? patternEvent.SwitchCount / 2 : 0,
            RuleTriggered = patternEvent.RuleSignal,
            RuleEvidence = ruleEvidence,
            ModelSupported = patternEvent.ModelSignal,
            ModelConfidence = patternEvent.ModelConfidence,
            ConfidenceCategory = category,
            Explanation = plainExplanation
        };
    }

    public IReadOnlyList<PatternEvidence> BuildEvidenceBatch(
        IEnumerable<PatternEvent> patternEvents,
        UserSettings? settings = null,
        UserBaseline? baseline = null)
    {
        return patternEvents.Select(p => BuildEvidence(p, settings, baseline)).ToList();
    }
}
