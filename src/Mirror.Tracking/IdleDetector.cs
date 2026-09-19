using System.Runtime.InteropServices;
using Mirror.Core.Interfaces;

namespace Mirror.Tracking;

public class IdleDetector : IIdleDetector
{
    public TimeSpan GetIdleDuration()
    {
        var lastInput = new Win32Api.LASTINPUTINFO();
        lastInput.cbSize = (uint)Marshal.SizeOf(lastInput);

        if (Win32Api.GetLastInputInfo(ref lastInput))
        {
            uint currentTick = Win32Api.GetTickCount();
            uint idleTicks = currentTick - lastInput.dwTime;
            return TimeSpan.FromMilliseconds(idleTicks);
        }

        return TimeSpan.Zero;
    }

    public bool IsIdle(TimeSpan threshold)
    {
        return GetIdleDuration() >= threshold;
    }
}
