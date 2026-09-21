using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Analytics;

public record EventReplayReport(
    int TotalEventsReplayed,
    int TotalSessionsConstructed,
    IReadOnlyList<PatternEvent> DetectedPatterns,
    PatternAgreementDiagnostics Diagnostics,
    TimeSpan ReplayDuration
);

public class EventReplayEngine
{
    private readonly IPatternDetector _patternDetector;
    private readonly IPatternFusionEngine _fusionEngine;

    public EventReplayEngine(
        IPatternDetector? patternDetector = null,
        IPatternFusionEngine? fusionEngine = null)
    {
        _patternDetector = patternDetector ?? new PatternDetector();
        _fusionEngine = fusionEngine ?? new PatternFusionEngine();
    }

    public EventReplayReport ReplayTimeline(
        IReadOnlyList<ActivitySession> sessions,
        IReadOnlyList<AppSwitchEvent> switches,
        UserSettings? settings = null,
        UserBaseline? baseline = null)
    {
        var sw = Stopwatch.StartNew();
        settings ??= new UserSettings();
        var detectedList = new List<PatternEvent>();

        // Replay through the timeline in chronological order
        var timestamps = sessions.Select(s => s.EndUtc)
            .Concat(switches.Select(s => s.TimestampUtc))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        foreach (var time in timestamps)
        {
            var activeSessions = sessions.Where(s => s.StartUtc <= time && s.EndUtc >= time.AddMinutes(-settings.HighSwitchWindowMinutes)).ToList();
            var activeSwitches = switches.Where(s => s.TimestampUtc <= time && s.TimestampUtc >= time.AddMinutes(-settings.HighSwitchWindowMinutes)).ToList();

            var ruleCandidates = _patternDetector.EvaluateRules(activeSessions, activeSwitches, settings, baseline, time);
            foreach (var candidate in ruleCandidates)
            {
                var fused = _fusionEngine.FuseSignals(candidate, null);
                if (fused != null)
                {
                    detectedList.Add(fused);
                }
            }
        }

        sw.Stop();

        // Deduplicate detected patterns of identical type within small time windows
        var uniquePatterns = detectedList
            .GroupBy(p => new { p.PatternType, Minute = p.DetectedUtc.ToString("yyyyMMdd_HHmm") })
            .Select(g => g.First())
            .ToList();

        return new EventReplayReport(
            TotalEventsReplayed: sessions.Count + switches.Count,
            TotalSessionsConstructed: sessions.Count,
            DetectedPatterns: uniquePatterns,
            Diagnostics: _fusionEngine.GetAgreementDiagnostics(),
            ReplayDuration: sw.Elapsed
        );
    }
}
