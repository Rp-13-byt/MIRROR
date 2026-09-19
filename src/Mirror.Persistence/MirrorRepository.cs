using System.Text.Json;
using Microsoft.Data.Sqlite;
using Mirror.Core.Domain;
using Mirror.Core.Interfaces;
using Mirror.Core.Models;
using Mirror.Security;

namespace Mirror.Persistence;

public class MirrorRepository : IMirrorRepository
{
    private readonly ISqliteConnectionFactory _connectionFactory;
    private readonly IPrivacyGuard _privacyGuard;

    public MirrorRepository(ISqliteConnectionFactory connectionFactory, IPrivacyGuard? privacyGuard = null)
    {
        _connectionFactory = connectionFactory;
        _privacyGuard = privacyGuard ?? new PrivacyGuard();
    }

    public async Task InsertSessionAsync(ActivitySession session, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(session);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO activity_sessions (app_key, display_name, category, start_utc, end_utc, active_seconds, close_reason, created_utc)
            VALUES (@appKey, @displayName, @category, @startUtc, @endUtc, @activeSeconds, @closeReason, @createdUtc);
        ";
        cmd.Parameters.AddWithValue("@appKey", session.AppKey);
        cmd.Parameters.AddWithValue("@displayName", session.DisplayName);
        cmd.Parameters.AddWithValue("@category", session.Category);
        cmd.Parameters.AddWithValue("@startUtc", session.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", session.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@activeSeconds", session.ActiveSeconds);
        cmd.Parameters.AddWithValue("@closeReason", session.CloseReason.ToString());
        cmd.Parameters.AddWithValue("@createdUtc", session.CreatedUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task InsertSessionsBatchAsync(IEnumerable<ActivitySession> sessions, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        foreach (var session in sessions)
        {
            _privacyGuard.ValidateSafeEntity(session);

            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = @"
                INSERT INTO activity_sessions (app_key, display_name, category, start_utc, end_utc, active_seconds, close_reason, created_utc)
                VALUES (@appKey, @displayName, @category, @startUtc, @endUtc, @activeSeconds, @closeReason, @createdUtc);
            ";
            cmd.Parameters.AddWithValue("@appKey", session.AppKey);
            cmd.Parameters.AddWithValue("@displayName", session.DisplayName);
            cmd.Parameters.AddWithValue("@category", session.Category);
            cmd.Parameters.AddWithValue("@startUtc", session.StartUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@endUtc", session.EndUtc.ToString("o"));
            cmd.Parameters.AddWithValue("@activeSeconds", session.ActiveSeconds);
            cmd.Parameters.AddWithValue("@closeReason", session.CloseReason.ToString());
            cmd.Parameters.AddWithValue("@createdUtc", session.CreatedUtc.ToString("o"));

            await cmd.ExecuteNonQueryAsync(ct);
        }

        tx.Commit();
    }

    public async Task<IReadOnlyList<ActivitySession>> GetSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var list = new List<ActivitySession>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, app_key, display_name, category, start_utc, end_utc, active_seconds, close_reason, created_utc
            FROM activity_sessions
            WHERE start_utc >= @startUtc AND end_utc <= @endUtc
            ORDER BY start_utc ASC;
        ";
        cmd.Parameters.AddWithValue("@startUtc", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", endUtc.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(ReadSession(reader));
        }

        return list;
    }

    public async Task<IReadOnlyList<ActivitySession>> GetRecentSessionsAsync(TimeSpan lookback, CancellationToken ct = default)
    {
        DateTime threshold = DateTime.UtcNow - lookback;
        var list = new List<ActivitySession>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, app_key, display_name, category, start_utc, end_utc, active_seconds, close_reason, created_utc
            FROM activity_sessions
            WHERE start_utc >= @threshold
            ORDER BY start_utc ASC;
        ";
        cmd.Parameters.AddWithValue("@threshold", threshold.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(ReadSession(reader));
        }

        return list;
    }

    public async Task InsertIdlePeriodAsync(IdlePeriod idlePeriod, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(idlePeriod);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO idle_periods (start_utc, end_utc, duration_seconds, created_utc)
            VALUES (@startUtc, @endUtc, @durationSeconds, @createdUtc);
        ";
        cmd.Parameters.AddWithValue("@startUtc", idlePeriod.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", idlePeriod.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@durationSeconds", idlePeriod.DurationSeconds);
        cmd.Parameters.AddWithValue("@createdUtc", idlePeriod.CreatedUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<IdlePeriod>> GetIdlePeriodsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var list = new List<IdlePeriod>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, start_utc, end_utc, duration_seconds, created_utc
            FROM idle_periods
            WHERE start_utc >= @startUtc AND end_utc <= @endUtc
            ORDER BY start_utc ASC;
        ";
        cmd.Parameters.AddWithValue("@startUtc", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", endUtc.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new IdlePeriod
            {
                Id = reader.GetInt64(0),
                StartUtc = DateTime.Parse(reader.GetString(1)),
                EndUtc = DateTime.Parse(reader.GetString(2)),
                DurationSeconds = reader.GetInt32(3),
                CreatedUtc = DateTime.Parse(reader.GetString(4))
            });
        }

        return list;
    }

    public async Task InsertAppSwitchAsync(AppSwitchEvent switchEvent, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(switchEvent);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO app_switch_events (from_app_key, to_app_key, timestamp_utc)
            VALUES (@fromApp, @toApp, @ts);
        ";
        cmd.Parameters.AddWithValue("@fromApp", switchEvent.FromAppKey);
        cmd.Parameters.AddWithValue("@toApp", switchEvent.ToAppKey);
        cmd.Parameters.AddWithValue("@ts", switchEvent.TimestampUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<AppSwitchEvent>> GetRecentSwitchesAsync(TimeSpan lookback, CancellationToken ct = default)
    {
        DateTime threshold = DateTime.UtcNow - lookback;
        var list = new List<AppSwitchEvent>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, from_app_key, to_app_key, timestamp_utc
            FROM app_switch_events
            WHERE timestamp_utc >= @threshold
            ORDER BY timestamp_utc ASC;
        ";
        cmd.Parameters.AddWithValue("@threshold", threshold.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new AppSwitchEvent
            {
                Id = reader.GetInt64(0),
                FromAppKey = reader.GetString(1),
                ToAppKey = reader.GetString(2),
                TimestampUtc = DateTime.Parse(reader.GetString(3))
            });
        }

        return list;
    }

    public async Task InsertPatternEventAsync(PatternEvent patternEvent, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(patternEvent);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO pattern_events (pattern_type, start_utc, end_utc, detected_utc, rule_signal, model_signal, model_confidence, explanation)
            VALUES (@type, @startUtc, @endUtc, @detectedUtc, @ruleSignal, @modelSignal, @confidence, @explanation);
        ";
        cmd.Parameters.AddWithValue("@type", patternEvent.PatternType.ToString());
        cmd.Parameters.AddWithValue("@startUtc", patternEvent.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", patternEvent.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@detectedUtc", patternEvent.DetectedUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@ruleSignal", patternEvent.RuleSignal ? 1 : 0);
        cmd.Parameters.AddWithValue("@modelSignal", patternEvent.ModelSignal ? 1 : 0);
        cmd.Parameters.AddWithValue("@confidence", patternEvent.ModelConfidence);
        cmd.Parameters.AddWithValue("@explanation", patternEvent.Explanation);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PatternEvent>> GetPatternEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var list = new List<PatternEvent>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, pattern_type, start_utc, end_utc, detected_utc, rule_signal, model_signal, model_confidence, explanation
            FROM pattern_events
            WHERE detected_utc >= @startUtc AND detected_utc <= @endUtc
            ORDER BY detected_utc DESC;
        ";
        cmd.Parameters.AddWithValue("@startUtc", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", endUtc.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PatternEvent
            {
                Id = reader.GetInt64(0),
                PatternType = Enum.TryParse<BehavioralPatternType>(reader.GetString(1), out var pt) ? pt : BehavioralPatternType.Normal,
                StartUtc = DateTime.Parse(reader.GetString(2)),
                EndUtc = DateTime.Parse(reader.GetString(3)),
                DetectedUtc = DateTime.Parse(reader.GetString(4)),
                RuleSignal = reader.GetInt32(5) == 1,
                ModelSignal = reader.GetInt32(6) == 1,
                ModelConfidence = reader.GetFloat(7),
                Explanation = reader.GetString(8)
            });
        }

        return list;
    }

    public async Task UpsertDailyMetricsAsync(DailyMetrics metrics, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(metrics);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO daily_metrics (date_local, active_seconds, idle_seconds, switch_count, session_count, unique_app_count, late_night_seconds, longest_session_seconds, top_app_key)
            VALUES (@dateLocal, @activeSec, @idleSec, @switchCnt, @sessCnt, @uniqApps, @lateNightSec, @longestSessSec, @topApp)
            ON CONFLICT(date_local) DO UPDATE SET
                active_seconds = excluded.active_seconds,
                idle_seconds = excluded.idle_seconds,
                switch_count = excluded.switch_count,
                session_count = excluded.session_count,
                unique_app_count = excluded.unique_app_count,
                late_night_seconds = excluded.late_night_seconds,
                longest_session_seconds = excluded.longest_session_seconds,
                top_app_key = excluded.top_app_key;
        ";
        cmd.Parameters.AddWithValue("@dateLocal", metrics.DateLocal);
        cmd.Parameters.AddWithValue("@activeSec", metrics.ActiveSeconds);
        cmd.Parameters.AddWithValue("@idleSec", metrics.IdleSeconds);
        cmd.Parameters.AddWithValue("@switchCnt", metrics.SwitchCount);
        cmd.Parameters.AddWithValue("@sessCnt", metrics.SessionCount);
        cmd.Parameters.AddWithValue("@uniqApps", metrics.UniqueAppCount);
        cmd.Parameters.AddWithValue("@lateNightSec", metrics.LateNightSeconds);
        cmd.Parameters.AddWithValue("@longestSessSec", metrics.LongestSessionSeconds);
        cmd.Parameters.AddWithValue("@topApp", (object?)metrics.TopAppKey ?? DBNull.Value);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<DailyMetrics>> GetDailyMetricsRangeAsync(string startDateLocal, string endDateLocal, CancellationToken ct = default)
    {
        var list = new List<DailyMetrics>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT date_local, active_seconds, idle_seconds, switch_count, session_count, unique_app_count, late_night_seconds, longest_session_seconds, top_app_key
            FROM daily_metrics
            WHERE date_local >= @startDate AND date_local <= @endDate
            ORDER BY date_local ASC;
        ";
        cmd.Parameters.AddWithValue("@startDate", startDateLocal);
        cmd.Parameters.AddWithValue("@endDate", endDateLocal);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new DailyMetrics
            {
                DateLocal = reader.GetString(0),
                ActiveSeconds = reader.GetInt32(1),
                IdleSeconds = reader.GetInt32(2),
                SwitchCount = reader.GetInt32(3),
                SessionCount = reader.GetInt32(4),
                UniqueAppCount = reader.GetInt32(5),
                LateNightSeconds = reader.GetInt32(6),
                LongestSessionSeconds = reader.GetInt32(7),
                TopAppKey = reader.IsDBNull(8) ? null : reader.GetString(8)
            });
        }

        return list;
    }

    public async Task<UserSettings> LoadSettingsAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT value FROM settings WHERE key = 'user_settings';";

        var result = await cmd.ExecuteScalarAsync(ct);
        if (result != null && result is string json)
        {
            try
            {
                var settings = JsonSerializer.Deserialize<UserSettings>(json);
                if (settings != null) return settings;
            }
            catch
            {
                // Fallback to default
            }
        }

        return new UserSettings();
    }

    public async Task SaveSettingsAsync(UserSettings settings, CancellationToken ct = default)
    {
        _privacyGuard.ValidateSafeEntity(settings);
        string json = JsonSerializer.Serialize(settings);

        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO settings (key, value, updated_utc)
            VALUES ('user_settings', @val, datetime('now'))
            ON CONFLICT(key) DO UPDATE SET
                value = excluded.value,
                updated_utc = excluded.updated_utc;
        ";
        cmd.Parameters.AddWithValue("@val", json);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, string>> GetCategoryOverridesAsync(CancellationToken ct = default)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT app_key, category FROM app_category_overrides;";

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            dict[reader.GetString(0)] = reader.GetString(1);
        }

        return dict;
    }

    public async Task SaveCategoryOverrideAsync(string appKey, string category, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO app_category_overrides (app_key, category, updated_utc)
            VALUES (@appKey, @category, datetime('now'))
            ON CONFLICT(app_key) DO UPDATE SET
                category = excluded.category,
                updated_utc = excluded.updated_utc;
        ";
        cmd.Parameters.AddWithValue("@appKey", appKey);
        cmd.Parameters.AddWithValue("@category", category);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<long> PruneOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();
        string thresholdStr = thresholdUtc.ToString("o");

        long rowsDeleted = 0;

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM activity_sessions WHERE start_utc < @threshold;";
            cmd.Parameters.AddWithValue("@threshold", thresholdStr);
            rowsDeleted += await cmd.ExecuteNonQueryAsync(ct);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM idle_periods WHERE start_utc < @threshold;";
            cmd.Parameters.AddWithValue("@threshold", thresholdStr);
            rowsDeleted += await cmd.ExecuteNonQueryAsync(ct);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM app_switch_events WHERE timestamp_utc < @threshold;";
            cmd.Parameters.AddWithValue("@threshold", thresholdStr);
            rowsDeleted += await cmd.ExecuteNonQueryAsync(ct);
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = "DELETE FROM pattern_events WHERE detected_utc < @threshold;";
            cmd.Parameters.AddWithValue("@threshold", thresholdStr);
            rowsDeleted += await cmd.ExecuteNonQueryAsync(ct);
        }

        tx.Commit();

        // Run vacuum to reclaim disk pages
        using (var vacCmd = conn.CreateCommand())
        {
            vacCmd.CommandText = "VACUUM;";
            try { await vacCmd.ExecuteNonQueryAsync(ct); } catch { /* best effort */ }
        }

        return rowsDeleted;
    }

    public async Task PurgeAllDataAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var tx = conn.BeginTransaction();

        using (var cmd = conn.CreateCommand())
        {
            cmd.Transaction = tx;
            cmd.CommandText = @"
                DELETE FROM activity_sessions;
                DELETE FROM idle_periods;
                DELETE FROM app_switch_events;
                DELETE FROM pattern_events;
                DELETE FROM daily_metrics;
                DELETE FROM flow_state_sessions;
                DELETE FROM adaptive_baselines;
                DELETE FROM pattern_feedback;
            ";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        tx.Commit();

        using (var vacCmd = conn.CreateCommand())
        {
            vacCmd.CommandText = "VACUUM;";
            await vacCmd.ExecuteNonQueryAsync(ct);
        }
    }

    // Flow State
    public async Task InsertFlowStateSessionAsync(FlowStateSession session, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO flow_state_sessions (app_key, display_name, start_utc, end_utc, duration_seconds, distraction_count, created_utc)
            VALUES (@appKey, @displayName, @startUtc, @endUtc, @duration, @distractions, @createdUtc);
        ";
        cmd.Parameters.AddWithValue("@appKey", session.AppKey);
        cmd.Parameters.AddWithValue("@displayName", session.DisplayName);
        cmd.Parameters.AddWithValue("@startUtc", session.StartUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", session.EndUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@duration", session.DurationSeconds);
        cmd.Parameters.AddWithValue("@distractions", session.DistractionCount);
        cmd.Parameters.AddWithValue("@createdUtc", session.CreatedUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<FlowStateSession>> GetFlowStateSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default)
    {
        var list = new List<FlowStateSession>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, app_key, display_name, start_utc, end_utc, duration_seconds, distraction_count, created_utc
            FROM flow_state_sessions
            WHERE start_utc >= @startUtc AND end_utc <= @endUtc
            ORDER BY start_utc DESC;
        ";
        cmd.Parameters.AddWithValue("@startUtc", startUtc.ToString("o"));
        cmd.Parameters.AddWithValue("@endUtc", endUtc.ToString("o"));

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new FlowStateSession
            {
                Id = reader.GetInt64(0),
                AppKey = reader.GetString(1),
                DisplayName = reader.GetString(2),
                StartUtc = DateTime.Parse(reader.GetString(3)),
                EndUtc = DateTime.Parse(reader.GetString(4)),
                DurationSeconds = reader.GetInt32(5),
                DistractionCount = reader.GetInt32(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7))
            });
        }
        return list;
    }

    // Adaptive Baseline
    public async Task UpsertAdaptiveBaselineAsync(AdaptiveBaseline baseline, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO adaptive_baselines (metric_key, window_days, median_value, std_dev, sample_count, updated_utc)
            VALUES (@metricKey, @windowDays, @medianValue, @stdDev, @sampleCount, @updatedUtc)
            ON CONFLICT(metric_key, window_days) DO UPDATE SET
                median_value = excluded.median_value,
                std_dev = excluded.std_dev,
                sample_count = excluded.sample_count,
                updated_utc = excluded.updated_utc;
        ";
        cmd.Parameters.AddWithValue("@metricKey", baseline.MetricKey);
        cmd.Parameters.AddWithValue("@windowDays", baseline.WindowDays);
        cmd.Parameters.AddWithValue("@medianValue", baseline.MedianValue);
        cmd.Parameters.AddWithValue("@stdDev", baseline.StdDev);
        cmd.Parameters.AddWithValue("@sampleCount", baseline.SampleCount);
        cmd.Parameters.AddWithValue("@updatedUtc", baseline.UpdatedUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<AdaptiveBaseline?> GetAdaptiveBaselineAsync(string metricKey, int windowDays, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT metric_key, window_days, median_value, std_dev, sample_count, updated_utc
            FROM adaptive_baselines
            WHERE metric_key = @metricKey AND window_days = @windowDays;
        ";
        cmd.Parameters.AddWithValue("@metricKey", metricKey);
        cmd.Parameters.AddWithValue("@windowDays", windowDays);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        if (await reader.ReadAsync(ct))
        {
            return new AdaptiveBaseline
            {
                MetricKey = reader.GetString(0),
                WindowDays = reader.GetInt32(1),
                MedianValue = reader.GetDouble(2),
                StdDev = reader.GetDouble(3),
                SampleCount = reader.GetInt32(4),
                UpdatedUtc = DateTime.Parse(reader.GetString(5))
            };
        }
        return null;
    }

    public async Task<IReadOnlyList<AdaptiveBaseline>> GetAllAdaptiveBaselinesAsync(int windowDays, CancellationToken ct = default)
    {
        var list = new List<AdaptiveBaseline>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT metric_key, window_days, median_value, std_dev, sample_count, updated_utc
            FROM adaptive_baselines
            WHERE window_days = @windowDays;
        ";
        cmd.Parameters.AddWithValue("@windowDays", windowDays);

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new AdaptiveBaseline
            {
                MetricKey = reader.GetString(0),
                WindowDays = reader.GetInt32(1),
                MedianValue = reader.GetDouble(2),
                StdDev = reader.GetDouble(3),
                SampleCount = reader.GetInt32(4),
                UpdatedUtc = DateTime.Parse(reader.GetString(5))
            });
        }
        return list;
    }

    // Pattern Feedback & Recalibrated Thresholds
    public async Task InsertPatternFeedbackAsync(PatternFeedback feedback, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO pattern_feedback (pattern_event_id, pattern_type, feedback_reason, adjusted_threshold_key, previous_threshold_val, new_threshold_val, created_utc)
            VALUES (@eventId, @patternType, @reason, @key, @prevVal, @newVal, @createdUtc);
        ";
        cmd.Parameters.AddWithValue("@eventId", feedback.PatternEventId);
        cmd.Parameters.AddWithValue("@patternType", feedback.PatternType.ToString());
        cmd.Parameters.AddWithValue("@reason", feedback.FeedbackReason);
        cmd.Parameters.AddWithValue("@key", feedback.AdjustedThresholdKey);
        cmd.Parameters.AddWithValue("@prevVal", feedback.PreviousThresholdValue);
        cmd.Parameters.AddWithValue("@newVal", feedback.NewThresholdValue);
        cmd.Parameters.AddWithValue("@createdUtc", feedback.CreatedUtc.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<PatternFeedback>> GetPatternFeedbackAsync(CancellationToken ct = default)
    {
        var list = new List<PatternFeedback>();
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT id, pattern_event_id, pattern_type, feedback_reason, adjusted_threshold_key, previous_threshold_val, new_threshold_val, created_utc
            FROM pattern_feedback
            ORDER BY created_utc DESC;
        ";

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new PatternFeedback
            {
                Id = reader.GetInt64(0),
                PatternEventId = reader.GetInt64(1),
                PatternType = Enum.TryParse<BehavioralPatternType>(reader.GetString(2), out var pt) ? pt : BehavioralPatternType.HighSwitchingBurst,
                FeedbackReason = reader.GetString(3),
                AdjustedThresholdKey = reader.GetString(4),
                PreviousThresholdValue = reader.GetDouble(5),
                NewThresholdValue = reader.GetDouble(6),
                CreatedUtc = DateTime.Parse(reader.GetString(7))
            });
        }
        return list;
    }

    public async Task UpsertRecalibratedThresholdAsync(string thresholdKey, double value, CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO recalibrated_thresholds (threshold_key, threshold_value, updated_utc)
            VALUES (@key, @val, @updated)
            ON CONFLICT(threshold_key) DO UPDATE SET
                threshold_value = excluded.threshold_value,
                updated_utc = excluded.updated_utc;
        ";
        cmd.Parameters.AddWithValue("@key", thresholdKey);
        cmd.Parameters.AddWithValue("@val", value);
        cmd.Parameters.AddWithValue("@updated", DateTime.UtcNow.ToString("o"));

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyDictionary<string, double>> GetRecalibratedThresholdsAsync(CancellationToken ct = default)
    {
        var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT threshold_key, threshold_value FROM recalibrated_thresholds;";

        using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            dict[reader.GetString(0)] = reader.GetDouble(1);
        }
        return dict;
    }

    public async Task ResetRecalibratedThresholdsAsync(CancellationToken ct = default)
    {
        using var conn = _connectionFactory.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM recalibrated_thresholds;";
        await cmd.ExecuteNonQueryAsync(ct);
    }


    public Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct = default)
    {
        string path = _connectionFactory.DatabasePath;
        if (File.Exists(path))
        {
            return Task.FromResult(new FileInfo(path).Length);
        }
        return Task.FromResult(0L);
    }

    private static ActivitySession ReadSession(SqliteDataReader reader)
    {
        return new ActivitySession
        {
            Id = reader.GetInt64(0),
            AppKey = reader.GetString(1),
            DisplayName = reader.GetString(2),
            Category = reader.GetString(3),
            StartUtc = DateTime.Parse(reader.GetString(4)),
            EndUtc = DateTime.Parse(reader.GetString(5)),
            ActiveSeconds = reader.GetInt32(6),
            CloseReason = Enum.TryParse<SessionCloseReason>(reader.GetString(7), out var reason) ? reason : SessionCloseReason.AppSwitch,
            CreatedUtc = DateTime.Parse(reader.GetString(8))
        };
    }
}
