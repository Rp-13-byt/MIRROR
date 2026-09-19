using System;
using System.Collections.Generic;
using System.Linq;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror_App.Services;

public sealed class DemoDataService : IDemoDataService
{
    private readonly List<DailyMetrics> _cachedMetrics = new();
    private readonly List<ActivitySession> _cachedSessions = new();
    private readonly List<PatternEvent> _cachedPatterns = new();
    private readonly UserBaseline _cachedBaseline;

    public bool IsDemoModeActive { get; set; } = true;

    public DemoDataService()
    {
        DateTime today = DateTime.Today;

        // 7 days of daily metrics
        for (int i = 6; i >= 0; i--)
        {
            DateTime day = today.AddDays(-i);
            int activeSecs = (320 + (i * 25) % 180) * 60;
            int idleSecs = (45 + (i * 12) % 60) * 60;
            int switches = 85 + (i * 19) % 90;

            _cachedMetrics.Add(new DailyMetrics
            {
                DateLocal = day.ToString("yyyy-MM-dd"),
                ActiveSeconds = activeSecs,
                IdleSeconds = idleSecs,
                SwitchCount = switches,
                SessionCount = 24 + i * 2,
                UniqueAppCount = 7,
                LateNightSeconds = (i % 3 == 0) ? 3600 : 0,
                LongestSessionSeconds = 7200,
                TopAppKey = "code.exe"
            });
        }

        // Today sessions
        DateTime sessionStart = today.AddHours(9);
        string[] apps = { "code.exe", "slack.exe", "windowsterminal.exe", "msedge.exe", "teams.exe", "code.exe" };
        string[] names = { "Visual Studio Code", "Slack", "Windows Terminal", "Microsoft Edge", "Microsoft Teams", "Visual Studio Code" };
        string[] cats = { "Development", "Communication", "Development", "Browsing", "Communication", "Development" };
        int[] durations = { 85, 25, 45, 30, 20, 120 };

        for (int j = 0; j < apps.Length; j++)
        {
            DateTime end = sessionStart.AddMinutes(durations[j]);
            _cachedSessions.Add(new ActivitySession
            {
                Id = j + 1,
                AppKey = apps[j],
                DisplayName = names[j],
                Category = cats[j],
                StartUtc = sessionStart.ToUniversalTime(),
                EndUtc = end.ToUniversalTime(),
                ActiveSeconds = durations[j] * 60,
                CloseReason = SessionCloseReason.AppSwitch
            });
            sessionStart = end.AddMinutes(5);
        }

        // Detected behavioral patterns (descriptive, non-clinical)
        _cachedPatterns.Add(new PatternEvent
        {
            Id = 1,
            PatternType = BehavioralPatternType.ExtendedSingleAppSession,
            StartUtc = today.AddHours(9).ToUniversalTime(),
            EndUtc = today.AddHours(11).ToUniversalTime(),
            DetectedUtc = today.AddHours(11).ToUniversalTime(),
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.94f,
            Explanation = "Continuous application focus was active for 120 uninterrupted minutes without an idle interval exceeding 5 minutes."
        });

        _cachedPatterns.Add(new PatternEvent
        {
            Id = 2,
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = today.AddHours(14).ToUniversalTime(),
            EndUtc = today.AddHours(14).AddMinutes(15).ToUniversalTime(),
            DetectedUtc = today.AddHours(14).AddMinutes(15).ToUniversalTime(),
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.88f,
            Explanation = "Observed 16 application focus transitions within a 10-minute observation window across communication and browsing tasks."
        });

        _cachedPatterns.Add(new PatternEvent
        {
            Id = 3,
            PatternType = BehavioralPatternType.LateNightUsageSpike,
            StartUtc = today.AddHours(1).AddMinutes(15).ToUniversalTime(),
            EndUtc = today.AddHours(3).ToUniversalTime(),
            DetectedUtc = today.AddHours(3).ToUniversalTime(),
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.92f,
            Explanation = "Device usage detected between 01:15 and 03:00, which deviates from your standard diurnal activity envelope."
        });

        _cachedPatterns.Add(new PatternEvent
        {
            Id = 4,
            PatternType = BehavioralPatternType.CompositeScrollLike,
            StartUtc = today.AddHours(16).ToUniversalTime(),
            EndUtc = today.AddHours(16).AddMinutes(55).ToUniversalTime(),
            DetectedUtc = today.AddHours(16).AddMinutes(55).ToUniversalTime(),
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.85f,
            Explanation = "Application maintained foreground status for 55 minutes with low input frequency (reading/media consumption profile)."
        });

        // Baseline profile
        _cachedBaseline = new UserBaseline
        {
            MedianDailyActiveSeconds = 395.0 * 60,
            MedianSwitchRatePerMinute = 2.4,
            MedianSessionLengthSeconds = 28.0 * 60,
            MedianLateNightActiveSeconds = 0,
            MedianAppEntropy = 1.8,
            SampleDaysCount = 14
        };
    }

    public IReadOnlyList<DailyMetrics> GetDemoWeeklyMetrics() => _cachedMetrics;
    public DailyMetrics GetDemoTodayMetrics() => _cachedMetrics.Last();
    public IReadOnlyList<ActivitySession> GetDemoTodaySessions() => _cachedSessions;
    public IReadOnlyList<PatternEvent> GetDemoDetectedPatterns() => _cachedPatterns;
    public UserBaseline GetDemoBaselineProfile() => _cachedBaseline;
}
