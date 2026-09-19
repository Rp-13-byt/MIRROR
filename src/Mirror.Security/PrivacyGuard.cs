using System.Reflection;
using Mirror.Core.Interfaces;

namespace Mirror.Security;

public class PrivacyViolationException : InvalidOperationException
{
    public PrivacyViolationException(string message) : base(message) { }
}

public class PrivacyGuard : IPrivacyGuard
{
    private static readonly HashSet<string> ForbiddenPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowTitle",
        "Title",
        "WindowName",
        "Url",
        "URL",
        "Uri",
        "BrowserHistory",
        "TabTitle",
        "Keystrokes",
        "Keystroke",
        "TypedText",
        "Password",
        "Clipboard",
        "ClipboardText",
        "Screenshot",
        "ScreenshotPath",
        "ScreenCapture",
        "Webcam",
        "Camera",
        "Microphone",
        "Audio",
        "DocumentName",
        "DocTitle",
        "FilePath",
        "CommandLine",
        "Arguments"
    };

    public bool IsFieldForbidden(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;
        return ForbiddenPropertyNames.Contains(fieldName);
    }

    public void ValidateSafeEntity<T>(T entity)
    {
        if (entity == null) return;
        var violations = ScanObjectForViolations(entity);
        if (violations.Count > 0)
        {
            throw new PrivacyViolationException(
                $"Privacy violation detected in {typeof(T).Name}: Forbidden fields found [{string.Join(", ", violations)}]. " +
                "Mirror strictly prohibits recording keystrokes, window titles, URLs, file contents, or screen captures.");
        }
    }

    public IReadOnlyList<string> ScanObjectForViolations(object obj)
    {
        var violations = new List<string>();
        if (obj == null) return violations;

        var type = obj.GetType();
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var p in props)
        {
            if (IsFieldForbidden(p.Name))
            {
                violations.Add(p.Name);
            }
        }

        return violations;
    }

    public static string SanitizeProcessName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return "Unknown";

        // Extract just the file name without directories
        string clean = Path.GetFileName(rawName).Trim();

        // Strip known executable extensions
        if (clean.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            clean = clean[..^4];
        }

        return clean.Length > 0 ? clean : "Unknown";
    }
}
