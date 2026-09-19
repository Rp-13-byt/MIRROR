using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Reporting;

public interface IPdfReportService
{
    Task<string> GenerateWeeklyReportAsync(string? targetPath = null, CancellationToken ct = default);
}

public class PdfReportService : IPdfReportService
{
    private readonly IMirrorRepository _repository;

    static PdfReportService()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        QuestPDF.Settings.ThrowOnMissingFontFamilies = false;
    }

    public PdfReportService(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> GenerateWeeklyReportAsync(string? targetPath = null, CancellationToken ct = default)
    {
        DateTime endUtc = DateTime.UtcNow;
        DateTime startUtc = endUtc.AddDays(-7);

        string startDateLocal = startUtc.ToLocalTime().ToString("yyyy-MM-dd");
        string endDateLocal = endUtc.ToLocalTime().ToString("yyyy-MM-dd");

        var dailyMetrics = await _repository.GetDailyMetricsRangeAsync(startDateLocal, endDateLocal, ct);
        var sessions = await _repository.GetSessionsAsync(startUtc, endUtc, ct);
        var patterns = await _repository.GetPatternEventsAsync(startUtc, endUtc, ct);
        var flows = await _repository.GetFlowStateSessionsAsync(startUtc, endUtc, ct);
        var baseline = await _repository.GetAdaptiveBaselineAsync("daily_active_seconds", 14, ct);

        int totalActive = dailyMetrics.Sum(m => m.ActiveSeconds);
        int totalIdle = dailyMetrics.Sum(m => m.IdleSeconds);
        int totalSwitches = dailyMetrics.Sum(m => m.SwitchCount);
        int lateNight = dailyMetrics.Sum(m => m.LateNightSeconds);
        int flowTotalSec = flows.Sum(f => f.DurationSeconds);

        if (totalActive == 0 && sessions.Count > 0)
        {
            totalActive = sessions.Sum(s => s.ActiveSeconds);
        }

        // Top apps
        var topApps = new List<WeeklyReportAppUsage>();
        if (sessions.Count > 0)
        {
            int allSessionSec = sessions.Sum(s => s.ActiveSeconds);
            if (allSessionSec == 0) allSessionSec = 1;

            topApps = sessions
                .GroupBy(s => (s.DisplayName, s.Category))
                .Select(g => new WeeklyReportAppUsage(
                    g.Key.DisplayName,
                    g.Key.Category,
                    g.Sum(s => s.ActiveSeconds),
                    (g.Sum(s => s.ActiveSeconds) / (double)allSessionSec) * 100.0))
                .OrderByDescending(a => a.ActiveSeconds)
                .Take(5)
                .ToList();
        }

        string baselineStatus = baseline != null
            ? (baseline.IsLearningPhase
                ? $"Learning personal baseline ({baseline.SampleCount} of 7 days recorded)."
                : "Personal baseline stable based on 14 days of local activity.")
            : "Adaptive baseline learning in progress.";

        var data = new WeeklyReportData
        {
            StartDate = startUtc.ToLocalTime(),
            EndDate = endUtc.ToLocalTime(),
            TotalActiveSeconds = totalActive,
            TotalIdleSeconds = totalIdle,
            TotalSwitches = totalSwitches,
            LateNightSeconds = lateNight,
            FlowSessionsCount = flows.Count,
            FlowTotalSeconds = flowTotalSec,
            PatternEventsCount = patterns.Count,
            BaselineStatus = baselineStatus,
            TopApps = topApps
        };

        if (string.IsNullOrWhiteSpace(targetPath))
        {
            string docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mirror");
            if (!Directory.Exists(docs))
            {
                Directory.CreateDirectory(docs);
            }
            targetPath = Path.Combine(docs, $"Mirror_Weekly_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }
        else
        {
            string? dir = Path.GetDirectoryName(targetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        var doc = new WeeklyReportDocument(data);
        doc.GeneratePdf(targetPath);

        return targetPath;
    }
}

