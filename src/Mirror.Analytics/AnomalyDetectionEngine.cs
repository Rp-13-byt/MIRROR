using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public record AnomalyEvent
{
    public string MetricKey { get; init; } = string.Empty;
    public double ObservedValue { get; init; }
    public double BaselineMedian { get; init; }
    public double BaselineStdDev { get; init; }
    public double ZScore { get; init; }
    public string Severity { get; init; } = "Moderate"; // Moderate, High
    public string MathematicalExplanation { get; init; } = string.Empty;
    public DateTime TimestampUtc { get; init; }
}

public class AnomalyDetectionEngine
{
    public IReadOnlyList<AnomalyEvent> DetectAnomalies(
        DailyMetrics todayMetrics,
        IReadOnlyList<AdaptiveBaseline> baselines,
        DateTime timestampUtc)
    {
        var anomalies = new List<AnomalyEvent>();

        // Check active hours
        var activeBaseline = baselines.FirstOrDefault(b => b.MetricKey == "daily_active_seconds");
        if (activeBaseline != null && !activeBaseline.IsLearningPhase && activeBaseline.StdDev > 0)
        {
            double observed = todayMetrics.ActiveSeconds;
            double z = (observed - activeBaseline.MedianValue) / activeBaseline.StdDev;
            if (Math.Abs(z) >= 2.0)
            {
                anomalies.Add(new AnomalyEvent
                {
                    MetricKey = "daily_active_seconds",
                    ObservedValue = Math.Round(observed / 3600.0, 1),
                    BaselineMedian = Math.Round(activeBaseline.MedianValue / 3600.0, 1),
                    BaselineStdDev = Math.Round(activeBaseline.StdDev / 3600.0, 1),
                    ZScore = Math.Round(z, 2),
                    Severity = Math.Abs(z) >= 3.0 ? "High" : "Moderate",
                    MathematicalExplanation = $"Total active screen time of {observed / 3600.0:F1}h deviates by {z:+0.00;-0.00}σ from your 14-day median ({activeBaseline.MedianValue / 3600.0:F1}h, σ={activeBaseline.StdDev / 3600.0:F1}h).",
                    TimestampUtc = timestampUtc
                });
            }
        }

        // Check switch count
        var switchBaseline = baselines.FirstOrDefault(b => b.MetricKey == "daily_switch_count");
        if (switchBaseline != null && !switchBaseline.IsLearningPhase && switchBaseline.StdDev > 0)
        {
            double observed = todayMetrics.SwitchCount;
            double z = (observed - switchBaseline.MedianValue) / switchBaseline.StdDev;
            if (Math.Abs(z) >= 2.0)
            {
                anomalies.Add(new AnomalyEvent
                {
                    MetricKey = "daily_switch_count",
                    ObservedValue = observed,
                    BaselineMedian = Math.Round(switchBaseline.MedianValue, 0),
                    BaselineStdDev = Math.Round(switchBaseline.StdDev, 1),
                    ZScore = Math.Round(z, 2),
                    Severity = Math.Abs(z) >= 3.0 ? "High" : "Moderate",
                    MathematicalExplanation = $"Application switch count ({observed}) deviates by {z:+0.00;-0.00}σ from your 14-day median ({switchBaseline.MedianValue:F0} switches, σ={switchBaseline.StdDev:F1}).",
                    TimestampUtc = timestampUtc
                });
            }
        }

        return anomalies;
    }
}
