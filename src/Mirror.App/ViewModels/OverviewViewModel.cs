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

public sealed partial class CategoryDisplayItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string DurationFormatted { get; set; } = string.Empty;
    public double Percentage { get; set; }
    public string ColorHex { get; set; } = "#0078D4";
}

public sealed partial class AppDisplayItem : ObservableObject
{
    public string ProcessName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string DurationFormatted { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

public sealed partial class PatternDisplayItem : ObservableObject
{
    public string PatternType { get; set; } = string.Empty;
    public string ConfidenceText { get; set; } = string.Empty;
    public string FormattedTimestamp { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string EvidenceSummary { get; set; } = string.Empty;
    public string ObservedValue { get; set; } = string.Empty;
    public string BaselineValue { get; set; } = string.Empty;
    public string ColorHex { get; set; } = "#0078D4";
}

public sealed partial class OverviewViewModel : ObservableObject, IDisposable
{
    private readonly IMirrorRepository _repository;
    private readonly IDemoDataService _demoService;
    private readonly ITrackingCoordinator _trackingCoordinator;
    private readonly Mirror.Voice.LocalVoiceNarrator _narrator;
    private readonly DispatcherQueue? _dispatcherQueue;
    private DispatcherQueueTimer? _liveTimer;

    [ObservableProperty]
    private string _currentAppName = "Observing Windows";

    [ObservableProperty]
    private string _currentProcessName = "Waiting for window focus...";

    [ObservableProperty]
    private string _currentCategory = "General";

    [ObservableProperty]
    private string _currentSessionElapsed = "00:00:00";

    [ObservableProperty]
    private bool _hasActiveSession = false;

    [ObservableProperty]
    private string _totalActiveTime = "0h 0m";

    [ObservableProperty]
    private string _totalIdleTime = "0h 0m";

    [ObservableProperty]
    private int _contextSwitches = 0;

    [ObservableProperty]
    private string _primaryCategory = "Observing Activity";

    [ObservableProperty]
    private bool _isDemoActive = true;

    // Innovation: Flow-State Recognition
    [ObservableProperty]
    private bool _hasFlowState = false;

    [ObservableProperty]
    private string _flowTitle = "Flow State Achieved";

    [ObservableProperty]
    private string _flowDetails = "You spent 74 consecutive minutes in Visual Studio Code without interruption. That's your longest focused stretch this week.";

    [ObservableProperty]
    private string _flowBadge = "74m deep work";

    // Innovation: Voice Narration
    [ObservableProperty]
    private bool _isNarrating = false;

    [ObservableProperty]
    private string _narrationButtonText = "Listen to Today's Summary";

    [ObservableProperty]
    private string _narrationIcon = "\uE767";

    // Focus Session Controls
    [ObservableProperty]
    private bool _isFocusSessionActive = false;

    [ObservableProperty]
    private string _focusRemainingFormatted = "25:00";

    [ObservableProperty]
    private string _focusContextName = "Deep Work";

    [ObservableProperty]
    private int _focusPlannedMinutes = 25;

    [ObservableProperty]
    private int _focusSessionSwitches = 0;

    [ObservableProperty]
    private string _focusCompletionMessage = string.Empty;

    [ObservableProperty]
    private bool _hasFocusCompletionMessage = false;

    public Microsoft.UI.Xaml.Visibility FocusCompletionVisibility => HasFocusCompletionMessage ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility FocusActiveVisibility => IsFocusSessionActive ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
    public Microsoft.UI.Xaml.Visibility FocusInactiveVisibility => IsFocusSessionActive ? Microsoft.UI.Xaml.Visibility.Collapsed : Microsoft.UI.Xaml.Visibility.Visible;

    partial void OnIsFocusSessionActiveChanged(bool value)
    {
        OnPropertyChanged(nameof(FocusActiveVisibility));
        OnPropertyChanged(nameof(FocusInactiveVisibility));
    }

    partial void OnHasFocusCompletionMessageChanged(bool value)
    {
        OnPropertyChanged(nameof(FocusCompletionVisibility));
    }

    private DateTime _focusStartUtc;
    private DateTime _focusEndUtc;
    private readonly System.Collections.Generic.HashSet<string> _focusAppsUsed = new(StringComparer.OrdinalIgnoreCase);

    public ObservableCollection<CategoryDisplayItem> Categories { get; } = new();
    public ObservableCollection<AppDisplayItem> TopApplications { get; } = new();
    public ObservableCollection<PatternDisplayItem> RecentPatterns { get; } = new();

    private int _completedActiveSecondsToday = 0;
    private int _completedIdleSecondsToday = 0;
    private int _completedSwitchesToday = 0;
    private int _demoTickCounter = 0;

    public OverviewViewModel(
        IMirrorRepository repository,
        IDemoDataService demoService,
        ITrackingCoordinator trackingCoordinator,
        Mirror.Voice.LocalVoiceNarrator narrator)
    {
        _repository = repository;
        _demoService = demoService;
        _trackingCoordinator = trackingCoordinator;
        _narrator = narrator;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _narrator.PlaybackStateChanged += OnPlaybackStateChanged;
        _trackingCoordinator.SessionRecorded += OnSessionRecorded;
        _trackingCoordinator.ForegroundAppChanged += OnForegroundAppChanged;

        StartLiveTimer();
        LoadData();
    }

    private void OnPlaybackStateChanged(bool isPlaying)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsNarrating = isPlaying;
            NarrationButtonText = isPlaying ? "Stop Audio Summary" : "Listen to Today's Summary";
            NarrationIcon = isPlaying ? "\uE71A" : "\uE767";
        });
    }

    private void StartLiveTimer()
    {
        if (_dispatcherQueue != null)
        {
            _liveTimer = _dispatcherQueue.CreateTimer();
            _liveTimer.Interval = TimeSpan.FromSeconds(1);
            _liveTimer.Tick += (s, e) => LiveTick();
            _liveTimer.Start();
        }
    }

    private void LiveTick()
    {
        if (IsDemoActive)
        {
            _demoTickCounter++;
            TimeSpan demoSpan = TimeSpan.FromHours(1).Add(TimeSpan.FromMinutes(42)).Add(TimeSpan.FromSeconds(_demoTickCounter));
            CurrentSessionElapsed = FormatLiveClock(demoSpan);
            return;
        }

        // Live mode tracking tick
        if (_trackingCoordinator.CurrentState == TrackingState.Running && _trackingCoordinator.CurrentApp != null)
        {
            HasActiveSession = true;
            CurrentAppName = _trackingCoordinator.CurrentApp.DisplayName;
            CurrentProcessName = _trackingCoordinator.CurrentApp.AppKey;
            CurrentCategory = _trackingCoordinator.CurrentApp.Category;

            var span = _trackingCoordinator.CurrentSessionDuration;
            CurrentSessionElapsed = FormatLiveClock(span);

            int totalSecs = _completedActiveSecondsToday + (int)span.TotalSeconds;
            TotalActiveTime = FormatTotalTime(totalSecs);
        }
        else
        {
            HasActiveSession = false;
            CurrentAppName = "Observing Windows";
            CurrentProcessName = "Waiting for window focus...";
            CurrentCategory = "System";
            CurrentSessionElapsed = "00:00:00";
            TotalActiveTime = FormatTotalTime(_completedActiveSecondsToday);
        }

        // Focus session countdown
        if (IsFocusSessionActive)
        {
            var remaining = _focusEndUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _ = CompleteFocusSessionAsync();
            }
            else
            {
                FocusRemainingFormatted = $"{(int)remaining.TotalMinutes:D2}:{remaining.Seconds:D2}";
                if (_trackingCoordinator.CurrentApp != null)
                {
                    _focusAppsUsed.Add(_trackingCoordinator.CurrentApp.AppKey);
                }
            }
        }
    }

    private void OnSessionRecorded(object? sender, ActivitySession session)
    {
        _dispatcherQueue?.TryEnqueue(LoadData);
    }

    private void OnForegroundAppChanged(object? sender, AppIdentity? identity)
    {
        if (IsFocusSessionActive)
        {
            FocusSessionSwitches++;
            if (identity != null)
            {
                _focusAppsUsed.Add(identity.AppKey);
            }
        }
        _dispatcherQueue?.TryEnqueue(LoadData);
    }

    [RelayCommand]
    public void StartFocusSession(string contextWithMinutes)
    {
        int minutes = 25;
        string name = "Deep Work";
        if (!string.IsNullOrWhiteSpace(contextWithMinutes) && contextWithMinutes.Contains(':'))
        {
            var parts = contextWithMinutes.Split(':');
            if (int.TryParse(parts[0], out int m)) minutes = m;
            if (parts.Length > 1) name = parts[1];
        }

        FocusPlannedMinutes = minutes;
        FocusContextName = name;
        _focusStartUtc = DateTime.UtcNow;
        _focusEndUtc = _focusStartUtc.AddMinutes(minutes);
        _focusAppsUsed.Clear();
        if (_trackingCoordinator.CurrentApp != null) _focusAppsUsed.Add(_trackingCoordinator.CurrentApp.AppKey);

        IsFocusSessionActive = true;
        HasFocusCompletionMessage = false;
        FocusSessionSwitches = 0;
        FocusRemainingFormatted = $"{minutes:D2}:00";
    }

    [RelayCommand]
    public async Task CompleteFocusSessionAsync()
    {
        if (!IsFocusSessionActive) return;
        IsFocusSessionActive = false;

        var actualDuration = DateTime.UtcNow - _focusStartUtc;
        int actualSeconds = Math.Max(1, (int)actualDuration.TotalSeconds);
        int switches = FocusSessionSwitches;
        int uniqueApps = Math.Max(1, _focusAppsUsed.Count);

        var session = new FocusSession
        {
            StartUtc = _focusStartUtc,
            EndUtc = DateTime.UtcNow,
            PlannedDurationSeconds = FocusPlannedMinutes * 60,
            ActualDurationSeconds = actualSeconds,
            ContextName = FocusContextName,
            SwitchCount = switches,
            UniqueAppCount = uniqueApps
        };

        try
        {
            await _repository.InsertFocusSessionAsync(session);
        }
        catch { }

        int minutesCompleted = Math.Max(1, (int)actualDuration.TotalMinutes);
        FocusCompletionMessage = $"Focus period complete: {minutesCompleted} minutes logged with {switches} switches across {uniqueApps} applications.";
        HasFocusCompletionMessage = true;
    }

    [RelayCommand]
    public void CancelFocusSession()
    {
        IsFocusSessionActive = false;
        FocusRemainingFormatted = "00:00";
        HasFocusCompletionMessage = false;
    }

    [RelayCommand]
    public void DismissFocusCompletion()
    {
        HasFocusCompletionMessage = false;
        FocusCompletionMessage = string.Empty;
    }

    [RelayCommand]
    public void LoadData()
    {
        IsDemoActive = _demoService.IsDemoModeActive;
        Categories.Clear();
        TopApplications.Clear();
        RecentPatterns.Clear();

        if (IsDemoActive)
        {
            HasActiveSession = true;
            CurrentAppName = "Visual Studio Code";
            CurrentProcessName = "code.exe";
            CurrentCategory = "Development";
            CurrentSessionElapsed = "01:42:18";

            var demoMetrics = _demoService.GetDemoTodayMetrics();
            int totalActiveMins = demoMetrics.ActiveSeconds / 60;
            int totalIdleMins = demoMetrics.IdleSeconds / 60;
            TotalActiveTime = $"{totalActiveMins / 60}h {totalActiveMins % 60}m";
            TotalIdleTime = $"{totalIdleMins / 60}h {totalIdleMins % 60}m";
            ContextSwitches = demoMetrics.SwitchCount;

            var sessions = _demoService.GetDemoTodaySessions();
            var catGroups = sessions.GroupBy(s => s.Category)
                .Select(g => new { Name = g.Key, Mins = g.Sum(s => s.Duration.TotalMinutes) })
                .OrderByDescending(x => x.Mins)
                .ToList();

            double totalMins = totalActiveMins > 0 ? totalActiveMins : 1;
            PrimaryCategory = catGroups.FirstOrDefault()?.Name ?? "Development";

            string[] palette = { "#0078D4", "#107C41", "#8764B8", "#D83B01", "#FFB900", "#00B7C3" };
            int idx = 0;
            foreach (var cat in catGroups)
            {
                double pct = Math.Round((cat.Mins / totalMins) * 100, 1);
                int hrs = (int)(cat.Mins / 60);
                int mins = (int)(cat.Mins % 60);
                Categories.Add(new CategoryDisplayItem
                {
                    Name = cat.Name,
                    DurationFormatted = $"{hrs}h {mins}m",
                    Percentage = pct,
                    ColorHex = palette[idx % palette.Length]
                });
                idx++;
            }

            var appGroups = sessions.GroupBy(s => s.DisplayName)
                .Select(g => new { App = g.Key, Mins = g.Sum(s => s.Duration.TotalMinutes) })
                .OrderByDescending(x => x.Mins)
                .Take(5);

            foreach (var app in appGroups)
            {
                double pct = Math.Round((app.Mins / totalMins) * 100, 1);
                int hrs = (int)(app.Mins / 60);
                int mins = (int)(app.Mins % 60);
                TopApplications.Add(new AppDisplayItem
                {
                    ProcessName = app.App,
                    ApplicationName = app.App,
                    DurationFormatted = $"{hrs}h {mins}m",
                    Percentage = pct
                });
            }

            foreach (var pat in _demoService.GetDemoDetectedPatterns().Take(3))
            {
                RecentPatterns.Add(new PatternDisplayItem
                {
                    PatternType = FormatPatternTitle(pat.PatternType),
                    ConfidenceText = $"{Math.Round(pat.ModelConfidence * 100)}% Match",
                    FormattedTimestamp = pat.DetectedUtc.ToLocalTime().ToString("h:mm tt"),
                    Explanation = pat.Explanation,
                    ColorHex = pat.ModelConfidence >= 0.8f ? "#0078D4" : "#107C41"
                });
            }

            // Demo flow state
            HasFlowState = true;
            FlowTitle = "Flow State Achieved";
            FlowDetails = "You spent 74 consecutive minutes in Visual Studio Code without interruption. That's your longest focused stretch this week.";
            FlowBadge = "74m deep work";
        }
        else
        {
            // Production live data queried directly from SQLite
            DateTime todayStartUtc = DateTime.Today.ToUniversalTime();
            DateTime nowUtc = DateTime.UtcNow.AddMinutes(1);

            var todayFlows = _repository.GetFlowStateSessionsAsync(todayStartUtc, nowUtc).GetAwaiter().GetResult();
            if (todayFlows.Count > 0)
            {
                var topFlow = todayFlows.OrderByDescending(f => f.DurationSeconds).First();
                int flowMins = topFlow.DurationSeconds / 60;
                HasFlowState = true;
                FlowTitle = "Flow State Achieved";
                FlowDetails = $"You spent {flowMins} consecutive minutes in {topFlow.DisplayName} without interruption. Zero context switches recorded.";
                FlowBadge = $"{flowMins}m deep work";
            }
            else
            {
                HasFlowState = false;
            }

            var todaySessions = _repository.GetSessionsAsync(todayStartUtc, nowUtc).GetAwaiter().GetResult();
            var todaySwitches = _repository.GetRecentSwitchesAsync(TimeSpan.FromHours(24)).GetAwaiter().GetResult();
            var todayIdles = _repository.GetIdlePeriodsAsync(todayStartUtc, nowUtc).GetAwaiter().GetResult();

            _completedActiveSecondsToday = todaySessions.Sum(s => s.ActiveSeconds);
            _completedIdleSecondsToday = todayIdles.Sum(i => i.DurationSeconds);
            _completedSwitchesToday = todaySwitches.Count > 0 ? todaySwitches.Count : todaySessions.Count;

            int curActiveSecs = 0;
            if (_trackingCoordinator.CurrentState == TrackingState.Running && _trackingCoordinator.CurrentApp != null)
            {
                HasActiveSession = true;
                CurrentAppName = _trackingCoordinator.CurrentApp.DisplayName;
                CurrentProcessName = _trackingCoordinator.CurrentApp.AppKey;
                CurrentCategory = _trackingCoordinator.CurrentApp.Category;
                var curSpan = _trackingCoordinator.CurrentSessionDuration;
                CurrentSessionElapsed = FormatLiveClock(curSpan);
                curActiveSecs = (int)curSpan.TotalSeconds;
            }
            else
            {
                HasActiveSession = false;
                CurrentAppName = "Observing Windows";
                CurrentProcessName = "Waiting for window focus...";
                CurrentCategory = "System";
                CurrentSessionElapsed = "00:00:00";
            }

            int totalActiveSecs = _completedActiveSecondsToday + curActiveSecs;
            TotalActiveTime = FormatTotalTime(totalActiveSecs);

            int idleMins = _completedIdleSecondsToday / 60;
            TotalIdleTime = idleMins >= 60 ? $"{idleMins / 60}h {idleMins % 60}m" : $"{idleMins}m";
            ContextSwitches = _completedSwitchesToday;

            // Categories
            var allActiveItems = todaySessions.Select(s => new { s.Category, Secs = s.ActiveSeconds }).ToList();
            if (HasActiveSession && _trackingCoordinator.CurrentApp != null)
            {
                allActiveItems.Add(new { Category = _trackingCoordinator.CurrentApp.Category, Secs = curActiveSecs });
            }

            var catGroups = allActiveItems.GroupBy(x => x.Category)
                .Select(g => new { Name = g.Key, Secs = g.Sum(x => x.Secs) })
                .OrderByDescending(x => x.Secs)
                .ToList();

            double totalSecsDbl = totalActiveSecs > 0 ? totalActiveSecs : 1;
            PrimaryCategory = catGroups.FirstOrDefault()?.Name ?? "Observing Activity";

            string[] palette = { "#0078D4", "#107C41", "#8764B8", "#D83B01", "#FFB900", "#00B7C3" };
            int idx = 0;
            foreach (var cat in catGroups)
            {
                double pct = Math.Round((cat.Secs / totalSecsDbl) * 100, 1);
                int mins = cat.Secs / 60;
                int hrs = mins / 60;
                int remMins = mins % 60;
                string durStr = hrs > 0 ? $"{hrs}h {remMins}m" : $"{cat.Secs}s";
                Categories.Add(new CategoryDisplayItem
                {
                    Name = cat.Name,
                    DurationFormatted = durStr,
                    Percentage = pct,
                    ColorHex = palette[idx % palette.Length]
                });
                idx++;
            }

            // Top Applications
            var appItems = todaySessions.Select(s => new { App = s.DisplayName, Proc = s.AppKey, Secs = s.ActiveSeconds }).ToList();
            if (HasActiveSession && _trackingCoordinator.CurrentApp != null)
            {
                appItems.Add(new { App = _trackingCoordinator.CurrentApp.DisplayName, Proc = _trackingCoordinator.CurrentApp.AppKey, Secs = curActiveSecs });
            }

            var appGroups = appItems.GroupBy(x => (x.App, x.Proc))
                .Select(g => new { App = g.Key.App, Proc = g.Key.Proc, Secs = g.Sum(x => x.Secs) })
                .OrderByDescending(x => x.Secs)
                .Take(5);

            foreach (var app in appGroups)
            {
                double pct = Math.Round((app.Secs / totalSecsDbl) * 100, 1);
                int mins = app.Secs / 60;
                int hrs = mins / 60;
                int remMins = mins % 60;
                TopApplications.Add(new AppDisplayItem
                {
                    ProcessName = app.Proc,
                    ApplicationName = app.App,
                    DurationFormatted = hrs > 0 ? $"{hrs}h {remMins}m" : $"{app.Secs}s",
                    Percentage = pct
                });
            }

            // Recent Patterns
            var patterns = _repository.GetPatternEventsAsync(todayStartUtc.AddDays(-1), nowUtc).GetAwaiter().GetResult();
            foreach (var pat in patterns.OrderByDescending(p => p.DetectedUtc).Take(3))
            {
                RecentPatterns.Add(new PatternDisplayItem
                {
                    PatternType = FormatPatternTitle(pat.PatternType),
                    ConfidenceText = $"{Math.Round(pat.ModelConfidence * 100)}% Match",
                    FormattedTimestamp = pat.DetectedUtc.ToLocalTime().ToString("h:mm tt"),
                    Explanation = pat.Explanation,
                    ColorHex = pat.ModelConfidence >= 0.8f ? "#0078D4" : "#107C41"
                });
            }
        }
    }

    [RelayCommand]
    private async Task ToggleNarrationAsync()
    {
        if (_narrator.IsSpeaking)
        {
            _narrator.Stop();
            IsNarrating = false;
            NarrationButtonText = "Listen to Today's Summary";
            NarrationIcon = "\uE767";
            return;
        }

        DailyMetrics? metrics = null;
        IReadOnlyList<FlowStateSession>? flows = null;
        IReadOnlyList<ActivitySession>? sessions = null;

        if (IsDemoActive)
        {
            metrics = _demoService.GetDemoTodayMetrics();
            sessions = _demoService.GetDemoTodaySessions();
            flows = new List<FlowStateSession>
            {
                new()
                {
                    AppKey = "vscode",
                    DisplayName = "Visual Studio Code",
                    StartUtc = DateTime.UtcNow.AddMinutes(-74),
                    EndUtc = DateTime.UtcNow,
                    DurationSeconds = 4440,
                    DistractionCount = 0
                }
            };
        }
        else
        {
            DateTime todayStartUtc = DateTime.Today.ToUniversalTime();
            DateTime nowUtc = DateTime.UtcNow.AddMinutes(1);
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            var range = await _repository.GetDailyMetricsRangeAsync(todayStr, todayStr);
            metrics = range.FirstOrDefault();
            flows = await _repository.GetFlowStateSessionsAsync(todayStartUtc, nowUtc);
            sessions = await _repository.GetSessionsAsync(todayStartUtc, nowUtc);
        }

        string script = Mirror.Voice.DailyScriptGenerator.GenerateScript(metrics, flows, sessions, DateTime.Now);
        _narrator.Speak(script);
        IsNarrating = true;
        NarrationButtonText = "Stop Audio Summary";
        NarrationIcon = "\uE71A";
    }

    private static string FormatLiveClock(TimeSpan span)
    {
        return $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
    }

    private static string FormatTotalTime(int totalSecs)
    {
        int hrs = totalSecs / 3600;
        int mins = (totalSecs % 3600) / 60;
        int secs = totalSecs % 60;
        if (hrs > 0)
            return $"{hrs}h {mins}m {secs}s";
        if (mins > 0)
            return $"{mins}m {secs}s";
        return $"{secs}s";
    }

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
        _liveTimer?.Stop();
        _narrator.PlaybackStateChanged -= OnPlaybackStateChanged;
        _trackingCoordinator.SessionRecorded -= OnSessionRecorded;
        _trackingCoordinator.ForegroundAppChanged -= OnForegroundAppChanged;
    }
}
