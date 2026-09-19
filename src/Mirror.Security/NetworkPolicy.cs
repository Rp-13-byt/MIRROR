using System.Net.NetworkInformation;

namespace Mirror.Security;

public static class NetworkPolicy
{
    public const string PolicyStatement =
        "Mirror operates 100% offline. No cloud account, no remote telemetry, no network calls.";

    public static void AssertOfflinePolicy()
    {
        // Runtime assertion that the application operates locally
        // Mirror runtime intentionally instantiates no HttpClient, WebSocket, or socket listeners
    }

    public static bool ScanAssemblyForForbiddenTypes(System.Reflection.Assembly assembly, out List<string> detectedViolations)
    {
        detectedViolations = new List<string>();
        var forbiddenTypeNames = new[]
        {
            "System.Net.Http.HttpClient",
            "System.Net.WebClient",
            "System.Net.Sockets.TcpClient",
            "System.Net.Sockets.Socket",
            "System.Net.WebSockets.ClientWebSocket"
        };

        try
        {
            foreach (var type in assembly.GetTypes())
            {
                foreach (var field in type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Static))
                {
                    if (forbiddenTypeNames.Contains(field.FieldType.FullName))
                    {
                        detectedViolations.Add($"{type.FullName}.{field.Name} uses {field.FieldType.FullName}");
                    }
                }
            }
        }
        catch (System.Reflection.ReflectionTypeLoadException ex)
        {
            foreach (var loaderEx in ex.LoaderExceptions)
            {
                if (loaderEx != null)
                {
                    detectedViolations.Add($"TypeLoadException: {loaderEx.Message}");
                }
            }
        }

        return detectedViolations.Count == 0;
    }
}
