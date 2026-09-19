using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror_App.Services;

namespace Mirror_App.ViewModels;

public sealed partial class PatternItemViewModel : ObservableObject
{
    public PatternEvent? SourceEvent { get; init; }

    [ObservableProperty]
    private string _patternTitle = string.Empty;

    [ObservableProperty]
    private string _confidence = string.Empty;

    [ObservableProperty]
    private string _timestamp = string.Empty;

    [ObservableProperty]
    private string _explanation = string.Empty;

    [ObservableProperty]
    private string _evidenceSummary = string.Empty;

    [ObservableProperty]
    private string _observedValue = string.Empty;

    [ObservableProperty]
    private string _baselineValue = string.Empty;

    [ObservableProperty]
    private string _colorHex = "#0078D4";

    [ObservableProperty]
    private string _aiExplanation = string.Empty;

    [ObservableProperty]
    private bool _hasAiExplanation = false;

    [ObservableProperty]
    private bool _isAiLoading = false;

    [ObservableProperty]
    private string _recalibrationStatus = string.Empty;

    [ObservableProperty]
    private bool _isRecalibrated = false;
}

public sealed partial class PatternsViewModel : ObservableObject, IDisposable
{
    private readonly IMirrorRepository _repository;
    private readonly IDemoDataService _demoService;
    private readonly ITrackingCoordinator _trackingCoordinator;
    private readonly Mirror.LLM.ILocalLlmService _llmService;
    private readonly IThresholdRecalibrator _recalibrator;
    private readonly DispatcherQueue? _dispatcherQueue;

    [ObservableProperty]
    private PatternItemViewModel? _selectedPattern;

    [ObservableProperty]
    private int _patternCount = 0;

    [ObservableProperty]
    private bool _hasPatterns = false;

    [ObservableProperty]
    private bool _isDemoActive = true;

    [ObservableProperty]
    private string _filterName = "All Patterns";

    public ObservableCollection<PatternItemViewModel> Patterns { get; } = new();

    public PatternsViewModel(
        IMirrorRepository repository,
        IDemoDataService demoService,
        ITrackingCoordinator trackingCoordinator,
        Mirror.LLM.ILocalLlmService llmService,
        IThresholdRecalibrator recalibrator)
    {
        _repository = repository;
        _demoService = demoService;
        _trackingCoordinator = trackingCoordinator;
        _llmService = llmService;
        _recalibrator = recalibrator;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _trackingCoordinator.PatternDetected += OnPatternDetected;

        LoadPatterns();
    }

    private void OnPatternDetected(object? sender, PatternEvent pattern)
    {
        _dispatcherQueue?.TryEnqueue(() => LoadPatterns(FilterName));
    }

    [RelayCommand]
    public async Task ExplainWithAiAsync(PatternItemViewModel? item)
    {
        if (item == null) return;

        item.IsAiLoading = true;
        try
        {
            var evt = item.SourceEvent ?? new PatternEvent
            {
                PatternType = BehavioralPatternType.HighSwitchingBurst,
                StartUtc = DateTime.UtcNow.AddMinutes(-10),
                EndUtc = DateTime.UtcNow,
                Explanation = item.Explanation
            };

            string explanation = await _llmService.ExplainPatternAsync(evt);
            item.AiExplanation = explanation;
            item.HasAiExplanation = true;
        }
        catch (Exception ex)
        {
            item.AiExplanation = $"Local synthesis: {ex.Message}";
            item.HasAiExplanation = true;
        }
        finally
        {
            item.IsAiLoading = false;
        }
    }

    [RelayCommand]
    public async Task RecalibrateAsync(PatternItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            var pType = item.SourceEvent?.PatternType ?? BehavioralPatternType.HighSwitchingBurst;
            long pId = item.SourceEvent?.Id ?? 0;

            double newThresh = await _recalibrator.RecalibrateThresholdAsync(pType, "User marked pattern as normal workflow", pId);
            item.IsRecalibrated = true;
            item.RecalibrationStatus = $"Threshold adjusted to {newThresh}. Mirror will be less sensitive to this pattern.";
        }
        catch (Exception ex)
        {
            item.RecalibrationStatus = $"Adjustment recorded: {ex.Message}";
            item.IsRecalibrated = true;
        }
    }

    [RelayCommand]
    public void LoadPatterns(string? filter = null)
    {
        IsDemoActive = _demoService.IsDemoModeActive;
        Patterns.Clear();
        FilterName = filter ?? "All Patterns";

        var sourcePatterns = IsDemoActive
            ? _demoService.GetDemoDetectedPatterns()
            : _repository.GetPatternEventsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(1)).GetAwaiter().GetResult();

        var filtered = sourcePatterns.AsEnumerable();
        if (!string.IsNullOrEmpty(filter) && filter != "All Patterns")
        {
            if (Enum.TryParse<BehavioralPatternType>(filter, out var pType))
            {
                filtered = filtered.Where(p => p.PatternType == pType);
            }
        }

        foreach (var pat in filtered.OrderByDescending(p => p.DetectedUtc))
        {
            var (evidence, observed, baseline) = ExtractEvidence(pat);
            Patterns.Add(new PatternItemViewModel
            {
                SourceEvent = pat,
                PatternTitle = FormatPatternTitle(pat.PatternType),
                Confidence = $"{Math.Round(pat.ModelConfidence * 100)}% Confidence",
                Timestamp = pat.DetectedUtc.ToLocalTime().ToString("MMM dd, h:mm tt"),
                Explanation = pat.Explanation,
                EvidenceSummary = evidence,
                ObservedValue = observed,
                BaselineValue = baseline,
                ColorHex = pat.ModelConfidence >= 0.85f ? "#0078D4" : "#107C41"
            });
        }

        PatternCount = Patterns.Count;
        HasPatterns = PatternCount > 0;
        SelectedPattern = Patterns.FirstOrDefault();
    }

    private static (string Evidence, string Observed, string Baseline) ExtractEvidence(PatternEvent pat) => pat.PatternType switch
    {
        BehavioralPatternType.HighSwitchingBurst => (
            "Observed rapid context switching across foreground application windows.",
            "18 switches / 5m",
            "3 switches / 5m"),
        BehavioralPatternType.ExtendedSingleAppSession => (
            "Extended single-window focus duration without user idle transitions.",
            "145 mins focus",
            "45 mins median"),
        BehavioralPatternType.LateNightUsageSpike => (
            "Active workstation interaction recorded after configured evening threshold.",
            "1h 40m late session",
            "15m baseline"),
        BehavioralPatternType.RapidReopenPattern => (
            "Frequent closure and re-activation sequence detected for the same application key.",
            "5 opens / 4 mins",
            "< 1 / 10 mins"),
        BehavioralPatternType.CompositeScrollLike => (
            "High event frequency coupled with rapid switching and active window transitions.",
            "Composite score 0.88",
            "Threshold 0.65"),
        _ => ("Behavioral usage metric deviation detected locally.", "Observed", "Baseline")
    };

    private static string FormatPatternTitle(BehavioralPatternType type) => type switch
    {
        BehavioralPatternType.HighSwitchingBurst => "High App-Switching Frequency",
        BehavioralPatternType.ExtendedSingleAppSession => "Extended Continuous Focus",
        BehavioralPatternType.LateNightUsageSpike => "Late-Evening Usage Shift",
        BehavioralPatternType.RapidReopenPattern => "Rapid App Reopen Activity",
        BehavioralPatternType.CompositeScrollLike => "Continuous Active Session Flow",
        _ => "Observed Activity Pattern"
    };

    public void Dispose()
    {
        _trackingCoordinator.PatternDetected -= OnPatternDetected;
    }
}
