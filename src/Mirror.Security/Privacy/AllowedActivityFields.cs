namespace Mirror.Security.Privacy;

public static class AllowedActivityFields
{
    private static readonly HashSet<string> PermittedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppKey",
        "DisplayName",
        "Category",
        "TimestampUtc",
        "StartUtc",
        "EndUtc",
        "ActiveSeconds",
        "IdleSeconds",
        "DurationSeconds",
        "Duration",
        "CloseReason",
        "DistractionCount",
        "FromAppKey",
        "ToAppKey",
        "PatternType",
        "RuleSignal",
        "ModelSignal",
        "ModelConfidence",
        "Explanation",
        "ConfidenceCategory",
        "Confidence",
        "SwitchCount",
        "DistinctApplicationCount",
        "SessionDurationSeconds",
        "ReopenCount",
        "SwitchesPerMinute",
        "RuleTriggered",
        "RuleEvidence",
        "ModelSupported",
        "IsUncertain",
        "BackendUsed",
        "ModelVersion",
        "InferenceDuration",
        "MetricKey",
        "WindowDays",
        "MedianValue",
        "StdDev",
        "SampleCount",
        "ThresholdKey",
        "Value",
        "FeedbackReason",
        "AdjustedThresholdKey",
        "PreviousThresholdValue",
        "NewThresholdValue",
        "PlannedDurationSeconds",
        "ActualDurationSeconds",
        "ContextName",
        "UniqueAppCount",
        "Name",
        "Visibility",
        "CreatedUtc",
        "DetectedUtc",
        "UpdatedUtc",
        "Id",
        "IsExcluded"
    };

    public static bool IsFieldPermitted(string fieldName)
    {
        return PermittedFields.Contains(fieldName);
    }
}
