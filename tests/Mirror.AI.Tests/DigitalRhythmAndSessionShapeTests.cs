using Mirror.Analytics;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Xunit;

namespace Mirror.AI.Tests;

public class DigitalRhythmAndSessionShapeTests
{
    [Fact]
    public void DigitalRhythm_IdentifiesMorningPeak_Correctly()
    {
        var service = new DigitalRhythmService();
        var today = DateTime.Today;

        // Morning session at 9:00 AM (2 hours)
        var morningSession = new ActivitySession
        {
            Id = 1,
            AppKey = "code_exe",
            DisplayName = "VS Code",
            Category = "Development",
            StartUtc = today.AddHours(9).ToUniversalTime(),
            EndUtc = today.AddHours(11).ToUniversalTime(),
            ActiveSeconds = 7200
        };

        // Evening session at 20:00 (30 mins)
        var eveningSession = new ActivitySession
        {
            Id = 2,
            AppKey = "browser_exe",
            DisplayName = "Browser",
            Category = "Communication",
            StartUtc = today.AddHours(20).ToUniversalTime(),
            EndUtc = today.AddHours(20.5).ToUniversalTime(),
            ActiveSeconds = 1800
        };

        var report = service.AnalyzeRhythm(today, new[] { morningSession, eveningSession }, Array.Empty<AppSwitchEvent>());

        Assert.Equal(TimeOfDayPeriod.Morning, report.PeakPeriod);
        Assert.Equal(2.5, report.TotalActiveHours);
        Assert.Contains("Morning", report.RhythmAnalysis);
    }

    [Fact]
    public void SessionClassifier_Identifies_FocusedAndSwitchHeavyBlocks()
    {
        var classifier = new SessionStructureClassifier();
        var now = DateTime.UtcNow;

        // 1. Focused Block (60 mins, 2 switches, 1 app)
        var focusedSession = new ActivitySession
        {
            Id = 10,
            AppKey = "word_exe",
            DisplayName = "Word",
            Category = "Productivity",
            StartUtc = now.AddMinutes(-60),
            EndUtc = now,
            ActiveSeconds = 3400
        };
        var fewSwitches = new List<AppSwitchEvent>
        {
            new AppSwitchEvent { FromAppKey = "word_exe", ToAppKey = "word_exe", TimestampUtc = now.AddMinutes(-30) }
        };

        var focusedResult = classifier.ClassifyBlock(now.AddMinutes(-60), now, new[] { focusedSession }, fewSwitches, Array.Empty<IdlePeriod>());
        Assert.True(focusedResult.StructureType == SessionStructureType.Focused || focusedResult.StructureType == SessionStructureType.LongForm);

        // 2. Switch Heavy Block (10 mins, 40 switches across diverse apps)
        var manySwitches = new List<AppSwitchEvent>();
        for (int i = 0; i < 40; i++)
        {
            manySwitches.Add(new AppSwitchEvent
            {
                FromAppKey = $"app_{i}",
                ToAppKey = $"app_{i + 1}",
                TimestampUtc = now.AddMinutes(-10).AddSeconds(i * 15)
            });
        }
        var switchHeavyResult = classifier.ClassifyBlock(now.AddMinutes(-10), now, new[] { focusedSession }, manySwitches, Array.Empty<IdlePeriod>());
        Assert.Equal(SessionStructureType.SwitchHeavy, switchHeavyResult.StructureType);
    }

    [Fact]
    public void AnomalyDetection_FlagsStatisticalOutliers_WithZScore()
    {
        var engine = new AnomalyDetectionEngine();
        var now = DateTime.UtcNow;

        // Baseline: Median = 5h (18000s), StdDev = 1h (3600s)
        var baselines = new List<AdaptiveBaseline>
        {
            new AdaptiveBaseline
            {
                MetricKey = "daily_active_seconds",
                MedianValue = 18000,
                StdDev = 3600,
                SampleCount = 14,
                WindowDays = 14,
                UpdatedUtc = now
            }
        };

        // Today: 9h (32400s) -> Z-Score = (32400 - 18000) / 3600 = +4.0σ
        var todayMetrics = new DailyMetrics
        {
            DateLocal = DateTime.Today.ToString("yyyy-MM-dd"),
            ActiveSeconds = 32400,
            SwitchCount = 50
        };

        var anomalies = engine.DetectAnomalies(todayMetrics, baselines, now);

        Assert.Single(anomalies);
        var anomaly = anomalies[0];
        Assert.Equal("daily_active_seconds", anomaly.MetricKey);
        Assert.Equal(4.0, anomaly.ZScore);
        Assert.Equal("High", anomaly.Severity);
        Assert.Contains("+4.00σ", anomaly.MathematicalExplanation);
    }

    [Fact]
    public void PatternRelationshipAnalyzer_IdentifiesTemporalCoOccurrence()
    {
        var analyzer = new PatternRelationshipAnalyzer();
        var t0 = DateTime.UtcNow;

        var patterns = new List<PatternEvent>
        {
            new PatternEvent
            {
                PatternType = BehavioralPatternType.HighSwitchingBurst,
                StartUtc = t0,
                EndUtc = t0.AddMinutes(5),
                DetectedUtc = t0.AddMinutes(5),
                Explanation = "High frequency switching burst detected"
            },
            new PatternEvent
            {
                PatternType = BehavioralPatternType.RapidReopenPattern,
                StartUtc = t0.AddMinutes(8),
                EndUtc = t0.AddMinutes(12),
                DetectedUtc = t0.AddMinutes(12),
                Explanation = "Rapid reopen sequence detected"
            }
        };

        var relationships = analyzer.AnalyzeRelationships(patterns, TimeSpan.FromMinutes(15));

        Assert.Single(relationships);
        var rel = relationships[0];
        Assert.Equal(1, rel.CoOccurrenceCount);
        Assert.Equal(3.0, rel.AverageIntervalMinutes);
        Assert.Contains("HighSwitchingBurst", rel.Summary);
        Assert.Contains("RapidReopenPattern", rel.Summary);
    }
}
