using Mirror.Core.Interfaces;
using Mirror.Core.Models;

namespace Mirror.Security.Privacy;

public class PrivacyEnforcementGate : IPrivacyEnforcementGate
{
    private readonly ActivitySanitizer _sanitizer;
    private readonly ForbiddenFieldGuard _forbiddenGuard;

    public PrivacyEnforcementGate(ActivitySanitizer sanitizer, ForbiddenFieldGuard forbiddenGuard)
    {
        _sanitizer = sanitizer;
        _forbiddenGuard = forbiddenGuard;
    }

    public CanonicalActivityEvent? SanitizeAndFilter(string processName, DateTime timestampUtc)
    {
        return _sanitizer.Sanitize(processName, timestampUtc);
    }

    public bool IsAllowedField(string fieldName)
    {
        return AllowedActivityFields.IsFieldPermitted(fieldName) && !_forbiddenGuard.IsForbidden(fieldName);
    }

    public void AssertForbiddenFieldAbsence(object payload)
    {
        _forbiddenGuard.AssertNoForbiddenFields(payload);
    }
}
