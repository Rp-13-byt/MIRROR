using Mirror.Core.Domain;
using Mirror.Core.Models;

namespace Mirror.Core.Interfaces;

public record ForegroundWindowInfo(nint Handle, int ProcessId, string ProcessName, DateTime TimestampUtc);

public interface IForegroundWindowMonitor : IAsyncDisposable
{
    event EventHandler<ForegroundWindowInfo>? ForegroundChanged;
    bool IsRunning { get; }
    void Start();
    void Stop();
}

public interface IProcessResolver
{
    string ResolveProcessName(nint hwnd, int processId);
}

public interface IApplicationIdentityResolver
{
    AppIdentity ResolveIdentity(string processName);
    void SetCategoryOverride(string appKey, string category);
    void SetExcluded(string appKey, bool excluded);
    IReadOnlyDictionary<string, string> GetCategoryOverrides();
    IReadOnlySet<string> GetExcludedApps();
}

public interface IIdleDetector
{
    TimeSpan GetIdleDuration();
    bool IsIdle(TimeSpan threshold);
}

public interface ISessionBuilder
{
    ActivitySession? CurrentSession { get; }
    event EventHandler<ActivitySession>? SessionCompleted;
    event EventHandler<IdlePeriod>? IdlePeriodCompleted;
    event EventHandler<AppSwitchEvent>? AppSwitched;

    void OnForegroundAppChanged(AppIdentity identity, DateTime timestampUtc);
    void OnUserIdleStateChanged(bool isIdle, DateTime timestampUtc);
    void OnSessionLockStateChanged(bool isLocked, DateTime timestampUtc);
    void OnSystemSleepStateChanged(bool isSleeping, DateTime timestampUtc);
    void OnTrackingDisabled(DateTime timestampUtc);
    ActivitySession? ForceCloseCurrentSession(SessionCloseReason reason, DateTime timestampUtc);
}

public interface ITrackingCoordinator
{
    TrackingState CurrentState { get; }
    AppIdentity? CurrentApp { get; }
    TimeSpan CurrentSessionDuration { get; }
    event EventHandler<TrackingState>? StateChanged;
    event EventHandler<ActivitySession>? SessionRecorded;
    event EventHandler<PatternEvent>? PatternDetected;
    event EventHandler<AppIdentity?>? ForegroundAppChanged;

    void Start();
    void Stop();
    void Pause(TimeSpan? duration = null);
    void Resume();

    TrackingMode TrackingMode { get; }
    IReadOnlySet<string> SelectedTrackedApps { get; }
    IReadOnlySet<string> SelectedTrackedCategories { get; }
    void SetTrackingMode(TrackingMode mode, IEnumerable<string>? selectedApps = null, IEnumerable<string>? selectedCategories = null);
}
