using System.Reflection;

namespace Mirror.Security.Privacy;

public class ForbiddenFieldGuard
{
    private static readonly HashSet<string> ForbiddenTerms = new(StringComparer.OrdinalIgnoreCase)
    {
        "WindowTitle",
        "WindowText",
        "Title",
        "Url",
        "Uri",
        "BrowserUrl",
        "Keystroke",
        "Keystrokes",
        "KeySequence",
        "TypedText",
        "Clipboard",
        "ClipboardData",
        "ClipboardText",
        "ScreenCapture",
        "Screenshot",
        "ScreenBuffer",
        "Webcam",
        "Camera",
        "Microphone",
        "AudioStream",
        "DocumentPath",
        "FilePath",
        "CommandLine",
        "Arguments",
        "ProcessArguments",
        "UserContent"
    };

    public bool IsForbidden(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;
        return ForbiddenTerms.Contains(fieldName) ||
               ForbiddenTerms.Any(term => fieldName.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public void AssertNoForbiddenFields(object entity)
    {
        if (entity == null) return;
        var type = entity.GetType();
        var violations = ScanType(type);
        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                $"Privacy Enforcement Violation: Type '{type.FullName}' exposes strictly forbidden privacy field(s): {string.Join(", ", violations)}");
        }
    }

    public IReadOnlyList<string> ScanType(Type type)
    {
        var violations = new List<string>();
        if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false) ||
            (type.FullName != null && type.FullName.Contains('<')))
        {
            return violations;
        }

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (IsForbidden(prop.Name))
            {
                violations.Add(prop.Name);
            }
        }
        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.Name.StartsWith("<") && field.Name.Contains("k__BackingField")) continue;
            if (IsForbidden(field.Name))
            {
                violations.Add(field.Name);
            }
        }
        return violations;
    }
}
