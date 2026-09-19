using System.Globalization;
using System.Text;
using System.Text.Json;
using Mirror.Core.Interfaces;

namespace Mirror.Persistence;

public class ExportService : IExportService
{
    private readonly IMirrorRepository _repository;

    public ExportService(IMirrorRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> ExportAsCsvAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var sessions = await _repository.GetSessionsAsync(startUtc, endUtc, ct);
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("start_utc,end_utc,app,category,duration_minutes,active_seconds,close_reason");

        foreach (var s in sessions)
        {
            double durationMinutes = Math.Round(s.Duration.TotalMinutes, 1);
            sb.AppendLine(string.Format(
                CultureInfo.InvariantCulture,
                "{0:o},{1:o},\"{2}\",\"{3}\",{4},{5},{6}",
                s.StartUtc,
                s.EndUtc,
                EscapeCsv(s.DisplayName),
                EscapeCsv(s.Category),
                durationMinutes,
                s.ActiveSeconds,
                s.CloseReason
            ));
        }

        return sb.ToString();
    }

    public async Task<string> ExportAsJsonAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var sessions = await _repository.GetSessionsAsync(startUtc, endUtc, ct);
        var patterns = await _repository.GetPatternEventsAsync(startUtc, endUtc, ct);

        var exportPayload = new
        {
            export_version = "1.0",
            exported_utc = DateTime.UtcNow.ToString("o"),
            date_range = new
            {
                start_utc = startUtc.ToString("o"),
                end_utc = endUtc.ToString("o")
            },
            sessions = sessions.Select(s => new
            {
                app = s.DisplayName,
                category = s.Category,
                start_utc = s.StartUtc.ToString("o"),
                end_utc = s.EndUtc.ToString("o"),
                duration_minutes = Math.Round(s.Duration.TotalMinutes, 1),
                active_seconds = s.ActiveSeconds,
                close_reason = s.CloseReason.ToString()
            }),
            patterns = patterns.Select(p => new
            {
                pattern = p.PatternType.ToString(),
                start_utc = p.StartUtc.ToString("o"),
                end_utc = p.EndUtc.ToString("o"),
                explanation = p.Explanation
            })
        };

        return JsonSerializer.Serialize(exportPayload, new JsonSerializerOptions { WriteIndented = true });
    }

    public async Task ExportToFileAsync(string filePath, string format, DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        string content = format.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? await ExportAsJsonAsync(startUtc, endUtc, ct)
            : await ExportAsCsvAsync(startUtc, endUtc, ct);

        string? dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, ct);
    }

    private static string EscapeCsv(string field)
    {
        return field.Replace("\"", "\"\"");
    }
}
