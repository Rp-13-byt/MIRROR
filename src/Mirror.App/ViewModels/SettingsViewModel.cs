using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror.Platform;

namespace Mirror_App.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IMirrorRepository _repository;
    private readonly IStartupManager _startupManager;
    private readonly IWalletService _walletService;
    private readonly Mirror.Reporting.IPdfReportService _pdfReportService;
    private readonly IThresholdRecalibrator _recalibrator;

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

    public SettingsViewModel(
        IMirrorRepository repository,
        IStartupManager startupManager,
        IWalletService walletService,
        Mirror.Reporting.IPdfReportService pdfReportService,
        IThresholdRecalibrator recalibrator)
    {
        _repository = repository;
        _startupManager = startupManager;
        _walletService = walletService;
        _pdfReportService = pdfReportService;
        _recalibrator = recalibrator;
        LoadSettings();
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
