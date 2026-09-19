using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror_App.Services;

namespace Mirror_App.ViewModels;

public sealed partial class TimelineItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _formattedTime = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private string _categoryColor = "#0078D4";

    [ObservableProperty]
    private string _durationFormatted = string.Empty;

    [ObservableProperty]
    private string _closeReasonText = string.Empty;

    [ObservableProperty]
    private bool _isLiveNow = false;
}

public sealed partial class TimelineViewModel : ObservableObject, IDisposable
{
    private readonly IMirrorRepository _repository;
    private readonly IDemoDataService _demoService;
    private readonly ITrackingCoordinator _trackingCoordinator;
    private readonly DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _liveTimer;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private string _sessionCountText = "0 Sessions Logged";

    [ObservableProperty]
    private bool _isDemoActive = true;

    public ObservableCollection<TimelineItemViewModel> Sessions { get; } = new();

    public TimelineViewModel(
        IMirrorRepository repository,
        IDemoDataService demoService,
        ITrackingCoordinator trackingCoordinator)
    {
        _repository = repository;
        _demoService = demoService;
        _trackingCoordinator = trackingCoordinator;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _trackingCoordinator.SessionRecorded += OnSessionRecorded;
        _trackingCoordinator.ForegroundAppChanged += OnForegroundAppChanged;

        StartLiveTimer();
        LoadSessions();
    }

    private void StartLiveTimer()
    {
        if (_dispatcherQueue != null)
        {
            _liveTimer = _dispatcherQueue.CreateTimer();
            _liveTimer.Interval = TimeSpan.FromSeconds(1);
            _liveTimer.Tick += (s, e) => UpdateLiveSessionTick();
            _liveTimer.Start();
        }
    }

    private void UpdateLiveSessionTick()
    {
        if (IsDemoActive) return;

        if (_trackingCoordinator.CurrentState == TrackingState.Running && _trackingCoordinator.CurrentApp != null)
        {
            var activeItem = Sessions.FirstOrDefault(x => x.IsLiveNow);
            if (activeItem != null && activeItem.ProcessName == _trackingCoordinator.CurrentApp.AppKey)
            {
                activeItem.DurationFormatted = FormatDuration(_trackingCoordinator.CurrentSessionDuration);
            }
            else
            {
                LoadSessions();
            }
        }
        else
        {
            var activeItem = Sessions.FirstOrDefault(x => x.IsLiveNow);
            if (activeItem != null)
            {
                LoadSessions();
            }
        }
    }

    private void OnSessionRecorded(object? sender, ActivitySession session)
    {
        _dispatcherQueue?.TryEnqueue(LoadSessions);
    }

    private void OnForegroundAppChanged(object? sender, AppIdentity? identity)
    {
        _dispatcherQueue?.TryEnqueue(LoadSessions);
    }

    [RelayCommand]
    public void LoadSessions()
    {
        IsDemoActive = _demoService.IsDemoModeActive;
        Sessions.Clear();

        if (IsDemoActive)
        {
            var demoSessions = _demoService.GetDemoTodaySessions().OrderByDescending(s => s.StartUtc).ToList();
            TotalSessions = demoSessions.Count;
            SessionCountText = $"{TotalSessions} Sessions Logged Today";

            bool isFirst = true;
            foreach (var s in demoSessions)
            {
                Sessions.Add(new TimelineItemViewModel
                {
                    FormattedTime = s.StartUtc.ToLocalTime().ToString("h:mm tt"),
                    DisplayName = s.DisplayName,
                    ProcessName = s.AppKey,
                    Category = s.Category,
                    CategoryColor = GetCategoryColor(s.Category),
                    DurationFormatted = FormatDuration(s.Duration),
                    CloseReasonText = isFirst ? "Observability Active" : FormatCloseReason(s.CloseReason),
                    IsLiveNow = isFirst
                });
                isFirst = false;
            }
        }
        else
        {
            DateTime start = SelectedDate.Date.ToUniversalTime();
            DateTime end = start.AddDays(1);
            var liveSessions = _repository.GetSessionsAsync(start, end).GetAwaiter().GetResult()
                .OrderByDescending(s => s.StartUtc)
                .ToList();

            // Prepend in-flight active session if viewing today
            if (SelectedDate.Date == DateTime.Today &&
                _trackingCoordinator.CurrentState == TrackingState.Running &&
                _trackingCoordinator.CurrentApp != null)
            {
                var curApp = _trackingCoordinator.CurrentApp;
                var curDuration = _trackingCoordinator.CurrentSessionDuration;
                DateTime curStart = DateTime.UtcNow - curDuration;

                Sessions.Add(new TimelineItemViewModel
                {
                    FormattedTime = curStart.ToLocalTime().ToString("h:mm tt"),
                    DisplayName = curApp.DisplayName,
                    ProcessName = curApp.AppKey,
                    Category = curApp.Category,
                    CategoryColor = GetCategoryColor(curApp.Category),
                    DurationFormatted = FormatDuration(curDuration),
                    CloseReasonText = "Observability Active",
                    IsLiveNow = true
                });
            }

            foreach (var s in liveSessions)
            {
                Sessions.Add(new TimelineItemViewModel
                {
                    FormattedTime = s.StartUtc.ToLocalTime().ToString("h:mm tt"),
                    DisplayName = s.DisplayName,
                    ProcessName = s.AppKey,
                    Category = s.Category,
                    CategoryColor = GetCategoryColor(s.Category),
                    DurationFormatted = FormatDuration(s.Duration),
                    CloseReasonText = FormatCloseReason(s.CloseReason),
                    IsLiveNow = false
                });
            }

            TotalSessions = Sessions.Count;
            SessionCountText = $"{TotalSessions} Sessions Logged Today";
        }
    }

    private static string FormatDuration(TimeSpan span)
    {
        if (span.TotalHours >= 1)
            return $"{(int)span.TotalHours}h {span.Minutes:D2}m {span.Seconds:D2}s";
        if (span.TotalMinutes >= 1)
            return $"{span.Minutes}m {span.Seconds:D2}s";
        return $"{Math.Max(0, span.Seconds)}s";
    }

    private static string FormatCloseReason(SessionCloseReason reason) => reason switch
    {
        SessionCloseReason.AppSwitch => "Switched focus",
        SessionCloseReason.Idle => "Idle timeout",
        SessionCloseReason.Lock => "Workstation locked",
        SessionCloseReason.Sleep => "System sleep",
        SessionCloseReason.TrackingDisabled => "Tracking paused",
        _ => "Focus change"
    };

    private static string GetCategoryColor(string category) => category switch
    {
        "Development" => "#0078D4",
        "Communication" => "#107C41",
        "Browser" => "#8764B8",
        "Productivity" => "#008272",
        "Media" => "#D83B01",
        "System" => "#69797E",
        _ => "#5C2D91"
    };

    public void Dispose()
    {
        _liveTimer?.Stop();
        _trackingCoordinator.SessionRecorded -= OnSessionRecorded;
        _trackingCoordinator.ForegroundAppChanged -= OnForegroundAppChanged;
    }
}
