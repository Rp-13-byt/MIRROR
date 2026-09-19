using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class AdaptiveBaselineService : IAdaptiveBaselineService
{
    private readonly IMirrorRepository _repository;

    public AdaptiveBaselineService(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdaptiveBaseline> ComputeAdaptiveBaselineAsync(string metricKey, int windowDays = 14, CancellationToken ct = default)
    {
        DateTime endUtc = DateTime.UtcNow;
        DateTime startUtc = endUtc.AddDays(-windowDays);

        string startDate = startUtc.ToLocalTime().ToString("yyyy-MM-dd");
        string endDate = endUtc.ToLocalTime().ToString("yyyy-MM-dd");

        var dailyMetrics = await _repository.GetDailyMetricsRangeAsync(startDate, endDate, ct);

        int sampleCount = dailyMetrics.Count;
        if (sampleCount == 0)
        {
            var emptyBaseline = new AdaptiveBaseline
            {
                MetricKey = metricKey,
                WindowDays = windowDays,
                MedianValue = 0,
                StdDev = 0,
                SampleCount = 0,
                UpdatedUtc = DateTime.UtcNow
            };
            await _repository.UpsertAdaptiveBaselineAsync(emptyBaseline, ct);
            return emptyBaseline;
        }

        List<double> values = metricKey.ToLowerInvariant() switch
        {
            "daily_active_seconds" => dailyMetrics.Select(m => (double)m.ActiveSeconds).ToList(),
            "daily_switches" => dailyMetrics.Select(m => (double)m.SwitchCount).ToList(),
            "daily_late_night_seconds" => dailyMetrics.Select(m => (double)m.LateNightSeconds).ToList(),
            "daily_sessions" => dailyMetrics.Select(m => (double)m.SessionCount).ToList(),
            _ => dailyMetrics.Select(m => (double)m.ActiveSeconds).ToList()
        };

        values.Sort();

        double median = ComputeMedian(values);
        double stdDev = ComputeStandardDeviation(values);

        var baseline = new AdaptiveBaseline
        {
            MetricKey = metricKey,
            WindowDays = windowDays,
            MedianValue = median,
            StdDev = stdDev,
            SampleCount = sampleCount,
            UpdatedUtc = DateTime.UtcNow
        };

        await _repository.UpsertAdaptiveBaselineAsync(baseline, ct);
        return baseline;
    }

    public async Task<IReadOnlyList<AdaptiveBaseline>> GetAllBaselinesAsync(int windowDays = 14, CancellationToken ct = default)
    {
        var existing = await _repository.GetAllAdaptiveBaselinesAsync(windowDays, ct);
        if (existing.Count >= 3)
        {
            return existing;
        }

        // Compute default set if missing
        var keys = new[] { "daily_active_seconds", "daily_switches", "daily_late_night_seconds", "daily_sessions" };
        var list = new List<AdaptiveBaseline>();
        foreach (var key in keys)
        {
            list.Add(await ComputeAdaptiveBaselineAsync(key, windowDays, ct));
        }
        return list;
    }

    public bool IsExceedingBaseline(double currentValue, AdaptiveBaseline baseline, double sigmaMultiplier = 2.0)
    {
        // Strict Gate: Before Day 7, no baseline-driven alerts or deviations are flagged
        if (baseline.IsLearningPhase)
        {
            return false;
        }

        double threshold = baseline.MedianValue + (sigmaMultiplier * baseline.StdDev);
        return currentValue > threshold;
    }

    private static double ComputeMedian(List<double> sorted)
    {
        if (sorted.Count == 0) return 0;
        int mid = sorted.Count / 2;
        if (sorted.Count % 2 != 0)
        {
            return sorted[mid];
        }
        return (sorted[mid - 1] + sorted[mid]) / 2.0;
    }

    private static double ComputeStandardDeviation(List<double> values)
    {
        if (values.Count < 2) return 0;
        double avg = values.Average();
        double sumSquares = values.Sum(d => Math.Pow(d - avg, 2));
        return Math.Sqrt(sumSquares / (values.Count - 1));
    }
}
