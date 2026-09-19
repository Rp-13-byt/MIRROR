using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Mirror.Core.Domain;
using Mirror.Core.Models;
using Mirror.Persistence;
using Mirror.Security;
using Xunit;

namespace Mirror.Persistence.Tests;

public class MirrorRepositoryTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteConnectionFactory _factory;
    private readonly MirrorRepository _repo;

    public MirrorRepositoryTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"mirror_test_repo_{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_tempDbPath);
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        _repo = new MirrorRepository(_factory, new PrivacyGuard());
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    [Fact]
    public async Task SessionLifecycle_InsertAndRetrieve_Succeeds()
    {
        var start = DateTime.UtcNow.AddMinutes(-30);
        var end = DateTime.UtcNow;
        var session = new ActivitySession
        {
            AppKey = "dev.vscode",
            DisplayName = "Visual Studio Code",
            Category = "Development",
            StartUtc = start,
            EndUtc = end,
            ActiveSeconds = 1800,
            CloseReason = SessionCloseReason.AppSwitch
        };

        await _repo.InsertSessionAsync(session);

        var sessions = await _repo.GetSessionsAsync(start.AddMinutes(-5), end.AddMinutes(5));
        Assert.Single(sessions);
        Assert.Equal("dev.vscode", sessions[0].AppKey);
        Assert.Equal("Development", sessions[0].Category);
        Assert.Equal(1800, sessions[0].ActiveSeconds);
    }

    [Fact]
    public async Task IdlePeriod_InsertAndRetrieve_Succeeds()
    {
        var start = DateTime.UtcNow.AddMinutes(-10);
        var end = DateTime.UtcNow;
        var idle = new IdlePeriod
        {
            StartUtc = start,
            EndUtc = end,
            DurationSeconds = 600
        };

        await _repo.InsertIdlePeriodAsync(idle);

        var periods = await _repo.GetIdlePeriodsAsync(start.AddMinutes(-1), end.AddMinutes(1));
        Assert.Single(periods);
        Assert.Equal(600, periods[0].DurationSeconds);
    }

    [Fact]
    public async Task AppSwitch_InsertAndRetrieveRecent_Succeeds()
    {
        var sw = new AppSwitchEvent
        {
            FromAppKey = "dev.vscode",
            ToAppKey = "browser.edge",
            TimestampUtc = DateTime.UtcNow
        };

        await _repo.InsertAppSwitchAsync(sw);

        var recent = await _repo.GetRecentSwitchesAsync(TimeSpan.FromMinutes(5));
        Assert.Single(recent);
        Assert.Equal("dev.vscode", recent[0].FromAppKey);
        Assert.Equal("browser.edge", recent[0].ToAppKey);
    }

    [Fact]
    public async Task PatternEvent_InsertAndRetrieve_Succeeds()
    {
        var pat = new PatternEvent
        {
            PatternType = BehavioralPatternType.HighSwitchingBurst,
            StartUtc = DateTime.UtcNow.AddMinutes(-15),
            EndUtc = DateTime.UtcNow,
            RuleSignal = true,
            ModelSignal = true,
            ModelConfidence = 0.92f,
            Explanation = "Rapid focus switching observed."
        };

        await _repo.InsertPatternEventAsync(pat);

        var events = await _repo.GetPatternEventsAsync(DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(5));
        Assert.Single(events);
        Assert.Equal(BehavioralPatternType.HighSwitchingBurst, events[0].PatternType);
        Assert.Equal(0.92f, events[0].ModelConfidence);
    }

    [Fact]
    public async Task DailyMetrics_UpsertAndRange_Succeeds()
    {
        var metrics = new DailyMetrics
        {
            DateLocal = "2026-09-18",
            ActiveSeconds = 14400,
            IdleSeconds = 3600,
            SwitchCount = 120,
            SessionCount = 35,
            UniqueAppCount = 6,
            LateNightSeconds = 0,
            LongestSessionSeconds = 3600,
            TopAppKey = "dev.vscode"
        };

        await _repo.UpsertDailyMetricsAsync(metrics);

        var range = await _repo.GetDailyMetricsRangeAsync("2026-09-18", "2026-09-18");
        Assert.Single(range);
        Assert.Equal(14400, range[0].ActiveSeconds);
        Assert.Equal(120, range[0].SwitchCount);
    }

    [Fact]
    public async Task UserSettings_SaveAndLoad_RoundtripsAccurately()
    {
        var custom = new UserSettings
        {
            TrackingEnabled = false,
            StartWithWindows = true,
            IdleThresholdSeconds = 300,
            RetentionDays = 60,
            ExcludedApps = new List<string> { "keepass.exe", "1password.exe" }
        };

        await _repo.SaveSettingsAsync(custom);
        var loaded = await _repo.LoadSettingsAsync();

        Assert.False(loaded.TrackingEnabled);
        Assert.True(loaded.StartWithWindows);
        Assert.Equal(300, loaded.IdleThresholdSeconds);
        Assert.Equal(60, loaded.RetentionDays);
        Assert.Equal(2, loaded.ExcludedApps.Count);
        Assert.Contains("keepass.exe", loaded.ExcludedApps);
    }

    [Fact]
    public async Task PurgeAllData_EradicatesAllUserData()
    {
        await _repo.InsertSessionAsync(new ActivitySession
        {
            AppKey = "test.exe",
            DisplayName = "Test",
            Category = "Other",
            StartUtc = DateTime.UtcNow.AddMinutes(-5),
            EndUtc = DateTime.UtcNow,
            ActiveSeconds = 300,
            CloseReason = SessionCloseReason.ProcessExit
        });

        await _repo.PurgeAllDataAsync();

        var sessions = await _repo.GetSessionsAsync(DateTime.UtcNow.AddHours(-1), DateTime.UtcNow.AddHours(1));
        Assert.Empty(sessions);
    }
}
