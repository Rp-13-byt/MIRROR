using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Security.Privacy;

public class ActivitySanitizer
{
    private readonly IApplicationIdentityResolver _identityResolver;
    private readonly ForbiddenFieldGuard _forbiddenGuard;

    public ActivitySanitizer(IApplicationIdentityResolver identityResolver, ForbiddenFieldGuard forbiddenGuard)
    {
        _identityResolver = identityResolver;
        _forbiddenGuard = forbiddenGuard;
    }

    public CanonicalActivityEvent? Sanitize(string rawProcessName, DateTime timestampUtc)
    {
        if (string.IsNullOrWhiteSpace(rawProcessName))
        {
            return null;
        }

        // Clean process name: strip path, strip extension, keep only executable base name
        string cleanName = Path.GetFileNameWithoutExtension(rawProcessName).Trim().ToLowerInvariant();

        var identity = _identityResolver.ResolveIdentity(cleanName);
        if (identity.IsExcluded)
        {
            return null;
        }

        // Build canonical activity event
        var canonical = new CanonicalActivityEvent(
            identity.AppKey,
            identity.DisplayName,
            identity.Category,
            timestampUtc == default ? DateTime.UtcNow : timestampUtc
        );

        // Architectural verification: ensure canonical event adheres to zero-forbidden-fields policy
        _forbiddenGuard.AssertNoForbiddenFields(canonical);

        return canonical;
    }
}
