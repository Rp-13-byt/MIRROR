using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror_App.Services;

namespace Mirror_App.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IDemoDataService _demoDataService;
    private readonly ITrackingCoordinator _trackingCoordinator;
    private readonly IInferenceBackendManager _inferenceManager;

    [ObservableProperty]
    private string _hardwareAccelerationName = "Snapdragon NPU / QNN (Hexagon)";

    [ObservableProperty]
    private bool _isNpuAccelerated = true;

    [ObservableProperty]
    private bool _isDemoMode = true;

    [ObservableProperty]
    private bool _isTrackingActive = true;

    [ObservableProperty]
    private string _statusText = "Ready — Observability Active";

    public MainViewModel(
        IDemoDataService demoDataService,
        ITrackingCoordinator trackingCoordinator,
        IInferenceBackendManager inferenceManager)
    {
        _demoDataService = demoDataService;
        _trackingCoordinator = trackingCoordinator;
        _inferenceManager = inferenceManager;

        _isDemoMode = _demoDataService.IsDemoModeActive;
        RefreshHardwareStatus();
    }

    [RelayCommand]
    private void ToggleDemoMode()
    {
        IsDemoMode = !IsDemoMode;
        _demoDataService.IsDemoModeActive = IsDemoMode;
    }

    [RelayCommand]
    private void ToggleTracking()
    {
        if (IsTrackingActive)
        {
            _trackingCoordinator.Stop();
            IsTrackingActive = false;
            StatusText = "Tracking Paused";
        }
        else
        {
            _trackingCoordinator.Start();
            IsTrackingActive = true;
            StatusText = "Observability Active";
        }
    }

    public void RefreshHardwareStatus()
    {
        HardwareAccelerationName = _inferenceManager.ActiveBackendName;
        IsNpuAccelerated = _inferenceManager.ActiveBackendKind == InferenceBackendKind.Qnn;
    }
}
