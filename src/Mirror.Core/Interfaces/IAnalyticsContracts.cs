using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Core.Interfaces;

public record UserBaseline
{
    public double MedianDailyActiveSeconds { get; init; }
    public double MedianSwitchRatePerMinute { get; init; }
    public double MedianSessionLengthSeconds { get; init; }
    public double MedianLateNightActiveSeconds { get; init; }
    public double MedianAppEntropy { get; init; }
    public int SampleDaysCount { get; init; }
    public bool HasSufficientHistory => SampleDaysCount >= 3;
}

public interface IFeatureExtractor
{
    FeatureSequence ExtractFeatureSequence(
        IReadOnlyList<ActivitySession> sessions,
        IReadOnlyList<AppSwitchEvent> switches,
        DateTime windowEndUtc,
        int windowMinutes = 60);

    float[] ExtractMinuteFeatures(
        IReadOnlyList<ActivitySession> sessionsInMinute,
        IReadOnlyList<AppSwitchEvent> switchesInMinute,
        DateTime minuteStartUtc);
}

public interface IBaselineAnalyzer
{
    Task<UserBaseline> ComputeBaselineAsync(int lookbackDays = 14, CancellationToken ct = default);
}

public interface IPatternDetector
{
    IReadOnlyList<PatternEvent> EvaluateRules(
        IReadOnlyList<ActivitySession> recentSessions,
        IReadOnlyList<AppSwitchEvent> recentSwitches,
        UserSettings settings,
        UserBaseline? baseline,
        DateTime evaluationTimeUtc);
}

public interface IPatternExplanationBuilder
{
    string BuildExplanation(
        BehavioralPatternType pattern,
        int switchCount,
        int uniqueApps,
        TimeSpan sessionDuration,
        double lateNightMinutes,
        double baselineComparisonMinutes);
}

public interface IPatternFusionEngine
{
    PatternEvent? FuseSignals(
        PatternEvent? ruleCandidate,
        InferenceResult? mlInference,
        float minimumConfidenceThreshold = 0.65f);

    PatternAgreementDiagnostics GetAgreementDiagnostics();
}

public interface IFlowStateDetector
{
    Task<FlowStateSession?> EvaluateFlowStateAsync(
        IReadOnlyList<ActivitySession> recentSessions,
        IReadOnlyList<AppSwitchEvent> recentSwitches,
        DateTime evaluationTimeUtc,
        CancellationToken ct = default);
}

public interface IAdaptiveBaselineService
{
    Task<AdaptiveBaseline> ComputeAdaptiveBaselineAsync(string metricKey, int windowDays = 14, CancellationToken ct = default);
    Task<IReadOnlyList<AdaptiveBaseline>> GetAllBaselinesAsync(int windowDays = 14, CancellationToken ct = default);
    bool IsExceedingBaseline(double currentValue, AdaptiveBaseline baseline, double sigmaMultiplier = 2.0);
}

public interface IThresholdRecalibrator
{
    Task<double> RecalibrateThresholdAsync(BehavioralPatternType patternType, string reason, long patternEventId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, double>> GetActiveThresholdsAsync(CancellationToken ct = default);
    Task ResetThresholdsAsync(CancellationToken ct = default);
}

public interface IInsightExplorerService
{
    PatternEvidence BuildEvidence(PatternEvent patternEvent, UserSettings? settings = null, UserBaseline? baseline = null);
    IReadOnlyList<PatternEvidence> BuildEvidenceBatch(IEnumerable<PatternEvent> patternEvents, UserSettings? settings = null, UserBaseline? baseline = null);
}

