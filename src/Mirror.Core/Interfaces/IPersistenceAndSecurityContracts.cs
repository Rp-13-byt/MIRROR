using Mirror.Core.Models;

namespace Mirror.Core.Interfaces;

public interface IMirrorRepository
{
    Task InsertSessionAsync(ActivitySession session, CancellationToken ct = default);
    Task InsertSessionsBatchAsync(IEnumerable<ActivitySession> sessions, CancellationToken ct = default);
    Task<IReadOnlyList<ActivitySession>> GetSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<IReadOnlyList<ActivitySession>> GetRecentSessionsAsync(TimeSpan lookback, CancellationToken ct = default);

    Task InsertIdlePeriodAsync(IdlePeriod idlePeriod, CancellationToken ct = default);
    Task<IReadOnlyList<IdlePeriod>> GetIdlePeriodsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    Task InsertAppSwitchAsync(AppSwitchEvent switchEvent, CancellationToken ct = default);
    Task<IReadOnlyList<AppSwitchEvent>> GetRecentSwitchesAsync(TimeSpan lookback, CancellationToken ct = default);

    Task InsertPatternEventAsync(PatternEvent patternEvent, CancellationToken ct = default);
    Task<IReadOnlyList<PatternEvent>> GetPatternEventsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    Task UpsertDailyMetricsAsync(DailyMetrics metrics, CancellationToken ct = default);
    Task<IReadOnlyList<DailyMetrics>> GetDailyMetricsRangeAsync(string startDateLocal, string endDateLocal, CancellationToken ct = default);

    Task<UserSettings> LoadSettingsAsync(CancellationToken ct = default);
    Task SaveSettingsAsync(UserSettings settings, CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, string>> GetCategoryOverridesAsync(CancellationToken ct = default);
    Task SaveCategoryOverrideAsync(string appKey, string category, CancellationToken ct = default);

    Task<long> PruneOlderThanAsync(DateTime thresholdUtc, CancellationToken ct = default);
    Task PurgeAllDataAsync(CancellationToken ct = default);
    Task<long> GetDatabaseSizeBytesAsync(CancellationToken ct = default);

    // Flow State
    Task InsertFlowStateSessionAsync(FlowStateSession session, CancellationToken ct = default);
    Task<IReadOnlyList<FlowStateSession>> GetFlowStateSessionsAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);

    // Adaptive Baseline
    Task UpsertAdaptiveBaselineAsync(AdaptiveBaseline baseline, CancellationToken ct = default);
    Task<AdaptiveBaseline?> GetAdaptiveBaselineAsync(string metricKey, int windowDays, CancellationToken ct = default);
    Task<IReadOnlyList<AdaptiveBaseline>> GetAllAdaptiveBaselinesAsync(int windowDays, CancellationToken ct = default);

    // Pattern Feedback & Recalibrated Thresholds
    Task InsertPatternFeedbackAsync(PatternFeedback feedback, CancellationToken ct = default);
    Task<IReadOnlyList<PatternFeedback>> GetPatternFeedbackAsync(CancellationToken ct = default);
    Task UpsertRecalibratedThresholdAsync(string thresholdKey, double value, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, double>> GetRecalibratedThresholdsAsync(CancellationToken ct = default);
    Task ResetRecalibratedThresholdsAsync(CancellationToken ct = default);
}


public interface IExportService
{
    Task<string> ExportAsCsvAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task<string> ExportAsJsonAsync(DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
    Task ExportToFileAsync(string filePath, string format, DateTime startUtc, DateTime endUtc, CancellationToken ct = default);
}

public interface IPrivacyGuard
{
    void ValidateSafeEntity<T>(T entity);
    bool IsFieldForbidden(string fieldName);
    IReadOnlyList<string> ScanObjectForViolations(object obj);
}

public interface IWalletService
{
    Task<string> ExportWalletAsync(string destinationFilePath, string password, CancellationToken ct = default);
    Task<bool> ImportWalletAsync(string sourceWalletFilePath, string password, CancellationToken ct = default);
}

