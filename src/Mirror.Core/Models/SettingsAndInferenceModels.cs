using Mirror.Core.Domain;

namespace Mirror.Core.Models;

public record UserSettings
{
    public bool TrackingEnabled { get; init; } = true;
    public bool StartWithWindows { get; init; } = false;
    public int IdleThresholdSeconds { get; init; } = 120;
    public int RetentionDays { get; init; } = 90;

    // Behavioral Pattern Thresholds
    public int HighSwitchWindowMinutes { get; init; } = 10;
    public int HighSwitchThreshold { get; init; } = 10;
    public int HighSwitchMinApps { get; init; } = 4;
    public int ExtendedSessionMinutes { get; init; } = 45;
    public int RapidReopenWindowMinutes { get; init; } = 20;
    public int RapidReopenThreshold { get; init; } = 4;
    public int LateNightStartHour { get; init; } = 23;
    public int LateNightEndHour { get; init; } = 4;

    // Rolling Baseline
    public int BaselineDays { get; init; } = 14;

    // Notifications
    public bool NotificationsEnabled { get; init; } = true;
    public bool DailySummaryNotification { get; init; } = true;
    public int QuietHoursStart { get; init; } = 23;
    public int QuietHoursEnd { get; init; } = 7;

    // Excluded applications
    public List<string> ExcludedApps { get; init; } = new();
}

public class FeatureSequence
{
    public const int DefaultTimesteps = 60;
    public const int DefaultFeatureCount = 10;

    public int Timesteps { get; init; } = DefaultTimesteps;
    public int FeatureCount { get; init; } = DefaultFeatureCount;
    public float[,] Values { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public FeatureSequence(int timesteps = DefaultTimesteps, int featureCount = DefaultFeatureCount)
    {
        Timesteps = timesteps;
        FeatureCount = featureCount;
        Values = new float[timesteps, featureCount];
    }

    public float[] Flatten()
    {
        var flat = new float[Timesteps * FeatureCount];
        int idx = 0;
        for (int t = 0; t < Timesteps; t++)
        {
            for (int f = 0; f < FeatureCount; f++)
            {
                flat[idx++] = Values[t, f];
            }
        }
        return flat;
    }
}

public record InferenceResult
{
    public InferenceBackendKind BackendUsed { get; init; }
    public BehavioralPatternType DetectedPattern { get; init; }
    public float Confidence { get; init; }
    public required float[] ClassProbabilities { get; init; }
    public double InferenceTimeMs { get; init; }
    public required string ModelVersion { get; init; }
}

public record ModelMetadata
{
    public required string ModelName { get; init; }
    public required string Version { get; init; }
    public required int[] InputShape { get; init; }
    public bool Quantized { get; init; }
    public required string TrainingDataset { get; init; }
    public int FeatureSchemaVersion { get; init; }
    public required string[] FeatureNames { get; init; }
}
