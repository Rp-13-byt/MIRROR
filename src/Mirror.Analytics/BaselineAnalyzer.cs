using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public class BaselineAnalyzer : IBaselineAnalyzer
{
    private readonly IMirrorRepository _repository;

    public BaselineAnalyzer(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<UserBaseline> ComputeBaselineAsync(int lookbackDays = 14, CancellationToken ct = default)
    {
        DateTime endUtc = DateTime.UtcNow;
        DateTime startUtc = endUtc.AddDays(-lookbackDays);

        string startDateLocal = startUtc.ToLocalTime().ToString("yyyy-MM-dd");
        string endDateLocal = endUtc.ToLocalTime().ToString("yyyy-MM-dd");

        var dailyMetrics = await _repository.GetDailyMetricsRangeAsync(startDateLocal, endDateLocal, ct);

        if (dailyMetrics.Count < 3)
        {
            // Not enough local history
            return new UserBaseline
            {
                SampleDaysCount = dailyMetrics.Count,
                MedianDailyActiveSeconds = 0,
                MedianSwitchRatePerMinute = 0,
                MedianSessionLengthSeconds = 0,
                MedianLateNightActiveSeconds = 0,
                MedianAppEntropy = 0
            };
        }

        var activeSecs = dailyMetrics.Select(m => (double)m.ActiveSeconds).OrderBy(x => x).ToList();
        var lateNightSecs = dailyMetrics.Select(m => (double)m.LateNightSeconds).OrderBy(x => x).ToList();
        var switchCounts = dailyMetrics.Select(m => (double)m.SwitchCount).OrderBy(x => x).ToList();
        var sessionCounts = dailyMetrics.Select(m => (double)m.SessionCount).OrderBy(x => x).ToList();

        double medianActive = GetMedian(activeSecs);
        double medianLateNight = GetMedian(lateNightSecs);
        double medianSwitches = GetMedian(switchCounts);
        double medianSessions = GetMedian(sessionCounts);

        double medianSwitchRate = medianActive > 0 ? (medianSwitches / (medianActive / 60.0)) : 0.0;
        double medianSessionLen = medianSessions > 0 ? (medianActive / medianSessions) : 0.0;

        return new UserBaseline
        {
            SampleDaysCount = dailyMetrics.Count,
            MedianDailyActiveSeconds = medianActive,
            MedianSwitchRatePerMinute = medianSwitchRate,
            MedianSessionLengthSeconds = medianSessionLen,
            MedianLateNightActiveSeconds = medianLateNight,
            MedianAppEntropy = 0.5
        };
    }

    private static double GetMedian(List<double> sorted)
    {
        if (sorted.Count == 0) return 0;
        int mid = sorted.Count / 2;
        if (sorted.Count % 2 != 0)
        {
            return sorted[mid];
        }
        return (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
}
