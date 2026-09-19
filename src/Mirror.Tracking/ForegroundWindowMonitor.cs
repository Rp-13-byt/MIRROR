using System.Runtime.InteropServices;
using Mirror.Core.Interfaces;

namespace Mirror.Tracking;

public class ForegroundWindowMonitor : IForegroundWindowMonitor
{
    private readonly IProcessResolver _processResolver;
    private readonly Win32Api.WinEventDelegate _winEventDelegate;
    private nint _hookHandle = nint.Zero;
    private nint _lastHwnd = nint.Zero;
    private readonly Timer _watchdogTimer;
    private readonly object _syncLock = new();

    public event EventHandler<ForegroundWindowInfo>? ForegroundChanged;
    public bool IsRunning { get; private set; }

    public ForegroundWindowMonitor(IProcessResolver processResolver)
    {
        _processResolver = processResolver;
        _winEventDelegate = OnWinEvent;
        // Watchdog heartbeat runs every 1000ms
        _watchdogTimer = new Timer(WatchdogCheck, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (_syncLock)
        {
            if (IsRunning) return;

            _hookHandle = Win32Api.SetWinEventHook(
                Win32Api.EVENT_SYSTEM_FOREGROUND,
                Win32Api.EVENT_SYSTEM_FOREGROUND,
                nint.Zero,
                _winEventDelegate,
                0,
                0,
                Win32Api.WINEVENT_OUTOFCONTEXT | Win32Api.WINEVENT_SKIPOWNPROCESS);

            _watchdogTimer.Change(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            IsRunning = true;

            // Trigger initial foreground window detection
            CheckCurrentForegroundWindow();
        }
    }

    public void Stop()
    {
        lock (_syncLock)
        {
            if (!IsRunning) return;

            _watchdogTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_hookHandle != nint.Zero)
            {
                Win32Api.UnhookWinEvent(_hookHandle);
                _hookHandle = nint.Zero;
            }

            IsRunning = false;
        }
    }

    private void OnWinEvent(
        nint hWinEventHook,
        uint eventType,
        nint hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        if (eventType == Win32Api.EVENT_SYSTEM_FOREGROUND && hwnd != nint.Zero)
        {
            ProcessWindow(hwnd);
        }
    }

    private void WatchdogCheck(object? state)
    {
        if (!IsRunning) return;
        CheckCurrentForegroundWindow();
    }

    private void CheckCurrentForegroundWindow()
    {
        nint fg = Win32Api.GetForegroundWindow();
        if (fg != nint.Zero)
        {
            ProcessWindow(fg);
        }
    }

    private void ProcessWindow(nint hwnd)
    {
        lock (_syncLock)
        {
            if (hwnd == _lastHwnd) return;
            _lastHwnd = hwnd;

            Win32Api.GetWindowThreadProcessId(hwnd, out uint pid);
            int processId = (int)pid;
            string processName = _processResolver.ResolveProcessName(hwnd, processId);

            var info = new ForegroundWindowInfo(hwnd, processId, processName, DateTime.UtcNow);
            ForegroundChanged?.Invoke(this, info);
        }
    }

    public ValueTask DisposeAsync()
    {
        Stop();
        _watchdogTimer.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
