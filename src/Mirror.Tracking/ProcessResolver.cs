using System.Diagnostics;
using Mirror.Core.Interfaces;
using Mirror.Security;

namespace Mirror.Tracking;

public class ProcessResolver : IProcessResolver
{
    public string ResolveProcessName(nint hwnd, int processId)
    {
        if (hwnd == nint.Zero || processId <= 0) return "Unknown";

        try
        {
            using var proc = Process.GetProcessById(processId);
            string rawName = proc.ProcessName;
            return PrivacyGuard.SanitizeProcessName(rawName);
        }
        catch
        {
            return "Unknown";
        }
    }
}
