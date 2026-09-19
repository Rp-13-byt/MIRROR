using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Persistence;
using Mirror.Reporting;
using Mirror.Security;
using Xunit;

namespace Mirror.Persistence.Tests;

public class PdfReportServiceTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly string _tempPdfPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly MirrorRepository _repo;
    private readonly PdfReportService _reportService;

    public PdfReportServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"mirror_test_pdf_{Guid.NewGuid():N}.db");
        _tempPdfPath = Path.Combine(Path.GetTempPath(), $"mirror_test_report_{Guid.NewGuid():N}.pdf");
        _factory = new SqliteConnectionFactory(_tempDbPath);
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        _repo = new MirrorRepository(_factory, new PrivacyGuard());
        _reportService = new PdfReportService(_repo);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
        if (File.Exists(_tempPdfPath))
        {
            try { File.Delete(_tempPdfPath); } catch { }
        }
    }

    [Fact]
    public async Task GenerateWeeklyReport_CreatesValidPdfFile()
    {
        // Seed some sample data
        var now = DateTime.UtcNow;
        await _repo.InsertSessionAsync(new ActivitySession
        {
            AppKey = "dev.vscode",
            DisplayName = "Visual Studio Code",
            Category = "Development",
            StartUtc = now.AddHours(-2),
            EndUtc = now.AddHours(-1),
            ActiveSeconds = 3600,
            CloseReason = SessionCloseReason.AppSwitch
        });

        await _repo.InsertFlowStateSessionAsync(new FlowStateSession
        {
            AppKey = "dev.vscode",
            DisplayName = "Visual Studio Code",
            StartUtc = now.AddHours(-2),
            EndUtc = now.AddHours(-1),
            DurationSeconds = 3600,
            DistractionCount = 0
        });

        string generatedPath = await _reportService.GenerateWeeklyReportAsync(_tempPdfPath);

        Assert.True(File.Exists(generatedPath), "Generated PDF report file must exist on disk.");
        var fileInfo = new FileInfo(generatedPath);
        Assert.True(fileInfo.Length > 1000, $"PDF report file size should be substantial (actual: {fileInfo.Length} bytes).");
    }
}
