using System;
using System.IO;
using Microsoft.Data.Sqlite;
using Mirror.Persistence;
using Xunit;

namespace Mirror.Persistence.Tests;

public class DatabaseMigrationTests : IDisposable
{
    private readonly string _tempDbPath;
    private readonly SqliteConnectionFactory _factory;

    public DatabaseMigrationTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"mirror_test_mig_{Guid.NewGuid():N}.db");
        _factory = new SqliteConnectionFactory(_tempDbPath);
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
    public void Migrate_CreatesAllTablesAndEnforcesPragmas()
    {
        var migrator = new DatabaseMigrator(_factory);
        migrator.Migrate();

        using var conn = _factory.CreateConnection();

        // Verify journal mode (WAL)
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA journal_mode;";
            var mode = cmd.ExecuteScalar()?.ToString();
            Assert.Equal("wal", mode?.ToLowerInvariant());
        }

        // Verify foreign keys enabled
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA foreign_keys;";
            var fk = Convert.ToInt32(cmd.ExecuteScalar());
            Assert.Equal(1, fk);
        }

        // Verify expected tables exist
        string[] expectedTables =
        {
            "schema_migrations",
            "activity_sessions",
            "idle_periods",
            "app_switch_events",
            "pattern_events",
            "daily_metrics",
            "settings",
            "app_category_overrides",
            "flow_state_sessions",
            "adaptive_baselines",
            "pattern_feedback",
            "recalibrated_thresholds"
        };

        foreach (var tbl in expectedTables)
        {
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@name;";
            cmd.Parameters.AddWithValue("@name", tbl);
            var exists = Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            Assert.True(exists, $"Table '{tbl}' should exist in migrated database.");
        }
    }
}
