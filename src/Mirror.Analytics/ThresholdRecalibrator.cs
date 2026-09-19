using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class ThresholdRecalibrator : IThresholdRecalibrator
{
    private readonly IMirrorRepository _repository;

    public ThresholdRecalibrator(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<double> RecalibrateThresholdAsync(
        BehavioralPatternType patternType,
        string reason,
        long patternEventId,
        CancellationToken ct = default)
    {
        var existingThresholds = await _repository.GetRecalibratedThresholdsAsync(ct);
        string key = GetThresholdKey(patternType);
        double defaultValue = GetDefaultThreshold(patternType);

        double currentVal = existingThresholds.TryGetValue(key, out var val) ? val : defaultValue;
        // Bump by 15% (rounded up to nearest integer or 0.5)
        double newVal = Math.Ceiling(currentVal * 1.15);

        // Store feedback
        var feedback = new PatternFeedback
        {
            PatternEventId = patternEventId,
            PatternType = patternType,
            FeedbackReason = reason,
            AdjustedThresholdKey = key,
            PreviousThresholdValue = currentVal,
            NewThresholdValue = newVal,
            CreatedUtc = DateTime.UtcNow
        };

        await _repository.InsertPatternFeedbackAsync(feedback, ct);
        await _repository.UpsertRecalibratedThresholdAsync(key, newVal, ct);

        return newVal;
    }

    public async Task<IReadOnlyDictionary<string, double>> GetActiveThresholdsAsync(CancellationToken ct = default)
    {
        var custom = await _repository.GetRecalibratedThresholdsAsync(ct);
        var result = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase)
        {
            [GetThresholdKey(BehavioralPatternType.HighSwitchingBurst)] = 10,
            [GetThresholdKey(BehavioralPatternType.ExtendedSingleAppSession)] = 45,
            [GetThresholdKey(BehavioralPatternType.LateNightUsageSpike)] = 45,
            [GetThresholdKey(BehavioralPatternType.RapidReopenPattern)] = 4
        };

        foreach (var kvp in custom)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    public async Task ResetThresholdsAsync(CancellationToken ct = default)
    {
        await _repository.ResetRecalibratedThresholdsAsync(ct);
    }

    public static string GetThresholdKey(BehavioralPatternType patternType) => patternType switch
    {
        BehavioralPatternType.HighSwitchingBurst => "threshold_high_switch",
        BehavioralPatternType.ExtendedSingleAppSession => "threshold_extended_session_min",
        BehavioralPatternType.LateNightUsageSpike => "threshold_late_night_min",
        BehavioralPatternType.RapidReopenPattern => "threshold_rapid_reopen",
        _ => "threshold_" + patternType.ToString().ToLowerInvariant()
    };

    private static double GetDefaultThreshold(BehavioralPatternType patternType) => patternType switch
    {
        BehavioralPatternType.HighSwitchingBurst => 10,
        BehavioralPatternType.ExtendedSingleAppSession => 45,
        BehavioralPatternType.LateNightUsageSpike => 45,
        BehavioralPatternType.RapidReopenPattern => 4,
        _ => 10
    };
}
