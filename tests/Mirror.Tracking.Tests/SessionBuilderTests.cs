using System;
using System.Collections.Generic;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Tracking;
using Xunit;

namespace Mirror.Tracking.Tests;

public class SessionBuilderTests
{
    [Fact]
    public void ForegroundAppChange_CreatesAndTransitionsSession()
    {
        var builder = new SessionBuilder();
        var recordedSessions = new List<ActivitySession>();
        builder.SessionCompleted += (s, e) => recordedSessions.Add(e);

        var t0 = DateTime.UtcNow;
        var app1 = new AppIdentity("code.exe", "Visual Studio Code", "Development");
        var app2 = new AppIdentity("msedge.exe", "Microsoft Edge", "Browsing");

        builder.OnForegroundAppChanged(app1, t0);
        Assert.NotNull(builder.CurrentSession);
        Assert.Equal("code.exe", builder.CurrentSession.AppKey);

        // Switch to app2 after 10 seconds
        var t1 = t0.AddSeconds(10);
        builder.OnForegroundAppChanged(app2, t1);

        Assert.Single(recordedSessions);
        Assert.Equal("code.exe", recordedSessions[0].AppKey);
        Assert.Equal(SessionCloseReason.AppSwitch, recordedSessions[0].CloseReason);
        Assert.Equal(10, recordedSessions[0].ActiveSeconds);

        Assert.NotNull(builder.CurrentSession);
        Assert.Equal("msedge.exe", builder.CurrentSession.AppKey);
    }

    [Fact]
    public void SameAppEvent_DoesNotSplitSession()
    {
        var builder = new SessionBuilder();
        var recordedSessions = new List<ActivitySession>();
        builder.SessionCompleted += (s, e) => recordedSessions.Add(e);

        var t0 = DateTime.UtcNow;
        var app1 = new AppIdentity("code.exe", "Visual Studio Code", "Development");

        builder.OnForegroundAppChanged(app1, t0);
        builder.OnForegroundAppChanged(app1, t0.AddSeconds(5));
        builder.OnForegroundAppChanged(app1, t0.AddSeconds(10));

        Assert.Empty(recordedSessions);
        Assert.NotNull(builder.CurrentSession);
        Assert.Equal("code.exe", builder.CurrentSession.AppKey);
    }

    [Fact]
    public void UserIdleState_ClosesSessionAndRecordsIdlePeriod()
    {
        var builder = new SessionBuilder();
        var recordedSessions = new List<ActivitySession>();
        var recordedIdle = new List<IdlePeriod>();
        builder.SessionCompleted += (s, e) => recordedSessions.Add(e);
        builder.IdlePeriodCompleted += (s, e) => recordedIdle.Add(e);

        var t0 = DateTime.UtcNow;
        var app = new AppIdentity("slack.exe", "Slack", "Communication");

        builder.OnForegroundAppChanged(app, t0);

        // Idle starts at t0 + 20s
        var t1 = t0.AddSeconds(20);
        builder.OnUserIdleStateChanged(true, t1);

        Assert.Single(recordedSessions);
        Assert.Equal(SessionCloseReason.Idle, recordedSessions[0].CloseReason);
        Assert.Null(builder.CurrentSession);

        // Idle ends at t1 + 60s
        var t2 = t1.AddSeconds(60);
        builder.OnUserIdleStateChanged(false, t2);

        Assert.Single(recordedIdle);
        Assert.Equal(60, recordedIdle[0].DurationSeconds);
    }

    [Fact]
    public void LockAndSleep_ClosesActiveSessionWithAppropriateReason()
    {
        var builder = new SessionBuilder();
        var recordedSessions = new List<ActivitySession>();
        builder.SessionCompleted += (s, e) => recordedSessions.Add(e);

        var t0 = DateTime.UtcNow;
        var app = new AppIdentity("code.exe", "Visual Studio Code", "Development");

        builder.OnForegroundAppChanged(app, t0);
        builder.OnSessionLockStateChanged(true, t0.AddSeconds(15));

        Assert.Single(recordedSessions);
        Assert.Equal(SessionCloseReason.Lock, recordedSessions[0].CloseReason);

        // Resume and then sleep
        builder.OnSessionLockStateChanged(false, t0.AddSeconds(30));
        builder.OnForegroundAppChanged(app, t0.AddSeconds(35));
        builder.OnSystemSleepStateChanged(true, t0.AddSeconds(50));

        Assert.Equal(2, recordedSessions.Count);
        Assert.Equal(SessionCloseReason.Sleep, recordedSessions[1].CloseReason);
    }
}
