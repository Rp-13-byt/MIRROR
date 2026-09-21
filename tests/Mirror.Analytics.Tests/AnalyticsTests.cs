using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mirror.Analytics;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Xunit;

namespace Mirror.Analytics.Tests;

public class FeatureExtractorTests
{
    private readonly FeatureExtractor _extractor = new();

    [Fact]
    public void ExtractFeatureSequence_ProducesCorrectShapeAndBoundedValues()
    {
        var now = DateTime.UtcNow;
        var sessions = new List<ActivitySession>
        {
            new()
            {
                AppKey = "browser",
                DisplayName = "Chrome",
                Category = "Browsing",
                StartUtc = now.AddMinutes(-45),
                EndUtc = now.AddMinutes(-15),
                ActiveSeconds = 1800,
                CloseReason = SessionCloseReason.AppSwitch
            },
            new()
            {
                AppKey = "editor",
                DisplayName = "VS Code",
                Category = "Development",
                StartUtc = now.AddMinutes(-15),
                EndUtc = now,
                ActiveSeconds = 900,
                CloseReason = SessionCloseReason.Shutdown
            }
        };

        var switches = new List<AppSwitchEvent>
        {
            new() { FromAppKey = "browser", ToAppKey = "editor", TimestampUtc = now.AddMinutes(-15) }
        };

        var sequence = _extractor.ExtractFeatureSequence(sessions, switches, now, 60);

        Assert.Equal(60, sequence.Timesteps);
        Assert.Equal(10, sequence.FeatureCount);

        for (int t = 0; t < 60; t++)
        {
            for (int f = 0; f < 10; f++)
            {
                float val = sequence.Values[t, f];
                Assert.True(val >= 0.0f && val <= 1.0f, $"Feature [{t}, {f}] value {val} out of range [0, 1]");
            }
        }
    }

    [Fact]
    public void ExtractMinuteFeatures_EmptyMinute_ReturnsValidZeros()
    {
        var now = new DateTime(2026, 9, 18, 14, 0, 0, DateTimeKind.Utc); // 2 PM
        var features = _extractor.ExtractMinuteFeatures(new List<ActivitySession>(), new List<AppSwitchEvent>(), now);

        Assert.Equal(10, features.Length);
        Assert.Equal(0.0f, features[0]); // Active seconds
        Assert.Equal(0.0f, features[1]); // Switch count
        Assert.Equal(0.0f, features[2]); // Unique apps
        Assert.Equal(0.0f, features[3]); // Reopens
        Assert.Equal(0.0f, features[7]); // Day time (not late night)
    }
}

public class BaselineAnalyzerTests
{
    public class MockRepo : IMirrorRepository
    {
        private readonly List<DailyMetrics> _metrics;
        public MockRepo(List<DailyMetrics> metrics) => _metrics = metrics;

        public Task<IReadOnlyList<DailyMetrics>> GetDailyMetricsRangeAsync(string startDate, string endDate, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DailyMetrics>>(_metrics);

        public Task InsertSessionAsync(ActivitySession s, CancellationToken ct = default) => Task.CompletedTask;
        public Task InsertSessionsBatchAsync(IEnumerable<ActivitySession> s, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<ActivitySession>> GetSessionsAsync(DateTime s, DateTime e, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ActivitySession>>(new List<ActivitySession>());
        public Task<IReadOnlyList<ActivitySession>> GetRecentSessionsAsync(TimeSpan lookback, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ActivitySession>>(new List<ActivitySession>());
        public Task InsertIdlePeriodAsync(IdlePeriod i, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<IdlePeriod>> GetIdlePeriodsAsync(DateTime s, DateTime e, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<IdlePeriod>>(new List<IdlePeriod>());
        public Task InsertAppSwitchAsync(AppSwitchEvent a, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<AppSwitchEvent>> GetRecentSwitchesAsync(TimeSpan lookback, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AppSwitchEvent>>(new List<AppSwitchEvent>());
        public Task InsertPatternEventAsync(PatternEvent p, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<PatternEvent>> GetPatternEventsAsync(DateTime s, DateTime e, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<PatternEvent>>(new List<PatternEvent>());
        public Task UpsertDailyMetricsAsync(DailyMetrics m, CancellationToken ct = default) => Task.CompletedTask;
        public Task<UserSettings> LoadSettingsAsync(CancellationToken ct = default) => Task.FromResult(new UserSettings());
        public Task SaveSettingsAsync(UserSettings s, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyDictionary<string, string>> GetCategoryOverridesAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
        public Task SaveCategoryOverrideAsync(string appKey, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task<long> PruneOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default) => Task.FromResult(0L);
        public Task PurgeAllDataAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct = default) => Task.FromResult(0L);

        public List<FlowStateSession> RecordedFlows { get; } = new();
        public Task InsertFlowStateSessionAsync(FlowStateSession session, CancellationToken ct = default)
        {
            RecordedFlows.Add(session);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<FlowStateSession>> GetFlowStateSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FlowStateSession>>(RecordedFlows.Where(f => f.StartUtc >= startUtc && f.EndUtc <= endUtc).ToList());

        public Dictionary<string, AdaptiveBaseline> Baselines { get; } = new();
        public Task UpsertAdaptiveBaselineAsync(AdaptiveBaseline baseline, CancellationToken ct = default)
        {
            Baselines[$"{baseline.MetricKey}_{baseline.WindowDays}"] = baseline;
            return Task.CompletedTask;
        }
        public Task<AdaptiveBaseline?> GetAdaptiveBaselineAsync(string metricKey, int windowDays, CancellationToken ct = default)
            => Task.FromResult(Baselines.TryGetValue($"{metricKey}_{windowDays}", out var b) ? b : null);
        public Task<IReadOnlyList<AdaptiveBaseline>> GetAllAdaptiveBaselinesAsync(int windowDays, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<AdaptiveBaseline>>(Baselines.Values.Where(b => b.WindowDays == windowDays).ToList());

        public List<PatternFeedback> FeedbackList { get; } = new();
        public Task InsertPatternFeedbackAsync(PatternFeedback feedback, CancellationToken ct = default)
        {
            FeedbackList.Add(feedback);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<PatternFeedback>> GetPatternFeedbackAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PatternFeedback>>(FeedbackList);

        public Dictionary<string, double> RecalibratedThresholds { get; } = new();
        public Task UpsertRecalibratedThresholdAsync(string thresholdKey, double value, CancellationToken ct = default)
        {
            RecalibratedThresholds[thresholdKey] = value;
            return Task.CompletedTask;
        }
        public Task<IReadOnlyDictionary<string, double>> GetRecalibratedThresholdsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyDictionary<string, double>>(RecalibratedThresholds);
        public Task ResetRecalibratedThresholdsAsync(CancellationToken ct = default)
        {
            RecalibratedThresholds.Clear();
            return Task.CompletedTask;
        }

        public List<FocusSession> FocusSessions { get; } = new();
        public Task InsertFocusSessionAsync(FocusSession session, CancellationToken ct = default)
        {
            FocusSessions.Add(session);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<FocusSession>> GetFocusSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<FocusSession>>(FocusSessions.Where(f => f.StartUtc >= startUtc && f.EndUtc <= endUtc).ToList());

        public List<UserContext> UserContexts { get; } = new();
        public Task<IReadOnlyList<UserContext>> GetUserContextsAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<UserContext>>(UserContexts);
        public Task<long> InsertUserContextAsync(string name, CancellationToken ct = default)
        {
            var ctx = new UserContext { Id = UserContexts.Count + 1, Name = name, CreatedUtc = DateTime.UtcNow };
            UserContexts.Add(ctx);
            return Task.FromResult(ctx.Id);
        }
        public Task DeleteUserContextAsync(long contextId, CancellationToken ct = default)
        {
            UserContexts.RemoveAll(c => c.Id == contextId);
            return Task.CompletedTask;
        }

        public Dictionary<string, long> AppContextMappings { get; } = new();
        public Task AssignAppContextAsync(string appKey, long contextId, CancellationToken ct = default)
        {
            AppContextMappings[appKey] = contextId;
            return Task.CompletedTask;
        }
        public Task<long?> GetAppContextAsync(string appKey, CancellationToken ct = default)
            => Task.FromResult(AppContextMappings.TryGetValue(appKey, out var id) ? (long?)id : null);

        public Dictionary<BehavioralPatternType, PatternVisibility> PatternPreferences { get; } = new();
        public Task<IReadOnlyList<PatternPreference>> GetPatternPreferencesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<PatternPreference>>(PatternPreferences.Select(p => new PatternPreference { PatternType = p.Key, Visibility = p.Value, UpdatedUtc = DateTime.UtcNow }).ToList());
        public Task SavePatternPreferenceAsync(BehavioralPatternType patternType, PatternVisibility visibility, CancellationToken ct = default)
        {
            PatternPreferences[patternType] = visibility;
            return Task.CompletedTask;
        }

        public Task<DataInventoryCounts> GetDataInventoryCountsAsync(CancellationToken ct = default)
            => Task.FromResult(new DataInventoryCounts(0, 0, 0, 0, 0, 0, 0, 1, 0, 0));
        public Task<bool> CheckDatabaseIntegrityAsync(CancellationToken ct = default)
            => Task.FromResult(true);

        public Task RecordHeartbeatAsync(bool isCleanShutdown, CancellationToken ct = default)
            => Task.CompletedTask;
        public Task<bool> WasLastShutdownCleanAsync(CancellationToken ct = default)
            => Task.FromResult(true);
    }

    [Fact]
    public async Task ComputeBaselineAsync_InsufficientDays_ReturnsEmptyBaseline()
    {
        var repo = new MockRepo(new List<DailyMetrics>
        {
            new() { DateLocal = "2026-09-17", ActiveSeconds = 3600, IdleSeconds = 600, SwitchCount = 20, SessionCount = 10, UniqueAppCount = 3, LateNightSeconds = 0, LongestSessionSeconds = 1200 }
        });

        var analyzer = new BaselineAnalyzer(repo);
        var baseline = await analyzer.ComputeBaselineAsync(14);

        Assert.Equal(1, baseline.SampleDaysCount);
        Assert.Equal(0, baseline.MedianDailyActiveSeconds);
    }

    [Fact]
    public async Task ComputeBaselineAsync_SufficientDays_ComputesMediansCorrectly()
    {
        var repo = new MockRepo(new List<DailyMetrics>
        {
            new() { DateLocal = "2026-09-15", ActiveSeconds = 3000, IdleSeconds = 500, SwitchCount = 30, SessionCount = 10, UniqueAppCount = 4, LateNightSeconds = 0, LongestSessionSeconds = 900 },
            new() { DateLocal = "2026-09-16", ActiveSeconds = 6000, IdleSeconds = 600, SwitchCount = 60, SessionCount = 20, UniqueAppCount = 5, LateNightSeconds = 100, LongestSessionSeconds = 1800 },
            new() { DateLocal = "2026-09-17", ActiveSeconds = 9000, IdleSeconds = 700, SwitchCount = 90, SessionCount = 30, UniqueAppCount = 6, LateNightSeconds = 200, LongestSessionSeconds = 2400 }
        });

        var analyzer = new BaselineAnalyzer(repo);
        var baseline = await analyzer.ComputeBaselineAsync(14);

        Assert.Equal(3, baseline.SampleDaysCount);
        Assert.Equal(6000, baseline.MedianDailyActiveSeconds);
        Assert.Equal(100, baseline.MedianLateNightActiveSeconds);
    }
}

public class PatternDetectorTests
{
    private readonly PatternDetector _detector = new();
    private readonly UserSettings _settings = new();

    [Fact]
    public void HighSwitchingBurst_DetectsWhenThresholdExceeded()
    {
        var now = DateTime.UtcNow;
        var switches = new List<AppSwitchEvent>();
        string[] apps = ["vscode", "chrome", "teams", "slack"];

        for (int i = 0; i < 16; i++)
        {
            switches.Add(new AppSwitchEvent
            {
                FromAppKey = apps[i % 4],
                ToAppKey = apps[(i + 1) % 4],
                TimestampUtc = now.AddMinutes(-4).AddSeconds(i * 15)
            });
        }

        var patterns = _detector.EvaluateRules(new List<ActivitySession>(), switches, _settings, null, now);

        Assert.Contains(patterns, p => p.PatternType == BehavioralPatternType.HighSwitchingBurst);
    }

    [Fact]
    public void ExtendedSingleAppSession_DetectsWhenSessionExceedsThreshold()
    {
        var now = DateTime.UtcNow;
        var sessions = new List<ActivitySession>
        {
            new()
            {
                AppKey = "game.exe",
                DisplayName = "Simulation Game",
                Category = "Gaming",
                StartUtc = now.AddMinutes(-100),
                EndUtc = now,
                ActiveSeconds = 6000,
                CloseReason = SessionCloseReason.ProcessExit
            }
        };

        var patterns = _detector.EvaluateRules(sessions, new List<AppSwitchEvent>(), _settings, null, now);

        Assert.Contains(patterns, p => p.PatternType == BehavioralPatternType.ExtendedSingleAppSession);
    }
}

public class PatternFusionEngineTests
{
    private readonly PatternFusionEngine _engine = new();

    [Fact]
    public void FuseSignals_NoRuleCandidate_ReturnsNullEvenIfMlDetects()
    {
        var mlInference = new InferenceResult
        {
            DetectedPattern = BehavioralPatternType.HighSwitchingBurst,
            Confidence = 0.95f,
            ClassProbabilities = new float[] { 0.01f, 0.95f, 0.02f, 0.01f, 0.01f },
            BackendUsed = InferenceBackendKind.Cpu,
            InferenceTimeMs = 0.5,
            ModelVersion = "v1"
        };

        var result = _engine.FuseSignals(null, mlInference);

        Assert.Null(result);
    }

    [Fact]
    public void FuseSignals_RuleCandidateWithCorroboratingMl_ConfirmsAndBoostsConfidence()
    {
        var rule = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = DateTime.UtcNow.AddMinutes(-10),
            EndUtc = DateTime.UtcNow,
            RuleSignal = true,
            ModelSignal = false,
            ModelConfidence = 0.70f,
            Explanation = "High switching detected."
        };

        var mlInference = new InferenceResult
        {
            DetectedPattern = BehavioralPatternType.HighSwitchingBurst,
            Confidence = 0.90f,
            ClassProbabilities = new float[] { 0.02f, 0.90f, 0.03f, 0.03f, 0.02f },
            BackendUsed = InferenceBackendKind.DirectMl,
            InferenceTimeMs = 0.8,
            ModelVersion = "v1"
        };

        var result = _engine.FuseSignals(rule, mlInference);

        Assert.NotNull(result);
        Assert.True(result.ModelSignal);
        Assert.True(result.ModelConfidence > 0.70f);
    }
}

public class PatternExplanationBuilderTests
{
    private readonly PatternExplanationBuilder _builder = new();

    [Theory]
    [InlineData(BehavioralPatternType.HighSwitchingBurst)]
    [InlineData(BehavioralPatternType.ExtendedSingleAppSession)]
    [InlineData(BehavioralPatternType.LateNightUsageSpike)]
    [InlineData(BehavioralPatternType.RapidReopenPattern)]
    [InlineData(BehavioralPatternType.CompositeScrollLike)]
    public void Explanations_ContainOnlyObjectiveNonClinicalLanguage(BehavioralPatternType patternType)
    {
        string text = _builder.BuildExplanation(patternType, 15, 4, TimeSpan.FromMinutes(45), 2.5, 0.4);

        Assert.False(string.IsNullOrWhiteSpace(text));

        string[] forbiddenTerms =
        {
            "addiction", "addicted", "disorder", "adhd", "burnout",
            "depression", "anxiety", "illness", "pathology", "treatment",
            "therapy", "obsessive", "compulsive", "unhealthy"
        };

        foreach (var term in forbiddenTerms)
        {
            Assert.DoesNotContain(term, text, StringComparison.OrdinalIgnoreCase);
        }
    }
}

public class FlowStateDetectorTests
{
    [Fact]
    public async Task EvaluateFlowStateAsync_UninterruptedLongSession_DetectsFlowState()
    {
        var repo = new BaselineAnalyzerTests.MockRepo(new List<DailyMetrics>());
        var detector = new FlowStateDetector(repo);

        var now = DateTime.UtcNow;
        var sessions = new List<ActivitySession>
        {
            new()
            {
                AppKey = "vscode",
                DisplayName = "Visual Studio Code",
                Category = "Development",
                StartUtc = now.AddMinutes(-75),
                EndUtc = now,
                ActiveSeconds = 4500, // 75 mins
                CloseReason = SessionCloseReason.AppSwitch
            }
        };

        var switches = new List<AppSwitchEvent>(); // 0 distractions

        var flow = await detector.EvaluateFlowStateAsync(sessions, switches, now);

        Assert.NotNull(flow);
        Assert.Equal("vscode", flow.AppKey);
        Assert.Equal(4500, flow.DurationSeconds);
        Assert.Equal(0, flow.DistractionCount);
        Assert.Single(repo.RecordedFlows);
    }

    [Fact]
    public async Task EvaluateFlowStateAsync_InterruptedSession_ReturnsNull()
    {
        var repo = new BaselineAnalyzerTests.MockRepo(new List<DailyMetrics>());
        var detector = new FlowStateDetector(repo);

        var now = DateTime.UtcNow;
        var sessions = new List<ActivitySession>
        {
            new()
            {
                AppKey = "vscode",
                DisplayName = "Visual Studio Code",
                Category = "Development",
                StartUtc = now.AddMinutes(-70),
                EndUtc = now,
                ActiveSeconds = 4200,
                CloseReason = SessionCloseReason.AppSwitch
            }
        };

        var switches = new List<AppSwitchEvent>
        {
            new() { FromAppKey = "vscode", ToAppKey = "slack", TimestampUtc = now.AddMinutes(-30) }
        };

        var flow = await detector.EvaluateFlowStateAsync(sessions, switches, now);

        Assert.Null(flow);
        Assert.Empty(repo.RecordedFlows);
    }
}

public class AdaptiveBaselineTests
{
    [Fact]
    public async Task AdaptiveBaseline_UnderDay7Gate_EnforcesLearningStatusAndSuppressesAlerts()
    {
        var repo = new BaselineAnalyzerTests.MockRepo(new List<DailyMetrics>
        {
            new() { DateLocal = "2026-09-15", ActiveSeconds = 7200 },
            new() { DateLocal = "2026-09-16", ActiveSeconds = 7500 },
            new() { DateLocal = "2026-09-17", ActiveSeconds = 7100 },
            new() { DateLocal = "2026-09-18", ActiveSeconds = 7300 }
        });

        var service = new AdaptiveBaselineService(repo);
        var baseline = await service.ComputeAdaptiveBaselineAsync("daily_active_seconds", 14);

        Assert.Equal(4, baseline.SampleCount);
        Assert.True(baseline.IsLearningPhase);
        Assert.Contains("4 of 7 days recorded", baseline.LearningStatusMessage);

        // Even with huge deviation, alerts must be suppressed during learning phase
        bool alert = service.IsExceedingBaseline(99999, baseline, 2.0);
        Assert.False(alert);
    }

    [Fact]
    public async Task AdaptiveBaseline_AfterDay7Gate_ComputesSigmaAndDetectsOutliers()
    {
        var repo = new BaselineAnalyzerTests.MockRepo(new List<DailyMetrics>
        {
            new() { DateLocal = "2026-09-11", ActiveSeconds = 5000 },
            new() { DateLocal = "2026-09-12", ActiveSeconds = 5050 },
            new() { DateLocal = "2026-09-13", ActiveSeconds = 4950 },
            new() { DateLocal = "2026-09-14", ActiveSeconds = 5100 },
            new() { DateLocal = "2026-09-15", ActiveSeconds = 5000 },
            new() { DateLocal = "2026-09-16", ActiveSeconds = 5020 },
            new() { DateLocal = "2026-09-17", ActiveSeconds = 4980 }
        });

        var service = new AdaptiveBaselineService(repo);
        var baseline = await service.ComputeAdaptiveBaselineAsync("daily_active_seconds", 14);

        Assert.Equal(7, baseline.SampleCount);
        Assert.False(baseline.IsLearningPhase);
        Assert.Contains("personal baseline calculated from the last 14 days", baseline.LearningStatusMessage);
        Assert.InRange(baseline.MedianValue, 4990, 5010);

        // Within 2 sigma
        Assert.False(service.IsExceedingBaseline(5050, baseline, 2.0));
        // Greater than 2 sigma
        Assert.True(service.IsExceedingBaseline(9000, baseline, 2.0));
    }
}

public class ThresholdRecalibratorTests
{
    [Fact]
    public async Task RecalibrateThresholdAsync_BumpsThresholdBy15PercentAndLogsFeedback()
    {
        var repo = new BaselineAnalyzerTests.MockRepo(new List<DailyMetrics>());
        var recalibrator = new ThresholdRecalibrator(repo);

        double newThreshold = await recalibrator.RecalibrateThresholdAsync(
            BehavioralPatternType.HighSwitchingBurst,
            "I was working across multiple docs",
            101);

        // High switch default = 10 -> ceil(10 * 1.15) = 12
        Assert.Equal(12.0, newThreshold);
        Assert.Single(repo.FeedbackList);
        Assert.Equal("I was working across multiple docs", repo.FeedbackList[0].FeedbackReason);

        var active = await recalibrator.GetActiveThresholdsAsync();
        Assert.Equal(12.0, active["threshold_high_switch"]);
    }
}

