using Microsoft.Extensions.Logging;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Tracking;

public class TrackingCoordinator : ITrackingCoordinator, IAsyncDisposable
{
    private readonly IForegroundWindowMonitor _monitor;
    private readonly IApplicationIdentityResolver _identityResolver;
    private readonly IIdleDetector _idleDetector;
    private readonly ISessionBuilder _sessionBuilder;
    private readonly IMirrorRepository _repository;
    private readonly IPrivacyEnforcementGate? _privacyGate;
    private readonly IPatternDetector? _patternDetector;
    private readonly IPatternFusionEngine? _fusionEngine;
    private readonly IInferenceBackendManager? _inferenceBackend;
    private readonly IFeatureExtractor? _featureExtractor;
    private readonly ILogger<TrackingCoordinator>? _logger;

    private readonly Timer _idlePollingTimer;
    private readonly object _stateLock = new();

    private TrackingState _currentState = TrackingState.Stopped;
    private AppIdentity? _currentApp;
    private DateTime? _pauseUntilUtc;
    private TimeSpan _idleThreshold = TimeSpan.FromSeconds(120);

    private TrackingMode _trackingMode = TrackingMode.TrackAll;
    private readonly HashSet<string> _selectedTrackedApps = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectedTrackedCategories = new(StringComparer.OrdinalIgnoreCase);

    public TrackingMode TrackingMode => _trackingMode;
    public IReadOnlySet<string> SelectedTrackedApps => _selectedTrackedApps;
    public IReadOnlySet<string> SelectedTrackedCategories => _selectedTrackedCategories;

    public void SetTrackingMode(TrackingMode mode, IEnumerable<string>? selectedApps = null, IEnumerable<string>? selectedCategories = null)
    {
        lock (_stateLock)
        {
            _trackingMode = mode;
            _selectedTrackedApps.Clear();
            if (selectedApps != null)
            {
                foreach (var app in selectedApps) _selectedTrackedApps.Add(app);
            }
            _selectedTrackedCategories.Clear();
            if (selectedCategories != null)
            {
                foreach (var cat in selectedCategories) _selectedTrackedCategories.Add(cat);
            }
        }
    }

    public TrackingState CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                if (_pauseUntilUtc.HasValue && DateTime.UtcNow < _pauseUntilUtc.Value)
                {
                    return TrackingState.Paused;
                }
                return _currentState;
            }
        }
        private set
        {
            lock (_stateLock)
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    StateChanged?.Invoke(this, _currentState);
                }
            }
        }
    }

    public AppIdentity? CurrentApp
    {
        get
        {
            lock (_stateLock)
            {
                return _currentApp;
            }
        }
    }

    public TimeSpan CurrentSessionDuration
    {
        get
        {
            var session = _sessionBuilder.CurrentSession;
            if (session != null && session.StartUtc != default)
            {
                var span = DateTime.UtcNow - session.StartUtc;
                return span > TimeSpan.Zero ? span : TimeSpan.Zero;
            }
            return TimeSpan.Zero;
        }
    }

    public event EventHandler<TrackingState>? StateChanged;
    public event EventHandler<ActivitySession>? SessionRecorded;
    public event EventHandler<PatternEvent>? PatternDetected;
    public event EventHandler<AppIdentity?>? ForegroundAppChanged;

    public TrackingCoordinator(
        IForegroundWindowMonitor monitor,
        IApplicationIdentityResolver identityResolver,
        IIdleDetector idleDetector,
        ISessionBuilder sessionBuilder,
        IMirrorRepository repository,
        IPrivacyEnforcementGate? privacyGate = null,
        IPatternDetector? patternDetector = null,
        IPatternFusionEngine? fusionEngine = null,
        IInferenceBackendManager? inferenceBackend = null,
        IFeatureExtractor? featureExtractor = null,
        ILogger<TrackingCoordinator>? logger = null)
    {
        _monitor = monitor;
        _identityResolver = identityResolver;
        _idleDetector = idleDetector;
        _sessionBuilder = sessionBuilder;
        _repository = repository;
        _privacyGate = privacyGate;
        _patternDetector = patternDetector;
        _fusionEngine = fusionEngine;
        _inferenceBackend = inferenceBackend;
        _featureExtractor = featureExtractor;
        _logger = logger;

        _monitor.ForegroundChanged += OnForegroundChanged;
        _sessionBuilder.SessionCompleted += OnSessionCompleted;
        _sessionBuilder.IdlePeriodCompleted += OnIdlePeriodCompleted;
        _sessionBuilder.AppSwitched += OnAppSwitched;

        // Poll idle state every 2 seconds
        _idlePollingTimer = new Timer(CheckIdleState, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (_stateLock)
        {
            if (_currentState == TrackingState.Running) return;

            _pauseUntilUtc = null;
            _monitor.Start();
            _idlePollingTimer.Change(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
            CurrentState = TrackingState.Running;
            _logger?.LogInformation("TrackingCoordinator started.");
        }
    }

    public void Stop()
    {
        lock (_stateLock)
        {
            if (_currentState == TrackingState.Stopped) return;

            _idlePollingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _monitor.Stop();
            _sessionBuilder.OnTrackingDisabled(DateTime.UtcNow);
            CurrentState = TrackingState.Stopped;
            _currentApp = null;
            ForegroundAppChanged?.Invoke(this, null);
            _logger?.LogInformation("TrackingCoordinator stopped.");
        }
    }

    public void Pause(TimeSpan? duration = null)
    {
        lock (_stateLock)
        {
            _pauseUntilUtc = duration.HasValue ? DateTime.UtcNow + duration.Value : DateTime.MaxValue;
            _sessionBuilder.OnTrackingDisabled(DateTime.UtcNow);
            CurrentState = TrackingState.Paused;
            _currentApp = null;
            ForegroundAppChanged?.Invoke(this, null);
            _logger?.LogInformation("TrackingCoordinator paused.");
        }
    }

    public void Resume()
    {
        lock (_stateLock)
        {
            _pauseUntilUtc = null;
            CurrentState = TrackingState.Running;
            _logger?.LogInformation("TrackingCoordinator resumed.");
        }
    }

    public void SetIdleThreshold(TimeSpan threshold)
    {
        _idleThreshold = threshold;
    }

    private void OnForegroundChanged(object? sender, ForegroundWindowInfo info)
    {
        if (CurrentState != TrackingState.Running) return;

        // Route through Privacy Enforcement Gate if available, or fallback to identity resolver
        CanonicalActivityEvent? canonical = null;
        if (_privacyGate != null)
        {
            canonical = _privacyGate.SanitizeAndFilter(info.ProcessName, info.TimestampUtc);
        }
        else
        {
            var rawId = _identityResolver.ResolveIdentity(info.ProcessName);
            if (!rawId.IsExcluded)
            {
                canonical = new CanonicalActivityEvent(rawId.AppKey, rawId.DisplayName, rawId.Category, info.TimestampUtc);
            }
        }

        if (canonical == null)
        {
            // Excluded app: ensure sessionizer receives excluded identity so no session is recorded
            var excludedIdentity = new AppIdentity("excluded", "Excluded", "Excluded", IsExcluded: true);
            lock (_stateLock) { _currentApp = null; }
            ForegroundAppChanged?.Invoke(this, null);
            _sessionBuilder.OnForegroundAppChanged(excludedIdentity, info.TimestampUtc);
            return;
        }

        // Check tracking mode filtering
        lock (_stateLock)
        {
            if (_trackingMode == TrackingMode.TrackSelectedApps && !_selectedTrackedApps.Contains(canonical.AppKey))
            {
                var untracked = new AppIdentity(canonical.AppKey, canonical.DisplayName, canonical.Category, IsExcluded: true);
                _currentApp = null;
                ForegroundAppChanged?.Invoke(this, null);
                _sessionBuilder.OnForegroundAppChanged(untracked, canonical.TimestampUtc);
                return;
            }

            if (_trackingMode == TrackingMode.TrackSelectedCategories && !_selectedTrackedCategories.Contains(canonical.Category))
            {
                var untracked = new AppIdentity(canonical.AppKey, canonical.DisplayName, canonical.Category, IsExcluded: true);
                _currentApp = null;
                ForegroundAppChanged?.Invoke(this, null);
                _sessionBuilder.OnForegroundAppChanged(untracked, canonical.TimestampUtc);
                return;
            }
        }

        var identity = new AppIdentity(canonical.AppKey, canonical.DisplayName, canonical.Category, IsExcluded: false);
        lock (_stateLock)
        {
            _currentApp = identity;
        }

        ForegroundAppChanged?.Invoke(this, identity);
        _sessionBuilder.OnForegroundAppChanged(identity, canonical.TimestampUtc);
    }

    private void CheckIdleState(object? state)
    {
        if (CurrentState != TrackingState.Running) return;

        bool isIdle = _idleDetector.IsIdle(_idleThreshold);
        _sessionBuilder.OnUserIdleStateChanged(isIdle, DateTime.UtcNow);

        lock (_stateLock)
        {
            if (isIdle && _currentState == TrackingState.Running)
            {
                CurrentState = TrackingState.Idle;
            }
            else if (!isIdle && _currentState == TrackingState.Idle)
            {
                CurrentState = TrackingState.Running;
            }
        }
    }

    private async void OnSessionCompleted(object? sender, ActivitySession session)
    {
        try
        {
            await _repository.InsertSessionAsync(session);
            SessionRecorded?.Invoke(this, session);

            if (_patternDetector != null)
            {
                await EvaluateRecentPatternsAsync(session.EndUtc);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist activity session.");
        }
    }

    private async Task EvaluateRecentPatternsAsync(DateTime evalTimeUtc)
    {
        try
        {
            var recentSessions = await _repository.GetRecentSessionsAsync(TimeSpan.FromHours(1));
            var recentSwitches = await _repository.GetRecentSwitchesAsync(TimeSpan.FromHours(1));
            var settings = await _repository.LoadSettingsAsync();

            var detected = _patternDetector!.EvaluateRules(recentSessions, recentSwitches, settings, null, evalTimeUtc);
            foreach (var rulePattern in detected)
            {
                PatternEvent finalPattern = rulePattern;
                if (_fusionEngine != null && _inferenceBackend != null && _featureExtractor != null && _inferenceBackend.IsInitialized)
                {
                    try
                    {
                        var sequence = _featureExtractor.ExtractFeatureSequence(recentSessions, recentSwitches, evalTimeUtc);
                        var mlResult = await _inferenceBackend.PredictAsync(sequence);
                        var fused = _fusionEngine.FuseSignals(rulePattern, mlResult);
                        if (fused != null) finalPattern = fused;
                    }
                    catch
                    {
                        // Fallback safely to rule output
                    }
                }

                await _repository.InsertPatternEventAsync(finalPattern);
                PatternDetected?.Invoke(this, finalPattern);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Pattern evaluation encountered an error.");
        }
    }

    private async void OnIdlePeriodCompleted(object? sender, IdlePeriod idle)
    {
        try
        {
            await _repository.InsertIdlePeriodAsync(idle);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist idle period.");
        }
    }

    private async void OnAppSwitched(object? sender, AppSwitchEvent sw)
    {
        try
        {
            await _repository.InsertAppSwitchAsync(sw);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to persist app switch event.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        await _monitor.DisposeAsync();
        await _idlePollingTimer.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
