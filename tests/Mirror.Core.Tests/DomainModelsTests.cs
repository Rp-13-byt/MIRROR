using System;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Xunit;

namespace Mirror.Core.Tests;

public class DomainModelsTests
{
    [Fact]
    public void ActivitySession_Duration_CalculatedCorrectly()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMinutes(45);
        var session = new ActivitySession
        {
            AppKey = "code.exe",
            DisplayName = "Visual Studio Code",
            Category = "Development",
            StartUtc = start,
            EndUtc = end,
            ActiveSeconds = 45 * 60,
            CloseReason = SessionCloseReason.AppSwitch
        };

        Assert.Equal(TimeSpan.FromMinutes(45), session.Duration);
        Assert.Equal("code.exe", session.AppKey);
        Assert.Equal("Development", session.Category);
    }

    [Fact]
    public void ActivitySession_Duration_HandlesInvalidTimespanGracefully()
    {
        var start = DateTime.UtcNow;
        var end = start.AddMinutes(-10); // End before start
        var session = new ActivitySession
        {
            AppKey = "slack.exe",
            DisplayName = "Slack",
            Category = "Communication",
            StartUtc = start,
            EndUtc = end,
            ActiveSeconds = 0,
            CloseReason = SessionCloseReason.AppSwitch
        };

        Assert.Equal(TimeSpan.Zero, session.Duration);
    }

    [Fact]
    public void FeatureSequence_Flatten_ProducesExpectedLengthAndOrder()
    {
        var seq = new FeatureSequence(timesteps: 60, featureCount: 10);
        Assert.Equal(60, seq.Timesteps);
        Assert.Equal(10, seq.FeatureCount);

        seq.Values[0, 0] = 1.0f;
        seq.Values[59, 9] = 2.5f;

        var flattened = seq.Flatten();
        Assert.Equal(600, flattened.Length);
        Assert.Equal(1.0f, flattened[0]);
        Assert.Equal(2.5f, flattened[599]);
    }

    [Fact]
    public void UserSettings_DefaultValues_AreSensibleAndSafe()
    {
        var settings = new UserSettings();

        Assert.True(settings.TrackingEnabled);
        Assert.False(settings.StartWithWindows);
        Assert.Equal(120, settings.IdleThresholdSeconds);
        Assert.Equal(90, settings.RetentionDays);
        Assert.Equal(10, settings.HighSwitchWindowMinutes);
        Assert.Equal(10, settings.HighSwitchThreshold);
        Assert.Equal(45, settings.ExtendedSessionMinutes);
        Assert.Empty(settings.ExcludedApps);
    }

    [Fact]
    public void PatternEvent_HoldsSignalsAndConfidence()
    {
        var now = DateTime.UtcNow;
        var pat = new PatternEvent
        {
            Id = 42,
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = now.AddMinutes(-10),
            EndUtc = now,
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.95f,
            Explanation = "Rapid focus switching observed across 6 applications."
        };

        Assert.Equal(BehavioralPatternType.HighSwitchingBurst, pat.PatternType);
        Assert.True(pat.RuleSignal);
        Assert.True(pat.ModelSignal);
        Assert.Equal(0.95f, pat.ModelConfidence);
        Assert.Contains("Rapid focus switching", pat.Explanation);
    }
}
