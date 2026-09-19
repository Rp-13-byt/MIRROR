using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror_App.Services;

namespace Mirror_App.ViewModels;

public sealed partial class DayTrendItem : ObservableObject
{
    public string DayName { get; set; } = string.Empty;
    public string DateFormatted { get; set; } = string.Empty;
    public double ActiveHours { get; set; }
    public string ActiveHoursFormatted { get; set; } = string.Empty;
    public int ContextSwitches { get; set; }
    public double RelativeHeight { get; set; }
}

public sealed partial class TrendsViewModel : ObservableObject
{
    private readonly IMirrorRepository _repository;
    private readonly IDemoDataService _demoService;
    private readonly IAdaptiveBaselineService _adaptiveBaselineService;

    [ObservableProperty]
    private string _weeklyAverageActive = "0h 0m";

    [ObservableProperty]
    private string _baselineComparisonText = "Observing typical behavioral envelope";

    [ObservableProperty]
    private double _baselineMedianDailyHours = 6.5;

    [ObservableProperty]
    private string _standardDeviationHours = "± 1.1h";

    [ObservableProperty]
    private string _upperBoundHours = "8.7h (+2σ)";

    [ObservableProperty]
    private bool _isLearningPhase = false;

    [ObservableProperty]
    private string _learningGateMessage = "Learning your baseline — 4 of 7 days recorded. Pattern alerts begin after Day 7.";

    [ObservableProperty]
    private string _baselineStatusDescription = "This is your personal baseline calculated from the last 14 days of local activity.";

    [ObservableProperty]
    private int _recordedDaysCount = 14;

    [ObservableProperty]
    private bool _isDemoActive = true;

    public ObservableCollection<DayTrendItem> DayTrends { get; } = new();

    public TrendsViewModel(
        IMirrorRepository repository,
        IDemoDataService demoService,
        IAdaptiveBaselineService adaptiveBaselineService)
    {
        _repository = repository;
        _demoService = demoService;
        _adaptiveBaselineService = adaptiveBaselineService;
        LoadTrends();
    }

    [RelayCommand]
    public void LoadTrends()
    {
        IsDemoActive = _demoService.IsDemoModeActive;
        DayTrends.Clear();

        var metricsList = IsDemoActive
            ? _demoService.GetDemoWeeklyMetrics()
            : _repository.GetDailyMetricsRangeAsync(DateTime.Today.AddDays(-6).ToString("yyyy-MM-dd"), DateTime.Today.ToString("yyyy-MM-dd")).GetAwaiter().GetResult();

        if (metricsList.Count == 0)
        {
            WeeklyAverageActive = "0h 0m";
            BaselineComparisonText = "Insufficient data for 7-day trend analysis.";
            IsLearningPhase = true;
            LearningGateMessage = "Learning your baseline — 0 of 7 days recorded. Pattern alerts begin after Day 7.";
            return;
        }

        double maxSeconds = metricsList.Max(m => m.ActiveSeconds);
        if (maxSeconds <= 0) maxSeconds = 1;

        double sumSeconds = 0;
        foreach (var m in metricsList)
        {
            sumSeconds += m.ActiveSeconds;
            double hrs = Math.Round(m.ActiveSeconds / 3600.0, 1);
            double relHeight = Math.Max(10, (m.ActiveSeconds / maxSeconds) * 140);

            DateTime date = DateTime.TryParse(m.DateLocal, out var parsedDate) ? parsedDate : DateTime.Today;

            DayTrends.Add(new DayTrendItem
            {
                DayName = date.ToString("ddd"),
                DateFormatted = date.ToString("MMM dd"),
                ActiveHours = hrs,
                ActiveHoursFormatted = $"{hrs:F1}h",
                ContextSwitches = m.SwitchCount,
                RelativeHeight = relHeight
            });
        }

        double avgSecs = sumSeconds / metricsList.Count;
        int avgHrs = (int)(avgSecs / 3600);
        int avgRemainingMins = (int)((avgSecs % 3600) / 60);
        WeeklyAverageActive = $"{avgHrs}h {avgRemainingMins}m";

        if (IsDemoActive)
        {
            RecordedDaysCount = 14;
            IsLearningPhase = false;
            BaselineMedianDailyHours = 6.5;
            StandardDeviationHours = "± 1.1h";
            UpperBoundHours = "8.7h (+2σ)";
            BaselineStatusDescription = "This is your personal baseline calculated from the last 14 days of local activity.";
            BaselineComparisonText = $"7-day usage is consistent with your 14-day median baseline ({BaselineMedianDailyHours:F1}h/day).";
        }
        else
        {
            var baseline = _adaptiveBaselineService.ComputeAdaptiveBaselineAsync("daily_active_seconds", 14).GetAwaiter().GetResult();
            RecordedDaysCount = baseline.SampleCount;
            IsLearningPhase = baseline.IsLearningPhase;

            if (baseline.IsLearningPhase)
            {
                LearningGateMessage = baseline.LearningStatusMessage;
                BaselineComparisonText = "Pattern alerts and baseline comparisons remain dormant until 7 full days of local metrics are recorded.";
            }
            else
            {
                BaselineStatusDescription = "This is your personal baseline calculated from the last 14 days of local activity.";
                BaselineMedianDailyHours = Math.Round(baseline.MedianValue / 3600.0, 1);
                double sigmaHrs = Math.Round(baseline.StdDev / 3600.0, 1);
                double upperHrs = Math.Round(baseline.ThresholdUpper / 3600.0, 1);

                StandardDeviationHours = $"± {sigmaHrs:F1}h";
                UpperBoundHours = $"{upperHrs:F1}h (+2σ)";

                double diffPct = Math.Round(((avgSecs - baseline.MedianValue) / (baseline.MedianValue > 0 ? baseline.MedianValue : 1)) * 100, 1);
                if (Math.Abs(diffPct) < 5)
                {
                    BaselineComparisonText = $"7-day usage is consistent with your personal 14-day median baseline ({BaselineMedianDailyHours:F1}h/day).";
                }
                else if (diffPct > 0)
                {
                    BaselineComparisonText = $"7-day usage is {diffPct:F1}% above your personal 14-day median baseline ({BaselineMedianDailyHours:F1}h/day).";
                }
                else
                {
                    BaselineComparisonText = $"7-day usage is {Math.Abs(diffPct):F1}% below your personal 14-day median baseline ({BaselineMedianDailyHours:F1}h/day).";
                }
            }
        }
    }
}
