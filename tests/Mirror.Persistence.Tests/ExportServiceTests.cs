using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Persistence;
using Mirror.Security;
using Xunit;

namespace Mirror.Persistence.Tests;

public class ExportServiceTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly MirrorRepository _repository;
    private readonly ExportService _exportService;

    public ExportServiceTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"mirror_export_test_{Guid.NewGuid():N}.db");
        _connectionFactory = new SqliteConnectionFactory(_testDbPath);
        var migrator = new DatabaseMigrator(_connectionFactory);
        migrator.Migrate();
        _repository = new MirrorRepository(_connectionFactory, new PrivacyGuard());
        _exportService = new ExportService(_repository);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            if (File.Exists(_testDbPath)) File.Delete(_testDbPath);
            var shm = _testDbPath + "-shm";
            var wal = _testDbPath + "-wal";
            if (File.Exists(shm)) File.Delete(shm);
            if (File.Exists(wal)) File.Delete(wal);
        }
        catch { }
    }

    [Fact]
    public async Task ExportAsCsvAsync_GeneratesValidCsvStructure()
    {
        var now = DateTime.UtcNow;
        var session = new ActivitySession
        {
            AppKey = "code",
            DisplayName = "Visual Studio Code",
            Category = "Development",
            StartUtc = now.AddMinutes(-30),
            EndUtc = now,
            ActiveSeconds = 1800,
            CloseReason = SessionCloseReason.AppSwitch
        };

        await _repository.InsertSessionAsync(session);

        var csv = await _exportService.ExportAsCsvAsync(now.AddHours(-1), now.AddHours(1));

        Assert.Contains("start_utc,end_utc,app,category,duration_minutes,active_seconds,close_reason", csv);
        Assert.Contains("Visual Studio Code", csv);
        Assert.Contains("Development", csv);
        Assert.Contains("AppSwitch", csv);
    }

    [Fact]
    public async Task ExportAsJsonAsync_GeneratesValidJsonStructure()
    {
        var now = DateTime.UtcNow;
        var session = new ActivitySession
        {
            AppKey = "slack",
            DisplayName = "Slack",
            Category = "Communication",
            StartUtc = now.AddMinutes(-15),
            EndUtc = now,
            ActiveSeconds = 900,
            CloseReason = SessionCloseReason.Idle
        };
        await _repository.InsertSessionAsync(session);

        var pattern = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = now.AddMinutes(-15),
            EndUtc = now,
            DetectedUtc = now,
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.95f,
            Explanation = "Frequent switches detected across multiple applications."
        };
        await _repository.InsertPatternEventAsync(pattern);

        var json = await _exportService.ExportAsJsonAsync(now.AddHours(-1), now.AddHours(1));

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal("1.0", root.GetProperty("export_version").GetString());
        Assert.True(root.TryGetProperty("sessions", out var sessionsEl));
        Assert.True(root.TryGetProperty("patterns", out var patternsEl));
        Assert.Equal(1, sessionsEl.GetArrayLength());
        Assert.Equal(1, patternsEl.GetArrayLength());
    }

    [Fact]
    public async Task ExportToFileAsync_WritesFileCorrectly()
    {
        var now = DateTime.UtcNow;
        var session = new ActivitySession
        {
            AppKey = "terminal",
            DisplayName = "Windows Terminal",
            Category = "Development",
            StartUtc = now.AddMinutes(-10),
            EndUtc = now,
            ActiveSeconds = 600,
            CloseReason = SessionCloseReason.Shutdown
        };
        await _repository.InsertSessionAsync(session);

        var tempCsv = Path.Combine(Path.GetTempPath(), $"mirror_test_export_{Guid.NewGuid():N}.csv");
        try
        {
            await _exportService.ExportToFileAsync(tempCsv, "csv", now.AddHours(-1), now.AddHours(1));
            Assert.True(File.Exists(tempCsv));
            var content = await File.ReadAllTextAsync(tempCsv);
            Assert.Contains("Windows Terminal", content);
        }
        finally
        {
            if (File.Exists(tempCsv)) File.Delete(tempCsv);
        }
    }
}
