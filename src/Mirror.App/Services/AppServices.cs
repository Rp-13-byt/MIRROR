using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Mirror.Analytics;
using Mirror.Core.Interfaces;
using Mirror.Inference;
using Mirror.Persistence;
using Mirror.Platform;
using Mirror.Security;
using Mirror.Security.Privacy;
using Mirror.Tracking;
using Mirror_App.ViewModels;

namespace Mirror_App.Services;

public static class AppServices
{
    private static IServiceProvider? _serviceProvider;

    public static IServiceProvider Services => _serviceProvider ?? throw new InvalidOperationException("Services not initialized.");

    public static void Initialize()
    {
        var services = new ServiceCollection();

        string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string mirrorDir = Path.Combine(localApp, "Mirror");
        Directory.CreateDirectory(mirrorDir);
        string dbPath = Path.Combine(mirrorDir, "mirror.db");

        // Persistence
        services.AddSingleton<ISqliteConnectionFactory>(new SqliteConnectionFactory(dbPath));
        services.AddSingleton<DatabaseMigrator>();
        services.AddSingleton<IMirrorRepository, MirrorRepository>();
        services.AddSingleton<IExportService, ExportService>();

        // Privacy Enforcement Gate & Security
        services.AddSingleton<ForbiddenFieldGuard>();
        services.AddSingleton<ActivitySanitizer>();
        services.AddSingleton<IPrivacyEnforcementGate, PrivacyEnforcementGate>();
        services.AddSingleton<PrivacyAuditService>();
        services.AddSingleton<IPrivacyGuard, PrivacyGuard>();

        // Tracking
        services.AddSingleton<IForegroundWindowMonitor, ForegroundWindowMonitor>();
        services.AddSingleton<IProcessResolver, ProcessResolver>();
        services.AddSingleton<IApplicationIdentityResolver, ApplicationIdentityResolver>();
        services.AddSingleton<IIdleDetector, IdleDetector>();
        services.AddSingleton<ISessionBuilder, SessionBuilder>();
        services.AddSingleton<ITrackingCoordinator, TrackingCoordinator>();

        // Analytics & ML
        services.AddSingleton<IFeatureExtractor, FeatureExtractor>();
        services.AddSingleton<IBaselineAnalyzer, BaselineAnalyzer>();
        services.AddSingleton<IPatternExplanationBuilder, PatternExplanationBuilder>();
        services.AddSingleton<IPatternDetector, PatternDetector>();
        services.AddSingleton<IPatternFusionEngine, PatternFusionEngine>();
        services.AddSingleton<IFlowStateDetector, FlowStateDetector>();
        services.AddSingleton<IAdaptiveBaselineService, AdaptiveBaselineService>();
        services.AddSingleton<IThresholdRecalibrator, ThresholdRecalibrator>();
        services.AddSingleton<IInsightExplorerService, InsightExplorerService>();

        // Wallet & Security
        services.AddSingleton<IWalletService>(sp => new WalletService(sp.GetRequiredService<IMirrorRepository>(), dbPath));

        // Reporting & Voice & LLM
        services.AddSingleton<Mirror.Reporting.IPdfReportService, Mirror.Reporting.PdfReportService>();
        services.AddSingleton<Mirror.Voice.LocalVoiceNarrator>();
        services.AddSingleton<Mirror.LLM.ILocalLlmService, Mirror.LLM.LocalLlmService>();

        // Inference
        services.AddSingleton<IInferenceBackendManager, InferenceBackendManager>();

        // Platform
        services.AddSingleton<IStartupManager, StartupManager>();
        services.AddSingleton<INotificationManager, NotificationManager>();

        // Demo Data
        services.AddSingleton<IDemoDataService, DemoDataService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddTransient<OverviewViewModel>();
        services.AddTransient<TimelineViewModel>();
        services.AddTransient<PatternsViewModel>();
        services.AddTransient<TrendsViewModel>();
        services.AddTransient<PrivacyViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DiagnosticsViewModel>();

        _serviceProvider = services.BuildServiceProvider();

        // Run migrations
        var migrator = _serviceProvider.GetRequiredService<DatabaseMigrator>();
        migrator.Migrate();

        // Initialize inference backends
        var inference = _serviceProvider.GetRequiredService<IInferenceBackendManager>();
        string modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Models");
        if (!Directory.Exists(modelsPath))
        {
            modelsPath = @"D:\MIRROR\ml\models";
        }
        // Initialize inference backends in background so UI thread is never blocked
        _ = Task.Run(async () =>
        {
            try
            {
                await inference.InitializeAsync(modelsPath).ConfigureAwait(false);
            }
            catch
            {
                // Graceful fallback to CPU managed by InferenceBackendManager
            }
        });
    }
}
