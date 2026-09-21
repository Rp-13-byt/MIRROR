using System.Reflection;
using Mirror.Core.Interfaces;

namespace Mirror.Security.Privacy;

public record PrivacyAuditReport(
    bool IsPassed,
    int CheckedRulesCount,
    IReadOnlyList<string> Violations,
    DateTime AuditTimestampUtc
);

public class PrivacyAuditService
{
    private readonly ForbiddenFieldGuard _forbiddenGuard;

    public PrivacyAuditService(ForbiddenFieldGuard forbiddenGuard)
    {
        _forbiddenGuard = forbiddenGuard;
    }

    public PrivacyAuditReport PerformAudit(IEnumerable<Assembly>? assembliesToScan = null)
    {
        var violations = new List<string>();
        int checkedRules = 0;

        assembliesToScan ??= AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.FullName != null && a.FullName.StartsWith("Mirror.") && !a.FullName.Contains(".Tests"));

        foreach (var assembly in assembliesToScan)
        {
            // Rule 1: Assembly References check
            checkedRules++;
            foreach (var refAsm in assembly.GetReferencedAssemblies())
            {
                if (refAsm.Name != null &&
                    (refAsm.Name.StartsWith("System.Net.Http", StringComparison.OrdinalIgnoreCase) ||
                     refAsm.Name.StartsWith("System.Net.Sockets", StringComparison.OrdinalIgnoreCase) ||
                     refAsm.Name.StartsWith("Microsoft.AspNetCore.SignalR", StringComparison.OrdinalIgnoreCase)))
                {
                    violations.Add($"Assembly '{assembly.GetName().Name}' references forbidden networking library: '{refAsm.Name}'");
                }
            }

            // Rule 2: Type property audit for forbidden terms
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).Cast<Type>().ToArray();
            }

            foreach (var type in types)
            {
                checkedRules++;
                var typeViolations = _forbiddenGuard.ScanType(type);
                foreach (var v in typeViolations)
                {
                    violations.Add($"Type '{type.FullName}' in assembly '{assembly.GetName().Name}' exposes forbidden property/field: '{v}'");
                }
            }
        }

        return new PrivacyAuditReport(
            violations.Count == 0,
            checkedRules,
            violations,
            DateTime.UtcNow
        );
    }
}
