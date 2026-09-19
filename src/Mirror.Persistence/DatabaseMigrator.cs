using Microsoft.Data.Sqlite;

namespace Mirror.Persistence;

public class DatabaseMigrator
{
    private readonly ISqliteConnectionFactory _connectionFactory;

    public DatabaseMigrator(ISqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void Migrate()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        // 1. Ensure migrations table exists
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS schema_migrations (
                    version INTEGER PRIMARY KEY,
                    description TEXT NOT NULL,
                    applied_utc TEXT NOT NULL
                );
            ";
            cmd.ExecuteNonQuery();
        }

        // 2. Check current version
        long currentVersion = 0;
        using (var cmd = connection.CreateCommand())
        {
            cmd.Transaction = transaction;
            cmd.CommandText = "SELECT COALESCE(MAX(version), 0) FROM schema_migrations;";
            currentVersion = (long)(cmd.ExecuteScalar() ?? 0L);
        }

        // Apply migration 1 if needed
        if (currentVersion < 1)
        {
            ApplyMigration1(connection, transaction);
        }

        // Apply migration 2 if needed
        if (currentVersion < 2)
        {
            ApplyMigration2(connection, transaction);
        }

        transaction.Commit();
    }

    private static void ApplyMigration1(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            -- Activity Sessions
            CREATE TABLE IF NOT EXISTS activity_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                app_key TEXT NOT NULL,
                display_name TEXT NOT NULL,
                category TEXT NOT NULL,
                start_utc TEXT NOT NULL,
                end_utc TEXT NOT NULL,
                active_seconds INTEGER NOT NULL,
                close_reason TEXT NOT NULL,
                created_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_sessions_start_utc ON activity_sessions(start_utc);
            CREATE INDEX IF NOT EXISTS idx_sessions_app_key ON activity_sessions(app_key);
            CREATE INDEX IF NOT EXISTS idx_sessions_category ON activity_sessions(category);

            -- Idle Periods
            CREATE TABLE IF NOT EXISTS idle_periods (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                start_utc TEXT NOT NULL,
                end_utc TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                created_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_idle_start_utc ON idle_periods(start_utc);

            -- App Switch Events
            CREATE TABLE IF NOT EXISTS app_switch_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                from_app_key TEXT NOT NULL,
                to_app_key TEXT NOT NULL,
                timestamp_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_switches_timestamp_utc ON app_switch_events(timestamp_utc);

            -- Pattern Events
            CREATE TABLE IF NOT EXISTS pattern_events (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pattern_type TEXT NOT NULL,
                start_utc TEXT NOT NULL,
                end_utc TEXT NOT NULL,
                detected_utc TEXT NOT NULL,
                rule_signal INTEGER NOT NULL,
                model_signal INTEGER NOT NULL,
                model_confidence REAL NOT NULL,
                explanation TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS idx_patterns_detected_utc ON pattern_events(detected_utc);
            CREATE INDEX IF NOT EXISTS idx_patterns_type ON pattern_events(pattern_type);

            -- Daily Metrics Aggregate
            CREATE TABLE IF NOT EXISTS daily_metrics (
                date_local TEXT PRIMARY KEY,
                active_seconds INTEGER NOT NULL,
                idle_seconds INTEGER NOT NULL,
                switch_count INTEGER NOT NULL,
                session_count INTEGER NOT NULL,
                unique_app_count INTEGER NOT NULL,
                late_night_seconds INTEGER NOT NULL,
                longest_session_seconds INTEGER NOT NULL,
                top_app_key TEXT
            );

            -- User Settings
            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );

            -- App Category Overrides
            CREATE TABLE IF NOT EXISTS app_category_overrides (
                app_key TEXT PRIMARY KEY,
                category TEXT NOT NULL,
                updated_utc TEXT NOT NULL
            );

            -- Record Migration
            INSERT INTO schema_migrations (version, description, applied_utc)
            VALUES (1, 'Initial schema with activity sessions, idle periods, patterns, settings', datetime('now'));
        ";
        cmd.ExecuteNonQuery();
    }

    private static void ApplyMigration2(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var cmd = connection.CreateCommand();
        cmd.Transaction = transaction;
        cmd.CommandText = @"
            -- Flow State Sessions
            CREATE TABLE IF NOT EXISTS flow_state_sessions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                app_key TEXT NOT NULL,
                display_name TEXT NOT NULL,
                start_utc TEXT NOT NULL,
                end_utc TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                distraction_count INTEGER NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_flow_start_utc ON flow_state_sessions(start_utc);

            -- Adaptive Baselines
            CREATE TABLE IF NOT EXISTS adaptive_baselines (
                metric_key TEXT NOT NULL,
                window_days INTEGER NOT NULL,
                median_value REAL NOT NULL,
                std_dev REAL NOT NULL,
                sample_count INTEGER NOT NULL,
                updated_utc TEXT NOT NULL,
                PRIMARY KEY (metric_key, window_days)
            );

            -- Pattern Feedback
            CREATE TABLE IF NOT EXISTS pattern_feedback (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                pattern_event_id INTEGER NOT NULL,
                pattern_type TEXT NOT NULL,
                feedback_reason TEXT NOT NULL,
                adjusted_threshold_key TEXT NOT NULL,
                previous_threshold_val REAL NOT NULL,
                new_threshold_val REAL NOT NULL,
                created_utc TEXT NOT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_feedback_created_utc ON pattern_feedback(created_utc);

            -- Recalibrated Thresholds
            CREATE TABLE IF NOT EXISTS recalibrated_thresholds (
                threshold_key TEXT PRIMARY KEY,
                threshold_value REAL NOT NULL,
                updated_utc TEXT NOT NULL
            );

            -- Record Migration
            INSERT INTO schema_migrations (version, description, applied_utc)
            VALUES (2, 'Add flow state sessions, adaptive baselines, feedback and recalibrated thresholds', datetime('now'));
        ";
        cmd.ExecuteNonQuery();
    }
}

