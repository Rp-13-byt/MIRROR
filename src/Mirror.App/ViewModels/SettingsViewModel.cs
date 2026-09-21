using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror.Platform;

namespace Mirror_App.ViewModels;

public sealed partial class PatternPreferenceItem : ObservableObject
{
    public BehavioralPatternType PatternType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    [ObservableProperty]
    private PatternVisibility _visibility;
}

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IMirrorRepository _repository;
    private readonly IStartupManager _startupManager;
    private readonly IWalletService _walletService;
    private readonly Mirror.Reporting.IPdfReportService _pdfReportService;
    private readonly IThresholdRecalibrator _recalibrator;
    private readonly ITrackingCoordinator? _trackingCoordinator;

    [ObservableProperty]
    private bool _launchOnStartup = false;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private int _idleThresholdSeconds = 180;

    [ObservableProperty]
    private bool _enableNpuAcceleration = true;

    [ObservableProperty]
    private string _newExcludedProcess = string.Empty;

    [ObservableProperty]
    private string _saveMessage = string.Empty;

    // Tracking Modes & Pause Controls
    [ObservableProperty]
    private int _trackingModeIndex = 0; // 0: All, 1: Selected Apps, 2: Selected Categories

    [ObservableProperty]
    private bool _isPaused = false;

    [ObservableProperty]
    private string _pauseStatusMessage = "Tracking is currently active.";

    // Innovation: Wellbeing Wallet
    [ObservableProperty]
    private string _walletPassword = string.Empty;

    [ObservableProperty]
    private string _walletStatusMessage = string.Empty;

    // Innovation: PDF Report
    [ObservableProperty]
    private string _pdfStatusMessage = string.Empty;

    public ObservableCollection<string> ExcludedProcesses { get; } = new();
    public ObservableCollection<string> ActiveRecalibrations { get; } = new();
    public ObservableCollection<PatternPreferenceItem> PatternPreferences { get; } = new();

    public SettingsViewModel(
        IMirrorRepository repository,
        IStartupManager startupManager,
        IWalletService walletService,
        Mirror.Reporting.IPdfReportService pdfReportService,
        IThresholdRecalibrator recalibrator,
        ITrackingCoordinator? trackingCoordinator = null)
    {
        _repository = repository;
        _startupManager = startupManager;
        _walletService = walletService;
        _pdfReportService = pdfReportService;
        _recalibrator = recalibrator;
        _trackingCoordinator = trackingCoordinator;

        if (_trackingCoordinator != null)
        {
            TrackingModeIndex = (int)_trackingCoordinator.TrackingMode;
            IsPaused = _trackingCoordinator.CurrentState == TrackingState.Paused;
            PauseStatusMessage = IsPaused ? "Tracking is paused." : "Tracking is active.";
        }

        LoadSettings();
        LoadPatternPreferences();
    }

    private void LoadSettings()
    {
        var settings = _repository.LoadSettingsAsync().GetAwaiter().GetResult();
        LaunchOnStartup = _startupManager.IsStartupEnabled();
        IdleThresholdSeconds = settings.IdleThresholdSeconds;

        ExcludedProcesses.Clear();
        foreach (var p in settings.ExcludedApps)
        {
            ExcludedProcesses.Add(p);
        }

        LoadRecalibrations();
    }

    private void LoadRecalibrations()
    {
        ActiveRecalibrations.Clear();
        var custom = _repository.GetRecalibratedThresholdsAsync().GetAwaiter().GetResult();
        foreach (var kvp in custom)
        {
            ActiveRecalibrations.Add($"{kvp.Key}: {kvp.Value}");
        }
    }

    private void LoadPatternPreferences()
    {
        PatternPreferences.Clear();
        var prefs = _repository.GetPatternPreferencesAsync().GetAwaiter().GetResult().ToDictionary(p => p.PatternType, p => p.Visibility);

        var patterns = new[]
        {
            (BehavioralPatternType.HighSwitchingBurst, "High-Switching Bursts", "Rapid toggles between multiple applications within short intervals"),
            (BehavioralPatternType.ExtendedSingleAppSession, "Extended Single-App Sessions", "Sustained focus in one application exceeding threshold"),
            (BehavioralPatternType.RapidReopenPattern, "Rapid Reopens", "Repeatedly closing and reopening an application within minutes"),
            (BehavioralPatternType.LateNightUsageSpike, "Late-Night Activity", "Significant interaction recorded past configured quiet hours"),
            (BehavioralPatternType.CompositeScrollLike, "Exploratory Scanning", "Rapid alternating window transitions resembling continuous browsing")
        };

        foreach (var (type, name, desc) in patterns)
        {
            var vis = prefs.TryGetValue(type, out var v) ? v : PatternVisibility.Show;
            PatternPreferences.Add(new PatternPreferenceItem
            {
                PatternType = type,
                Name = name,
                Description = desc,
                Visibility = vis
            });
        }
    }

    [RelayCommand]
    public async Task UpdatePatternPreferenceAsync(PatternPreferenceItem item)
    {
        if (item == null) return;
        await _repository.SavePatternPreferenceAsync(item.PatternType, item.Visibility);
        SaveMessage = $"Updated visibility for {item.Name}.";
    }

    [RelayCommand]
    public void SetTrackingMode(int modeIndex)
    {
        TrackingModeIndex = modeIndex;
        var mode = (TrackingMode)modeIndex;
        _trackingCoordinator?.SetTrackingMode(mode);
        SaveMessage = $"Tracking mode set to {mode}.";
    }

    [RelayCommand]
    public void Pause15Minutes()
    {
        _trackingCoordinator?.Pause(TimeSpan.FromMinutes(15));
        IsPaused = true;
        PauseStatusMessage = "Tracking paused for 15 minutes.";
    }

    [RelayCommand]
    public void Pause1Hour()
    {
        _trackingCoordinator?.Pause(TimeSpan.FromHours(1));
        IsPaused = true;
        PauseStatusMessage = "Tracking paused for 1 hour.";
    }

    [RelayCommand]
    public void Pause4Hours()
    {
        _trackingCoordinator?.Pause(TimeSpan.FromHours(4));
        IsPaused = true;
        PauseStatusMessage = "Tracking paused for 4 hours.";
    }

    [RelayCommand]
    public void PauseUntilTomorrow()
    {
        var tomorrowMorning = DateTime.Today.AddDays(1).AddHours(6);
        var duration = tomorrowMorning - DateTime.Now;
        if (duration <= TimeSpan.Zero) duration = TimeSpan.FromHours(8);
        _trackingCoordinator?.Pause(duration);
        IsPaused = true;
        PauseStatusMessage = "Tracking paused until tomorrow morning (06:00).";
    }

    [RelayCommand]
    public void ResumeTracking()
    {
        _trackingCoordinator?.Resume();
        IsPaused = false;
        PauseStatusMessage = "Tracking is currently active.";
    }

    [RelayCommand]
    private async Task ExportWalletAsync()
    {
        if (string.IsNullOrWhiteSpace(WalletPassword))
        {
            WalletStatusMessage = "Please enter an encryption password for the wallet.";
            return;
        }

        try
        {
            string docs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Mirror");
            if (!Directory.Exists(docs)) Directory.CreateDirectory(docs);
            string walletFile = Path.Combine(docs, $"Mirror_Wallet_{DateTime.Now:yyyyMMdd_HHmmss}.mirrorwallet");

            await _walletService.ExportWalletAsync(walletFile, WalletPassword);
            WalletStatusMessage = $"Encrypted wallet exported successfully to:\n{walletFile}";
        }
        catch (Exception ex)
        {
            WalletStatusMessage = $"Export error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task GeneratePdfReportAsync()
    {
        try
        {
            PdfStatusMessage = "Generating local PDF report card via QuestPDF...";
            string pdfPath = await _pdfReportService.GenerateWeeklyReportAsync();
            PdfStatusMessage = $"Report card generated locally and saved to:\n{pdfPath}";
        }
        catch (Exception ex)
        {
            PdfStatusMessage = $"PDF generation error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ResetRecalibrationsAsync()
    {
        try
        {
            await _recalibrator.ResetThresholdsAsync();
            LoadRecalibrations();
            SaveMessage = "All behavioral pattern thresholds reset to factory defaults.";
        }
        catch (Exception ex)
        {
            SaveMessage = $"Reset error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddExcludedProcess()
    {
        if (string.IsNullOrWhiteSpace(NewExcludedProcess)) return;
        string proc = NewExcludedProcess.Trim().ToLowerInvariant();
        if (!proc.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            proc += ".exe";
        }

        if (!ExcludedProcesses.Contains(proc, StringComparer.OrdinalIgnoreCase))
        {
            ExcludedProcesses.Add(proc);
            SaveSettings();
        }
        NewExcludedProcess = string.Empty;
    }

    [RelayCommand]
    private void RemoveExcludedProcess(string process)
    {
        if (ExcludedProcesses.Contains(process))
        {
            ExcludedProcesses.Remove(process);
            SaveSettings();
        }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        try
        {
            _startupManager.SetStartupEnabled(LaunchOnStartup);

            var settings = new UserSettings
            {
                StartWithWindows = LaunchOnStartup,
                IdleThresholdSeconds = IdleThresholdSeconds,
                ExcludedApps = new System.Collections.Generic.List<string>(ExcludedProcesses)
            };

            _repository.SaveSettingsAsync(settings).GetAwaiter().GetResult();
            SaveMessage = "Settings saved successfully.";
        }
        catch (Exception ex)
        {
            SaveMessage = $"Failed to save settings: {ex.Message}";
        }
    }
}
