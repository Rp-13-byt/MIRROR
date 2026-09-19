using Mirror.Core.Domain;

namespace Mirror.Core.Models;

public record ActivitySession
{
    public long Id { get; init; }
    public required string AppKey { get; init; }
    public required string DisplayName { get; init; }
    public required string Category { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int ActiveSeconds { get; init; }
    public SessionCloseReason CloseReason { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public TimeSpan Duration => EndUtc >= StartUtc ? EndUtc - StartUtc : TimeSpan.Zero;
}

public record IdlePeriod
{
    public long Id { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int DurationSeconds { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}

public record AppSwitchEvent
{
    public long Id { get; init; }
    public required string FromAppKey { get; init; }
    public required string ToAppKey { get; init; }
    public DateTime TimestampUtc { get; init; }
}

public record PatternEvent
{
    public long Id { get; init; }
    public BehavioralPatternType PatternType { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public DateTime DetectedUtc { get; init; } = DateTime.UtcNow;
    public bool RuleSignal { get; init; }
    public bool ModelSignal { get; init; }
    public float ModelConfidence { get; init; }
    public required string Explanation { get; init; }
}

public record DailyMetrics
{
    public required string DateLocal { get; init; }
    public int ActiveSeconds { get; init; }
    public int IdleSeconds { get; init; }
    public int SwitchCount { get; init; }
    public int SessionCount { get; init; }
    public int UniqueAppCount { get; init; }
    public int LateNightSeconds { get; init; }
    public int LongestSessionSeconds { get; init; }
    public string? TopAppKey { get; init; }
}

public record AppCategoryOverride
{
    public required string AppKey { get; init; }
    public required string Category { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

public record AppIdentity(string AppKey, string DisplayName, string Category, bool IsExcluded = false);

public record FlowStateSession
{
    public long Id { get; init; }
    public required string AppKey { get; init; }
    public required string DisplayName { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public int DurationSeconds { get; init; }
    public int DistractionCount { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public TimeSpan Duration => EndUtc >= StartUtc ? EndUtc - StartUtc : TimeSpan.FromSeconds(DurationSeconds);
}

public record AdaptiveBaseline
{
    public required string MetricKey { get; init; }
    public int WindowDays { get; init; } = 14;
    public double MedianValue { get; init; }
    public double StdDev { get; init; }
    public int SampleCount { get; init; }
    public bool IsLearningPhase => SampleCount < 7;
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;

    public double ThresholdUpper => MedianValue + (2.0 * StdDev);
    public string LearningStatusMessage => IsLearningPhase
        ? $"Learning your baseline — {SampleCount} of 7 days recorded. Pattern alerts begin after Day 7."
        : "This is your personal baseline calculated from the last 14 days of local activity.";
}

public record PatternFeedback
{
    public long Id { get; init; }
    public long PatternEventId { get; init; }
    public BehavioralPatternType PatternType { get; init; }
    public required string FeedbackReason { get; init; }
    public required string AdjustedThresholdKey { get; init; }
    public double PreviousThresholdValue { get; init; }
    public double NewThresholdValue { get; init; }
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}

public record RecalibratedThreshold
{
    public required string ThresholdKey { get; init; }
    public double Value { get; init; }
    public DateTime UpdatedUtc { get; init; } = DateTime.UtcNow;
}

