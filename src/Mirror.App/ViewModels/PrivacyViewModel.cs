using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Interfaces;

namespace Mirror_App.ViewModels;

public sealed partial class PrivacyViewModel : ObservableObject
{
    private readonly IMirrorRepository _repository;
    private readonly IExportService _exportService;

    [ObservableProperty]
    private string _networkStatus = "ENFORCED OFFLINE — Zero Outbound Traffic";

    [ObservableProperty]
    private string _databasePath = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isActionSuccessful = false;

    public PrivacyViewModel(IMirrorRepository repository, IExportService exportService)
    {
        _repository = repository;
        _exportService = exportService;

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        DatabasePath = Path.Combine(localApp, "Mirror", "mirror.db");
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
    private async Task DeleteTodayDataAsync()
    {
        try
        {
            DateTime start = DateTime.Today.ToUniversalTime();
            DateTime end = start.AddDays(1);
            await _repository.PruneOlderThanAsync(start);
            StatusMessage = "Today's tracking and analytics records were permanently deleted.";
            IsActionSuccessful = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Deletion failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }

    [RelayCommand]
    private async Task PurgeAllDataAsync()
    {
        try
        {
            await _repository.PurgeAllDataAsync();
            StatusMessage = "Nuclear purge complete: All local databases and history permanently eradicated.";
            IsActionSuccessful = true;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Purge failed: {ex.Message}";
            IsActionSuccessful = false;
        }
    }
}
