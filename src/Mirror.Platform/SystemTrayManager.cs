using Mirror.Core.Domain;
using Mirror.Core.Interfaces;

namespace Mirror.Platform;

public class SystemTrayManager
{
    private readonly ITrackingCoordinator _trackingCoordinator;

    public event EventHandler? RequestOpenWindow;
    public event EventHandler? RequestExit;

    public SystemTrayManager(ITrackingCoordinator trackingCoordinator)
    {
        _trackingCoordinator = trackingCoordinator;
    }

    public void OnOpenRequested()
    {
        RequestOpenWindow?.Invoke(this, EventArgs.Empty);
    }

    public void OnPauseRequested(TimeSpan? duration)
    {
        _trackingCoordinator.Pause(duration);
    }

    public void OnResumeRequested()
    {
        _trackingCoordinator.Resume();
    }

    public void OnExitRequested()
    {
        _trackingCoordinator.Stop();
        RequestExit?.Invoke(this, EventArgs.Empty);
    }

    public TrackingState GetCurrentStatus()
    {
        return _trackingCoordinator.CurrentState;
    }
}
