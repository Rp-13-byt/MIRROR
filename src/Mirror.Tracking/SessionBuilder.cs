using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Tracking;

public class SessionBuilder : ISessionBuilder
{
    private readonly object _lock = new();

    private ActivitySession? _currentSession;
    private AppIdentity? _currentApp;
    private DateTime _sessionStartTimeUtc;
    private DateTime _lastActiveCheckUtc;

    private bool _isUserIdle;
    private DateTime _idleStartTimeUtc;

    public ActivitySession? CurrentSession
    {
        get
        {
            lock (_lock)
            {
                return _currentSession;
            }
        }
    }

    public event EventHandler<ActivitySession>? SessionCompleted;
    public event EventHandler<IdlePeriod>? IdlePeriodCompleted;
    public event EventHandler<AppSwitchEvent>? AppSwitched;

    public void OnForegroundAppChanged(AppIdentity identity, DateTime timestampUtc)
    {
        lock (_lock)
        {
            if (_currentApp != null && _currentApp.AppKey == identity.AppKey)
            {
                // Same application, keep session active
                return;
            }

            // If an app switch happened, record switch event and close previous session
            if (_currentApp != null && !string.IsNullOrEmpty(_currentApp.AppKey))
            {
                AppSwitched?.Invoke(this, new AppSwitchEvent
                {
                    FromAppKey = _currentApp.AppKey,
                    ToAppKey = identity.AppKey,
                    TimestampUtc = timestampUtc
                });

                CloseCurrentSession(SessionCloseReason.AppSwitch, timestampUtc);
            }

            _currentApp = identity;

            // If app is excluded or user is idle, do not start active session
            if (identity.IsExcluded || _isUserIdle)
            {
                _currentSession = null;
                return;
            }

            _sessionStartTimeUtc = timestampUtc;
            _lastActiveCheckUtc = timestampUtc;
            _currentSession = new ActivitySession
            {
                AppKey = identity.AppKey,
                DisplayName = identity.DisplayName,
                Category = identity.Category,
                StartUtc = timestampUtc,
                EndUtc = timestampUtc,
                ActiveSeconds = 0,
                CloseReason = SessionCloseReason.AppSwitch
            };
        }
    }

    public void OnUserIdleStateChanged(bool isIdle, DateTime timestampUtc)
    {
        lock (_lock)
        {
            if (_isUserIdle == isIdle) return;
            _isUserIdle = isIdle;

            if (isIdle)
            {
                // User became idle: close current session
                _idleStartTimeUtc = timestampUtc;
                CloseCurrentSession(SessionCloseReason.Idle, timestampUtc);
            }
            else
            {
                // User became active again: close idle period
                if (_idleStartTimeUtc != default && timestampUtc > _idleStartTimeUtc)
                {
                    int idleDurationSec = (int)(timestampUtc - _idleStartTimeUtc).TotalSeconds;
                    if (idleDurationSec > 0)
                    {
                        var idlePeriod = new IdlePeriod
                        {
                            StartUtc = _idleStartTimeUtc,
                            EndUtc = timestampUtc,
                            DurationSeconds = idleDurationSec,
                            CreatedUtc = DateTime.UtcNow
                        };
                        IdlePeriodCompleted?.Invoke(this, idlePeriod);
                    }
                }

                // Resume session for current app if not excluded
                if (_currentApp != null && !_currentApp.IsExcluded)
                {
                    _sessionStartTimeUtc = timestampUtc;
                    _lastActiveCheckUtc = timestampUtc;
                    _currentSession = new ActivitySession
                    {
                        AppKey = _currentApp.AppKey,
                        DisplayName = _currentApp.DisplayName,
                        Category = _currentApp.Category,
                        StartUtc = timestampUtc,
                        EndUtc = timestampUtc,
                        ActiveSeconds = 0,
                        CloseReason = SessionCloseReason.AppSwitch
                    };
                }
            }
        }
    }

    public void OnSessionLockStateChanged(bool isLocked, DateTime timestampUtc)
    {
        lock (_lock)
        {
            if (isLocked)
            {
                CloseCurrentSession(SessionCloseReason.Lock, timestampUtc);
            }
            else
            {
                if (_currentApp != null && !_currentApp.IsExcluded && !_isUserIdle)
                {
                    _sessionStartTimeUtc = timestampUtc;
                    _lastActiveCheckUtc = timestampUtc;
                    _currentSession = new ActivitySession
                    {
                        AppKey = _currentApp.AppKey,
                        DisplayName = _currentApp.DisplayName,
                        Category = _currentApp.Category,
                        StartUtc = timestampUtc,
                        EndUtc = timestampUtc,
                        ActiveSeconds = 0,
                        CloseReason = SessionCloseReason.AppSwitch
                    };
                }
            }
        }
    }

    public void OnSystemSleepStateChanged(bool isSleeping, DateTime timestampUtc)
    {
        lock (_lock)
        {
            if (isSleeping)
            {
                CloseCurrentSession(SessionCloseReason.Sleep, timestampUtc);
            }
        }
    }

    public void OnTrackingDisabled(DateTime timestampUtc)
    {
        lock (_lock)
        {
            CloseCurrentSession(SessionCloseReason.TrackingDisabled, timestampUtc);
            _currentApp = null;
        }
    }

    public ActivitySession? ForceCloseCurrentSession(SessionCloseReason reason, DateTime timestampUtc)
    {
        lock (_lock)
        {
            return CloseCurrentSession(reason, timestampUtc);
        }
    }

    private ActivitySession? CloseCurrentSession(SessionCloseReason reason, DateTime timestampUtc)
    {
        if (_currentSession == null) return null;

        DateTime end = timestampUtc >= _sessionStartTimeUtc ? timestampUtc : _sessionStartTimeUtc;
        int activeSeconds = (int)(end - _sessionStartTimeUtc).TotalSeconds;

        // Only record sessions with non-zero duration
        var completed = _currentSession with
        {
            EndUtc = end,
            ActiveSeconds = Math.Max(0, activeSeconds),
            CloseReason = reason
        };

        _currentSession = null;

        if (completed.ActiveSeconds > 0)
        {
            SessionCompleted?.Invoke(this, completed);
        }

        return completed;
    }
}
