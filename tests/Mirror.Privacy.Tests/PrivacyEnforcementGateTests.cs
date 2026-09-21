using System;
using System.IO;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror.Security.Privacy;
using Mirror.Tracking;
using Xunit;

namespace Mirror.Privacy.Tests;

public class PrivacyEnforcementGateTests
{
    private readonly ForbiddenFieldGuard _forbiddenGuard = new();
    private readonly ApplicationIdentityResolver _identityResolver = new();
    private readonly ActivitySanitizer _sanitizer;
    private readonly PrivacyEnforcementGate _gate;

    public PrivacyEnforcementGateTests()
    {
        _sanitizer = new ActivitySanitizer(_identityResolver, _forbiddenGuard);
        _gate = new PrivacyEnforcementGate(_sanitizer, _forbiddenGuard);
    }

    [Theory]
    [InlineData("AppKey", true)]
    [InlineData("DisplayName", true)]
    [InlineData("Category", true)]
    [InlineData("TimestampUtc", true)]
    [InlineData("ActiveSeconds", true)]
    [InlineData("WindowTitle", false)]
    [InlineData("Url", false)]
    [InlineData("BrowserUrl", false)]
    [InlineData("Keystrokes", false)]
    [InlineData("Clipboard", false)]
    [InlineData("ScreenCapture", false)]
    [InlineData("CommandLine", false)]
    public void AllowedActivityFields_CorrectlyFiltersFields(string fieldName, bool expectedPermitted)
    {
        bool permitted = AllowedActivityFields.IsFieldPermitted(fieldName);
        Assert.Equal(expectedPermitted, permitted);
    }

    [Fact]
    public void ForbiddenFieldGuard_RejectsIllegalProperties()
    {
        Assert.True(_forbiddenGuard.IsForbidden("WindowTitle"));
        Assert.True(_forbiddenGuard.IsForbidden("Url"));
        Assert.True(_forbiddenGuard.IsForbidden("Keystrokes"));
        Assert.True(_forbiddenGuard.IsForbidden("ClipboardData"));
        Assert.True(_forbiddenGuard.IsForbidden("Screenshot"));
        Assert.True(_forbiddenGuard.IsForbidden("CommandLine"));
        Assert.False(_forbiddenGuard.IsForbidden("AppKey"));
        Assert.False(_forbiddenGuard.IsForbidden("ActiveSeconds"));
    }

    private class MaliciousPayload
    {
        public string AppKey { get; set; } = "browser.chrome";
        public string WindowTitle { get; set; } = "Secret Bank Balance";
    }

    [Fact]
    public void ForbiddenFieldGuard_ThrowsOnMaliciousPayload()
    {
        var malicious = new MaliciousPayload();
        Assert.Throws<InvalidOperationException>(() => _forbiddenGuard.AssertNoForbiddenFields(malicious));
    }

    [Fact]
    public void ActivitySanitizer_StripsPathsAndReturnsCanonicalEvent()
    {
        string rawPath = @"C:\Users\Secret\AppData\Local\Programs\Microsoft VS Code\Code.exe";
        var now = DateTime.UtcNow;

        var canonical = _sanitizer.Sanitize(rawPath, now);

        Assert.NotNull(canonical);
        Assert.Equal("dev.vscode", canonical.AppKey);
        Assert.Equal("Visual Studio Code", canonical.DisplayName);
        Assert.Equal("Development", canonical.Category);
        Assert.Equal(now, canonical.TimestampUtc);
    }

    [Fact]
    public void ActivitySanitizer_ExcludesExcludedApplication()
    {
        // Add custom exclusion
        _identityResolver.SetExcluded("comm.slack", true);

        var canonical = _sanitizer.Sanitize("slack.exe", DateTime.UtcNow);

        // Excluded app must return null - no downstream canonical event produced
        Assert.Null(canonical);
    }

    [Fact]
    public void PrivacyEnforcementGate_SanitizeAndFilter_ProducesCleanCanonicalEvent()
    {
        var canonical = _gate.SanitizeAndFilter("devenv.exe", DateTime.UtcNow);

        Assert.NotNull(canonical);
        Assert.Equal("dev.vs", canonical.AppKey);
        Assert.Equal("Visual Studio", canonical.DisplayName);
        Assert.Equal("Development", canonical.Category);
        _gate.AssertForbiddenFieldAbsence(canonical);
    }

    [Fact]
    public void PrivacyAuditService_PassesOnAllMirrorAssemblies()
    {
        var auditService = new PrivacyAuditService(_forbiddenGuard);
        var report = auditService.PerformAudit();

        Assert.True(report.IsPassed, $"Privacy audit failed with violations: {string.Join("; ", report.Violations)}");
        Assert.Empty(report.Violations);
        Assert.True(report.CheckedRulesCount > 10);
    }
}
