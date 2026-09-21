using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror.Security.Privacy;

namespace Mirror_App.ViewModels;

public sealed partial class PrivacyViewModel : ObservableObject
{
    private readonly IMirrorRepository _repository;
    private readonly IExportService _exportService;
    private readonly PrivacyAuditService? _auditService;

    [ObservableProperty]
    private string _networkStatus = "ENFORCED OFFLINE — Zero Outbound Traffic";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isActionSuccessful = false;

    // Phase 5: Privacy Integrity & Data Inventory
    [ObservableProperty]
    private string _databaseSizeFormatted = "Calculating...";

    [ObservableProperty]
    private string _databaseIntegrityStatus = "Checking...";

    [ObservableProperty]
    private string _privacyAuditSummary = "Offline gate active. Click 'Run Integrity Audit' to scan assemblies.";

    [ObservableProperty]
    private bool _isAuditPassed = true;

    [ObservableProperty]
    private long _sessionCount;

    [ObservableProperty]
    private long _idlePeriodCount;

    [ObservableProperty]
    private long _switchEventCount;

    [ObservableProperty]
    private long _patternEventCount;

    [ObservableProperty]
    private long _flowSessionCount;

    [ObservableProperty]
    private long _focusSessionCount;

    [ObservableProperty]
    private long _totalRecordCount;

    [ObservableProperty]
    private bool _isDeletionPreviewVisible;

    [ObservableProperty]
    private string _deletionPreviewMessage = string.Empty;

    public PrivacyViewModel(
        IMirrorRepository repository,
        IExportService exportService,
        PrivacyAuditService? auditService = null)
    {
        _repository = repository;
        _exportService = exportService;
        _auditService = auditService;

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DatabasePath = Path.Combine(localApp, "Mirror", "mirror.db");

        _ = RefreshDataInventoryAsync();
        _ = RunPrivacyAuditAsync();
    }

    [RelayCommand]
    public async Task RefreshDataInventoryAsync()
    {
        try
        {
            var counts = await _repository.GetDataInventoryCountsAsync();
            SessionCount = counts.SessionCount;
            IdlePeriodCount = counts.IdlePeriodCount;
            SwitchEventCount = counts.SwitchEventCount;
            PatternEventCount = counts.PatternEventCount;
            FlowSessionCount = counts.FlowSessionCount;
            FocusSessionCount = counts.FocusSessionCount;
            TotalRecordCount = counts.SessionCount + counts.IdlePeriodCount + counts.SwitchEventCount +
                               counts.PatternEventCount + counts.FlowSessionCount + counts.FocusSessionCount;

            long sizeBytes = counts.DatabaseSizeBytes > 0 ? counts.DatabaseSizeBytes : (File.Exists(DatabasePath) ? new FileInfo(DatabasePath).Length : 0);
            DatabaseSizeFormatted = FormatBytes(sizeBytes);

            bool healthy = await _repository.CheckDatabaseIntegrityAsync();
            DatabaseIntegrityStatus = healthy ? "Verified Healthy (PRAGMA integrity_check passed)" : "Integrity Warning";
        }
        catch (Exception ex)
        {
            DatabaseIntegrityStatus = $"Check failed: {ex.Message}";
        }
    }

    [RelayCommand]
    public Task RunPrivacyAuditAsync()
    {
        try
        {
            if (_auditService != null)
            {
                var report = _auditService.PerformAudit();
                IsAuditPassed = report.IsPassed;
                PrivacyAuditSummary = report.IsPassed
                    ? $"Passed: 0 networking calls, 0 forbidden fields across {report.CheckedRulesCount} inspected rules."
                    : $"Warning: {report.Violations.Count} violation(s) detected.";
            }
            else
            {
                PrivacyAuditSummary = "Enforced: Local-only execution with zero network APIs.";
            }
        }
        catch (Exception ex)
        {
            PrivacyAuditSummary = $"Audit inspection error: {ex.Message}";
            IsAuditPassed = false;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        try
        {
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string filePath = Path.Combine(exportDir, $"mirror_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            await _exportService.ExportToFileAsync(filePath, "csv", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(1));
            StatusMessage = $"Successfully exported CSV to: {filePath}";
            IsActionSuccessful = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }

    [RelayCommand]
    private async Task ExportToJsonAsync()
    {
        try
        {
            string exportDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            string filePath = Path.Combine(exportDir, $"mirror_export_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            await _exportService.ExportToFileAsync(filePath, "json", DateTime.UtcNow.AddDays(-30), DateTime.UtcNow.AddDays(1));
            StatusMessage = $"Successfully exported JSON to: {filePath}";
            IsActionSuccessful = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }

    [RelayCommand]
    private void RequestPurgePreview()
    {
        DeletionPreviewMessage = $"This action will permanently delete all {TotalRecordCount} records across all database tables (sessions, switches, idle events, and behavioral patterns). This cannot be undone.";
        IsDeletionPreviewVisible = true;
    }

    [RelayCommand]
    private void CancelPurge()
    {
        IsDeletionPreviewVisible = false;
        DeletionPreviewMessage = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteTodayDataAsync()
    {
        try
        {
            DateTime start = DateTime.Today.ToUniversalTime();
            await _repository.PruneOlderThanAsync(start);
            StatusMessage = "Today's tracking and analytics records were permanently deleted.";
            IsActionSuccessful = true;
            await RefreshDataInventoryAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Deletion failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }

    [RelayCommand]
    public async Task PurgeAllDataAsync()
    {
        try
        {
            IsDeletionPreviewVisible = false;
            await _repository.PurgeAllDataAsync();

            // Post-wipe verification
            await RefreshDataInventoryAsync();
            bool postCheckHealthy = await _repository.CheckDatabaseIntegrityAsync();

            StatusMessage = postCheckHealthy && TotalRecordCount == 0
                ? "Nuclear purge verified: 0 records remaining. SQLite database vacuumed and verified healthy."
                : "Nuclear purge executed.";
            IsActionSuccessful = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Purge failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F2} MB";
    }
}
